using System.Security.Cryptography;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Infrastructure.Estoque;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Persistence;

internal sealed class OperacoesComerciais(OperacoesCadastro operations,IMongoClient client)
{
    private IMongoCollection<BsonDocument> Receipts=>operations.Db.GetCollection<BsonDocument>("idempotency_receipts");
    private IMongoCollection<BsonDocument> Movements=>operations.Db.GetCollection<BsonDocument>("stock_movements");
    private static BsonBinaryData Uuid(Guid id)=>MongoEstoque.Uuid(id);
    internal async Task<BsonDocument> ExecutarAsync(SessaoAtual ator,string[] roles,Guid chave,string scope,byte[] hash,int status,Guid resourceId,Func<IClientSessionHandle,CancellationToken,Task<BsonDocument>> body,CancellationToken ct)
    {
        if(chave==Guid.Empty)throw new CadastroException("VALIDACAO");
        await operations.ExigirAsync(ator,roles,ct);
        var receiptFilter=new BsonDocument{{"atorId",Uuid(ator.Usuario.Id)},{"escopo",scope},{"chave",Uuid(chave)}};
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct); budget.CancelAfter(TimeSpan.FromSeconds(5)); var token=budget.Token;
        using var session=await client.StartSessionAsync(cancellationToken:token);
        for(var attempt=0;;attempt++)
        {
            session.StartTransaction(new TransactionOptions(ReadConcern.Snapshot,ReadPreference.Primary,WriteConcern.WMajority));
            var committing=false;
            try
            {
                await operations.ExigirAsync(ator,roles,token,session);
                var existing=await Receipts.Find(session,receiptFilter).SingleOrDefaultAsync(token);
                if(existing is not null) { await session.AbortTransactionAsync(token); return Receipt(existing,hash); }
                var id=resourceId;
                // Reserve the unique key before touching the product. This document is never visible until commit.
                var receipt=new BsonDocument { { "_id",Uuid(Guid.NewGuid()) }, { "schemaVersion",1 }, { "atorId",Uuid(ator.Usuario.Id) }, { "escopo",scope }, { "chave",Uuid(chave) }, { "hashRequest",new BsonBinaryData(hash) }, { "recursoId",Uuid(id) }, { "estado","Preparando" }, { "statusHttp",status }, { "resposta",new BsonDocument() }, { "criadoEm",operations.Now } };
                await Receipts.InsertOneAsync(session,receipt,cancellationToken:token);
                var movement=await body(session,token);
                receipt["estado"]="Concluido";receipt["resposta"]=movement;
                var result=await Receipts.ReplaceOneAsync(session,new BsonDocument("_id",receipt["_id"]),receipt,cancellationToken:token);
                if(result.ModifiedCount!=1) throw new InvalidOperationException("Recibo não persistido.");
                committing=true;
                while(true)
                {
                    try { await session.CommitTransactionAsync(token); return movement; }
                    catch(MongoException e) when(e.HasErrorLabel("UnknownTransactionCommitResult") && !token.IsCancellationRequested) { await Task.Delay(20,token); }
                }
            }
            catch(MongoException e) when(!committing && Duplicate(e))
            {
                await Abort(session);
                try
                {
                    while(!token.IsCancellationRequested)
                    {
                        var receipt=await Receipts.WithReadConcern(ReadConcern.Majority).WithReadPreference(ReadPreference.Primary).Find(receiptFilter).SingleOrDefaultAsync(token);
                        if(receipt is not null) return Receipt(receipt,hash);
                        // A globally reused manual key from another actor cannot reveal that actor's receipt.
                        var other=scope.StartsWith("estoque/",StringComparison.Ordinal) && await Movements.Find(new BsonDocument("operacaoId",Uuid(chave))).AnyAsync(token);
                        if(other) throw new CadastroException("CHAVE_REUTILIZADA",409);
                        await Task.Delay(50,token);
                    }
                } catch(OperationCanceledException) when(!ct.IsCancellationRequested) { }
                throw new CadastroException("OPERACAO_EM_ANDAMENTO",409);
            }
            catch(MongoException e) when(e.HasErrorLabel("TransientTransactionError") && !e.HasErrorLabel("UnknownTransactionCommitResult") && attempt<2 && !token.IsCancellationRequested)
            { await Abort(session);await Task.Delay(250*(attempt+1)+Random.Shared.Next(20,70),token); }
            catch(MongoException e) when(e.HasErrorLabel("TransientTransactionError") && !e.HasErrorLabel("UnknownTransactionCommitResult"))
            { await Abort(session);throw; }
            catch(Exception e) when(committing && (e is MongoException or OperationCanceledException))
            { throw new CadastroException("RESULTADO_DESCONHECIDO",503); }
            catch { await Abort(session);throw; }
        }
    }
    private static bool Duplicate(MongoException e) => e is MongoWriteException { WriteError.Category:ServerErrorCategory.DuplicateKey } || e is MongoCommandException { Code:11000 };
    private static BsonDocument Receipt(BsonDocument d,byte[] hash)
    {
        if(!CryptographicOperations.FixedTimeEquals(d["hashRequest"].AsByteArray,hash)) throw new CadastroException("CHAVE_REUTILIZADA",409);
        if(d["estado"]!="Concluido") throw new CadastroException("OPERACAO_EM_ANDAMENTO",409);
        return d["resposta"].AsBsonDocument;
    }
    private static async Task Abort(IClientSessionHandle session)
    { if(session.IsInTransaction)try { using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(2));await session.AbortTransactionAsync(timeout.Token); }catch(MongoException){}catch(OperationCanceledException){} }

}
