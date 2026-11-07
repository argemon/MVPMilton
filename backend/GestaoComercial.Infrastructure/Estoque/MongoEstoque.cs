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

public sealed class MongoEstoque(OperacoesCadastro operations, IMongoClient client, Microsoft.Extensions.Logging.ILogger<MongoEstoque> logger, Microsoft.Extensions.Configuration.IConfiguration configuration,EstoqueSchema schema) : IEstoque
{
    private static readonly string[] Readers = ["Administrador", "Vendedor", "Estoquista"];
    private static readonly string[] Writers = ["Administrador", "Estoquista"];
    private IMongoCollection<ProdutoDocumento> Products => operations.Db.GetCollection<ProdutoDocumento>("products");
    private IMongoCollection<BsonDocument> Movements => operations.Db.GetCollection<BsonDocument>("stock_movements");
    private IMongoCollection<BsonDocument> Receipts => operations.Db.GetCollection<BsonDocument>("idempotency_receipts");
    internal static BsonBinaryData Uuid(Guid value) => new(value, GuidRepresentation.Standard);
    internal static Movimento Map(BsonDocument d) => new(d["_id"].AsGuid, d["produtoId"].AsGuid, d["tipo"].AsString, d["delta"].AsInt32, d["saldoAntes"].AsInt32, d["saldoDepois"].AsInt32, d["motivo"].AsString, d["atorId"].AsGuid, d["atorNomeSnapshot"].AsString, d["ocorridoEm"].ToUniversalTime(), d.GetValue("vendaId",BsonNull.Value).IsBsonNull?null:d["vendaId"].AsGuid, d.GetValue("itemId",BsonNull.Value).IsBsonNull?null:d["itemId"].AsGuid);

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
        if (produtoId == Guid.Empty || tipo is not (null or "Entrada" or "SaidaManual" or "Ajuste" or "Venda" or "EstornoVenda")) throw new CadastroException("VALIDACAO");
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
        if(!await schema.IsReadyAsync(ct))throw new CadastroException("PERSISTENCIA_NAO_INICIALIZADA",503);
        if (chave == Guid.Empty || produtoId == Guid.Empty) throw new CadastroException("VALIDACAO");
        int delta; string reason;
        try { delta=Movimentacao.Delta(tipo,quantidade); reason=Movimentacao.Motivo(motivo); } catch(ArgumentException) { throw new CadastroException("VALIDACAO"); }
        var scope = "estoque/"+tipo+"/v1";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { produtoId, delta, motivo = reason })));
        var id=Guid.NewGuid();
        var result=await new OperacoesComerciais(operations,client).ExecutarAsync(ator,roles,chave,scope,hash,201,id,(session,token)=>AplicarAsync(session,ator,produtoId,tipo,delta,reason,id,chave,token),ct);
        return Map(result);
    }
    internal async Task<BsonDocument> AplicarAsync(IClientSessionHandle session,SessaoAtual ator,Guid productId,string type,int delta,string reason,Guid id,Guid operationId,CancellationToken ct,Guid? vendaId=null,Guid? itemId=null)
    {
        // Read raw BSON so a missing embedded balance cannot become a POCO's default zero.
        var raw=await operations.Db.GetCollection<BsonDocument>("products").Find(session,new BsonDocument("_id",Uuid(productId))).SingleOrDefaultAsync(ct) ?? throw new CadastroException("PRODUTO_NAO_ENCONTRADO",404);
        if(type!="EstornoVenda"&&!raw["ativo"].AsBoolean)throw new CadastroException("RECURSO_INATIVO",409);
        var (before,balanceVersion)=LerSaldo(raw);int after;
        try { after=Movimentacao.SaldoFinal(before,delta); }catch(InvalidOperationException e){throw new CadastroException(e.Message,409);}
        var filter=Builders<ProdutoDocumento>.Filter.Eq(x=>x.Id,productId) & Builders<ProdutoDocumento>.Filter.Eq(x=>x.Saldo.Quantidade,before) & Builders<ProdutoDocumento>.Filter.Eq(x=>x.Saldo.Versao,balanceVersion);
        if(type!="EstornoVenda")filter &= Builders<ProdutoDocumento>.Filter.Eq(x=>x.Ativo,true);
        filter &= delta<0 ? Builders<ProdutoDocumento>.Filter.Gte(x=>x.Saldo.Quantidade,-delta) : Builders<ProdutoDocumento>.Filter.Lte(x=>x.Saldo.Quantidade,int.MaxValue-delta);
        var updated=await Products.UpdateOneAsync(session,filter,Builders<ProdutoDocumento>.Update.Inc(x=>x.Saldo.Quantidade,delta).Inc(x=>x.Saldo.Versao,1L),cancellationToken:ct);
        if(updated.ModifiedCount!=1)throw new CadastroException("SALDO_INEXISTENTE",409);
        var movement=new BsonDocument { { "_id",Uuid(id) }, { "schemaVersion",1 }, { "produtoId",Uuid(productId) }, { "operacaoId",Uuid(operationId) }, { "tipo",type }, { "delta",delta }, { "saldoAntes",before }, { "saldoDepois",after }, { "motivo",reason }, { "atorId",Uuid(ator.Usuario.Id) }, { "atorNomeSnapshot",ator.Usuario.Nome }, { "ocorridoEm",operations.Now } };
        if(vendaId is {} sale && itemId is {} item){movement.Remove("operacaoId");movement["vendaId"]=Uuid(sale);movement["itemId"]=Uuid(item);}
        await Movements.InsertOneAsync(session,movement,cancellationToken:ct);
        await operations.AuditarAsync(session,ator,type,"MovimentoEstoque",id,ct);
        return movement;
    }
    internal (int Quantidade,long Versao) LerSaldo(BsonDocument raw)
    {
        if(!raw.TryGetValue("saldo",out var balance) || !balance.IsBsonDocument || !balance.AsBsonDocument.TryGetValue("quantidade",out var qty) || qty.BsonType!=BsonType.Int32 || qty.AsInt32<0 || !balance.AsBsonDocument.TryGetValue("versao",out var version) || version.BsonType!=BsonType.Int64 || version.AsInt64<1)
        { Microsoft.Extensions.Logging.LoggerExtensions.LogError(logger,"Saldo inconsistente no produto {ProdutoId}",raw["_id"].AsGuid);throw new CadastroException("SALDO_INEXISTENTE",409); }
        return (qty.AsInt32,version.AsInt64);
    }

}
