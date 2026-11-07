using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Infrastructure.Funcionarios;
using GestaoComercial.Infrastructure.Persistence;
using GestaoComercial.Infrastructure.Produtos;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Estoque;

public sealed class EstoqueSchema(IMongoClient client, MongoConfiguration configuration, ProdutosSchema previous)
{
    private IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    public const string Id = "05-stock";
    private static BsonDocument Type(string value) => new("bsonType", value);
    private static BsonDocument Text(int min, int max) => new() { { "bsonType", "string" }, { "minLength", min }, { "maxLength", max } };
    internal static BsonDocument MovementValidator => new("$and", new BsonArray
    {
        FuncionariosSchema.Validator(new BsonDocument
        {
            { "_id", Type("binData") }, { "schemaVersion", new BsonDocument { { "bsonType", "int" }, { "enum", new BsonArray { 1 } } } },
            { "produtoId", Type("binData") }, { "operacaoId", Type("binData") },
            { "tipo", new BsonDocument("enum", new BsonArray { "Entrada", "SaidaManual", "Ajuste" }) },
            { "delta", new BsonDocument { { "bsonType", "int" }, { "minimum", -1000000 }, { "maximum", 1000000 } } },
            { "saldoAntes", new BsonDocument { { "bsonType", "int" }, { "minimum", 0 } } },
            { "saldoDepois", new BsonDocument { { "bsonType", "int" }, { "minimum", 0 } } },
            { "motivo", Text(10, 500) }, { "atorId", Type("binData") }, { "atorNomeSnapshot", Text(2,150) }, { "ocorridoEm", Type("date") }
        }),
        BsonDocument.Parse("{ $expr: { $and: [ { $ne: ['$delta',0] }, { $eq: ['$saldoDepois',{$add:['$saldoAntes','$delta']}] }, { $or: [ {$and:[{$eq:['$tipo','Entrada']},{$gt:['$delta',0]}]}, {$and:[{$eq:['$tipo','SaidaManual']},{$lt:['$delta',0]}]}, {$eq:['$tipo','Ajuste']} ] } ] } }")
    });
    internal static BsonDocument ReceiptValidator => new("$and", new BsonArray
    {
        FuncionariosSchema.Validator(new BsonDocument
        {
            { "_id", Type("binData") }, { "schemaVersion", Type("int") }, { "atorId", Type("binData") }, { "escopo", Text(1,100) }, { "chave", Type("binData") },
            { "hashRequest", Type("binData") }, { "recursoId", Type("binData") }, { "estado", new BsonDocument("enum", new BsonArray { "Preparando", "Concluido" }) },
            { "statusHttp", new BsonDocument("enum", new BsonArray { 201 }) }, { "resposta", Type("object") }, { "criadoEm", Type("date") }
        }),
        BsonDocument.Parse("{$expr:{$and:[{$eq:[{$binarySize:'$hashRequest'},32]},{$or:[{$eq:['$estado','Preparando']},{$and:[{$eq:['$estado','Concluido']},{$eq:['$recursoId','$resposta._id']},{$eq:['$atorId','$resposta.atorId']},{$eq:['$chave','$resposta.operacaoId']}]}]}]}}")
    });
    private static string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(MovementValidator.ToJson() + ReceiptValidator.ToJson() + ":movementDateId:productDateId:operationUnique:actorScopeKeyUnique:receiptDate:v1")));
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        var r = await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id", Id)).FirstOrDefaultAsync(ct);
        return r is not null && r.GetValue("checksum", "") == Checksum && r.GetValue("version", 0) == 6;
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await previous.IsReadyAsync(ct)) throw new InvalidOperationException("Evolução04 ausente.");
        await GestaoComercial.Infrastructure.Vendas.VendasSchema.EnsureCompatibleAsync(Db, "stock_movements", MovementValidator, GestaoComercial.Infrastructure.Vendas.VendasSchema.MovementValidator, ct);
        await GestaoComercial.Infrastructure.Vendas.VendasSchema.EnsureCompatibleAsync(Db, "idempotency_receipts", ReceiptValidator, GestaoComercial.Infrastructure.Vendas.VendasSchema.ReceiptValidator, ct);
        await Index("stock_movements", "ix_stock_data_id", new BsonDocument { { "ocorridoEm", -1 }, { "_id", -1 } }, false, null, ct);
        await Index("stock_movements", "ix_stock_produto_data_id", new BsonDocument { { "produtoId", 1 }, { "ocorridoEm", -1 }, { "_id", -1 } }, false, null, ct);
        await Index("stock_movements", "ux_stock_operacao", new BsonDocument("operacaoId",1), true, new BsonDocument("operacaoId",new BsonDocument("$type","binData")), ct);
        await Index("idempotency_receipts", "ux_receipts_ator_escopo_chave", new BsonDocument { { "atorId", 1 }, { "escopo", 1 }, { "chave", 1 } }, true, null, ct);
        await Index("idempotency_receipts", "ix_receipts_data", new BsonDocument("criadoEm",1), false, null, ct);
        await FuncionariosSchema.RecordAsync(Db, Id, 6, Checksum, ct);
    }
    private Task<string> Index(string collection, string name, BsonDocument keys, bool unique, BsonDocument? partial, CancellationToken ct) => Db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions<BsonDocument> { Name = name, Unique = unique, PartialFilterExpression = partial }), cancellationToken: ct);
}
