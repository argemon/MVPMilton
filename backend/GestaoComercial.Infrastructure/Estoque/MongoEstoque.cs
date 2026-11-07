using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Estoque;
using GestaoComercial.Domain.Estoque;
using GestaoComercial.Infrastructure.Persistence;
using GestaoComercial.Infrastructure.Produtos;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Estoque;

public sealed class MongoEstoque(OperacoesCadastro operations, IMongoClient client, Microsoft.Extensions.Logging.ILogger<MongoEstoque> logger, Microsoft.Extensions.Configuration.IConfiguration configuration) : IEstoque
{
    private static readonly string[] Readers = ["Administrador", "Vendedor", "Estoquista"];
    private static readonly string[] Writers = ["Administrador", "Estoquista"];
    private IMongoCollection<ProdutoDocumento> Products => operations.Db.GetCollection<ProdutoDocumento>("products");
    private IMongoCollection<BsonDocument> Movements => operations.Db.GetCollection<BsonDocument>("stock_movements");
    private IMongoCollection<BsonDocument> Receipts => operations.Db.GetCollection<BsonDocument>("idempotency_receipts");
    internal static BsonBinaryData Uuid(Guid value) => new(value, GuidRepresentation.Standard);
    internal static Movimento Map(BsonDocument d) => new(d["_id"].AsGuid, d["produtoId"].AsGuid, d["tipo"].AsString, d["delta"].AsInt32, d["saldoAntes"].AsInt32, d["saldoDepois"].AsInt32, d["motivo"].AsString, d["atorId"].AsGuid, d["atorNomeSnapshot"].AsString, d["ocorridoEm"].ToUniversalTime());

    public async Task<PaginaEstoque<SaldoProduto>> SaldosAsync(SessaoAtual ator, string? search, bool? ativo, bool abaixoMinimo, int page, int pageSize, CancellationToken ct)
    {
        await operations.ExigirAsync(ator, Readers, ct); OperacoesCadastro.Pagina(page, pageSize, search);
        var f = Builders<ProdutoDocumento>.Filter.Empty;
        if (ativo is { } active) f &= Builders<ProdutoDocumento>.Filter.Eq(x => x.Ativo, active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            f &= Builders<ProdutoDocumento>.Filter.Regex(x => x.NomeNormalizado, new BsonRegularExpression("^" + Regex.Escape(normalized))) | Builders<ProdutoDocumento>.Filter.Eq(x => x.SkuNormalizado, normalized);
        }
        if (abaixoMinimo) f &= new BsonDocument("$expr", new BsonDocument("$lte",new BsonArray { "$saldo.quantidade", "$estoqueMinimo" }));
        var total = await Products.CountDocumentsAsync(f, cancellationToken: ct);
        var rows = await Products.Find(f).SortBy(x => x.Saldo.Quantidade).ThenBy(x => x.NomeNormalizado).ThenBy(x => x.Id).Skip((page-1)*pageSize).Limit(pageSize)
            .Project(x => new SaldoProduto(x.Id,x.Sku,x.Nome,x.Saldo.Quantidade,x.EstoqueMinimo,x.Ativo)).ToListAsync(ct);
        return new(rows,page,pageSize,total);
    }
    internal static (DateTime? Inicio, DateTime? Fim) Periodo(DateOnly? inicio, DateOnly? fim, string zoneId = "America/Cuiaba")
    {
        if (inicio is null && fim is null) return (null,null);
        if (inicio is null || fim is null || fim <= inicio || fim.Value.DayNumber-inicio.Value.DayNumber > 366) throw new CadastroException("VALIDACAO");
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        return (TimeZoneInfo.ConvertTimeToUtc(inicio.Value.ToDateTime(TimeOnly.MinValue),zone),TimeZoneInfo.ConvertTimeToUtc(fim.Value.ToDateTime(TimeOnly.MinValue),zone));
    }
    public async Task<PaginaEstoque<Movimento>> HistoricoAsync(SessaoAtual ator, Guid? produtoId, string? tipo, DateOnly? inicio, DateOnly? fim, int page, int pageSize, CancellationToken ct)
    {
        await operations.ExigirAsync(ator, Writers, ct); OperacoesCadastro.Pagina(page,pageSize);
        if (produtoId == Guid.Empty || tipo is not (null or "Entrada" or "SaidaManual" or "Ajuste")) throw new CadastroException("VALIDACAO");
        var period = Periodo(inicio,fim,configuration["APP_TIME_ZONE"]??"America/Cuiaba"); var f = Builders<BsonDocument>.Filter.Empty;
        if (produtoId is { } product) f &= new BsonDocument("produtoId",Uuid(product));
        if (tipo is not null) f &= new BsonDocument("tipo",tipo);
        if (period.Inicio is { } start) f &= Builders<BsonDocument>.Filter.Gte("ocorridoEm",start) & Builders<BsonDocument>.Filter.Lt("ocorridoEm",period.Fim!.Value);
        var total = await Movements.CountDocumentsAsync(f,cancellationToken:ct);
        var rows = await Movements.Find(f).Sort(new BsonDocument { { "ocorridoEm",-1 }, { "_id",-1 } }).Skip((page-1)*pageSize).Limit(pageSize).ToListAsync(ct);
        return new(rows.Select(Map).ToArray(),page,pageSize,total);
    }
    public async Task<Movimento> ConsultarAsync(SessaoAtual ator, Guid id, CancellationToken ct)
    {
        await operations.ExigirAsync(ator,Writers,ct);
        return Map(await Movements.Find(new BsonDocument("_id",Uuid(id))).SingleOrDefaultAsync(ct) ?? throw new CadastroException("MOVIMENTO_NAO_ENCONTRADO",404));
    }
    public async Task<Movimento> MovimentarAsync(SessaoAtual ator, Guid chave, Guid produtoId, string tipo, int quantidade, string? motivo, CancellationToken ct)
    {
        var roles = tipo == "Ajuste" ? new[] { "Administrador" } : Writers;
        await operations.ExigirAsync(ator,roles,ct);
        if (chave == Guid.Empty || produtoId == Guid.Empty) throw new CadastroException("VALIDACAO");
        int delta; string reason;
        try { delta=Movimentacao.Delta(tipo,quantidade); reason=Movimentacao.Motivo(motivo); } catch(ArgumentException) { throw new CadastroException("VALIDACAO"); }
        var scope = "estoque/"+tipo+"/v1";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { produtoId, delta, motivo = reason })));
        var receiptFilter = new BsonDocument { { "atorId",Uuid(ator.Usuario.Id) }, { "escopo",scope }, { "chave",Uuid(chave) } };
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
                var id=Guid.NewGuid();
                // Reserve the unique key before touching the product. This document is never visible until commit.
                var receipt=new BsonDocument { { "_id",Uuid(Guid.NewGuid()) }, { "schemaVersion",1 }, { "atorId",Uuid(ator.Usuario.Id) }, { "escopo",scope }, { "chave",Uuid(chave) }, { "hashRequest",new BsonBinaryData(hash) }, { "recursoId",Uuid(id) }, { "estado","Preparando" }, { "statusHttp",201 }, { "resposta",new BsonDocument() }, { "criadoEm",operations.Now } };
                await Receipts.InsertOneAsync(session,receipt,cancellationToken:token);
                var movement=await AplicarAsync(session,ator,produtoId,tipo,delta,reason,id,chave,token);
                receipt["estado"]="Concluido";receipt["resposta"]=movement;
                var result=await Receipts.ReplaceOneAsync(session,new BsonDocument("_id",receipt["_id"]),receipt,cancellationToken:token);
                if(result.ModifiedCount!=1) throw new InvalidOperationException("Recibo não persistido.");
                committing=true;
                while(true)
                {
                    try { await session.CommitTransactionAsync(token); return Map(movement); }
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
                        var other=await Movements.Find(new BsonDocument("operacaoId",Uuid(chave))).AnyAsync(token);
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
    private static Movimento Receipt(BsonDocument d,byte[] hash)
    {
        if(!CryptographicOperations.FixedTimeEquals(d["hashRequest"].AsByteArray,hash)) throw new CadastroException("CHAVE_REUTILIZADA",409);
        if(d["estado"]!="Concluido") throw new CadastroException("OPERACAO_EM_ANDAMENTO",409);
        return Map(d["resposta"].AsBsonDocument);
    }
    private static async Task Abort(IClientSessionHandle session)
    { if(session.IsInTransaction)try { using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(2));await session.AbortTransactionAsync(timeout.Token); }catch(MongoException){}catch(OperationCanceledException){} }

    internal async Task<BsonDocument> AplicarAsync(IClientSessionHandle session,SessaoAtual ator,Guid productId,string type,int delta,string reason,Guid id,Guid operationId,CancellationToken ct)
    {
        // Read raw BSON so a missing embedded balance cannot become a POCO's default zero.
        var raw=await operations.Db.GetCollection<BsonDocument>("products").Find(session,new BsonDocument("_id",Uuid(productId))).SingleOrDefaultAsync(ct) ?? throw new CadastroException("PRODUTO_NAO_ENCONTRADO",404);
        if(!raw["ativo"].AsBoolean)throw new CadastroException("RECURSO_INATIVO",409);
        if(!raw.TryGetValue("saldo",out var balance) || !balance.IsBsonDocument || !balance.AsBsonDocument.TryGetValue("quantidade",out var qty) || qty.BsonType!=BsonType.Int32 || qty.AsInt32<0 || !balance.AsBsonDocument.TryGetValue("versao",out var version) || version.BsonType!=BsonType.Int64 || version.AsInt64<1)
        { Microsoft.Extensions.Logging.LoggerExtensions.LogError(logger,"Saldo inconsistente no produto {ProdutoId}",productId);throw new CadastroException("SALDO_INEXISTENTE",409); }
        var before=qty.AsInt32;int after;
        try { after=Movimentacao.SaldoFinal(before,delta); }catch(InvalidOperationException e){throw new CadastroException(e.Message,409);}
        var filter=Builders<ProdutoDocumento>.Filter.Eq(x=>x.Id,productId) & Builders<ProdutoDocumento>.Filter.Eq(x=>x.Ativo,true) & Builders<ProdutoDocumento>.Filter.Eq(x=>x.Saldo.Quantidade,before) & Builders<ProdutoDocumento>.Filter.Eq(x=>x.Saldo.Versao,version.AsInt64);
        filter &= delta<0 ? Builders<ProdutoDocumento>.Filter.Gte(x=>x.Saldo.Quantidade,-delta) : Builders<ProdutoDocumento>.Filter.Lte(x=>x.Saldo.Quantidade,int.MaxValue-delta);
        var updated=await Products.UpdateOneAsync(session,filter,Builders<ProdutoDocumento>.Update.Inc(x=>x.Saldo.Quantidade,delta).Inc(x=>x.Saldo.Versao,1L),cancellationToken:ct);
        if(updated.ModifiedCount!=1)throw new CadastroException("SALDO_INEXISTENTE",409);
        var movement=new BsonDocument { { "_id",Uuid(id) }, { "schemaVersion",1 }, { "produtoId",Uuid(productId) }, { "operacaoId",Uuid(operationId) }, { "tipo",type }, { "delta",delta }, { "saldoAntes",before }, { "saldoDepois",after }, { "motivo",reason }, { "atorId",Uuid(ator.Usuario.Id) }, { "atorNomeSnapshot",ator.Usuario.Nome }, { "ocorridoEm",operations.Now } };
        await Movements.InsertOneAsync(session,movement,cancellationToken:ct);
        await operations.AuditarAsync(session,ator,type,"MovimentoEstoque",id,ct);
        return movement;
    }
}
