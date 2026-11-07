using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Infrastructure.Funcionarios;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Clientes;

public sealed class ClientesSchema(IMongoClient client, MongoConfiguration configuration, FuncionariosSchema previous)
{
    private IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    public const string Id = "03-customers";
    private static BsonDocument Validator
    {
        get {
            var p = FuncionariosSchema.Properties;
            p.Add("tipo", new BsonDocument("enum", new BsonArray { "PF", "PJ" }));
            p.Add("documento", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "pattern", "^([0-9]{11}|[0-9]{14})$" } });
            return FuncionariosSchema.Validator(p);
        }
    }
    private static string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Validator.ToJson() + ":nameId:documentUniqueString:v1")));
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        var r = await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id", Id)).FirstOrDefaultAsync(ct);
        return r is not null && r.GetValue("checksum", "") == Checksum && r.GetValue("version", 0) == 4;
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await previous.IsReadyAsync(ct)) throw new InvalidOperationException("Evolução02 ausente.");
        await FuncionariosSchema.EnsureCollectionAsync(Db, "customers", Validator, ct);
        var c = Db.GetCollection<BsonDocument>("customers");
        await c.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument { { "nomeNormalizado", 1 }, { "_id", 1 } }, new CreateIndexOptions { Name = "ix_customers_nome_id" }), cancellationToken: ct);
        await c.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("documento", 1), new CreateIndexOptions<BsonDocument> { Name = "ux_customers_documento", Unique = true, PartialFilterExpression = new BsonDocument("documento", new BsonDocument("$type", "string")) }), cancellationToken: ct);
        await FuncionariosSchema.RecordAsync(Db, Id, 4, Checksum, ct);
    }
}
