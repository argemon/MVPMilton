using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Infrastructure.Identity;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Funcionarios;

public sealed class FuncionariosSchema(IMongoClient client, MongoConfiguration configuration, AuthSchema auth)
{
    private IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    public const string Id = "02-employees";
    private static BsonDocument Text(int min, int max) => new() { { "bsonType", "string" }, { "minLength", min }, { "maxLength", max } };
    internal static BsonDocument Properties => new()
    {
        { "_id", new BsonDocument("bsonType", "binData") }, { "schemaVersion", new BsonDocument { { "bsonType", "int" }, { "enum", new BsonArray { 1 } } } },
        { "nome", Text(2,150) }, { "nomeNormalizado", Text(2,150) },
        { "email", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "minLength", 3 }, { "maxLength", 254 } } },
        { "telefone", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "pattern", "^[0-9]{10,15}$" } } },
        { "ativo", new BsonDocument("bsonType", "bool") }, { "versao", new BsonDocument { { "bsonType", "long" }, { "minimum", 1 } } },
        { "guardVersion", new BsonDocument { { "bsonType", "long" }, { "minimum", 0 } } },
        { "createdAt", new BsonDocument("bsonType", "date") }, { "updatedAt", new BsonDocument("bsonType", "date") }
    };
    internal static BsonDocument Validator(BsonDocument properties) => new("$jsonSchema", new BsonDocument { { "bsonType", "object" }, { "additionalProperties", false }, { "required", new BsonArray(properties.Names) }, { "properties", properties } });
    private static string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Validator(Properties).ToJson() + ":nameId:employeeUniqueBinData:v1")));
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        var r = await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id", Id)).FirstOrDefaultAsync(ct);
        return r is not null && r.GetValue("checksum", "") == Checksum && r.GetValue("version", 0) == 3;
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await auth.IsReadyAsync(ct)) throw new InvalidOperationException("Evolução01 ausente.");
        await EnsureCollectionAsync(Db, "employees", Validator(Properties), ct);
        await Db.GetCollection<BsonDocument>("employees").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument { { "nomeNormalizado", 1 }, { "_id", 1 } }, new CreateIndexOptions { Name = "ix_employees_nome_id" }), cancellationToken: ct);
        await Db.GetCollection<BsonDocument>("users").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("employeeId", 1), new CreateIndexOptions<BsonDocument> { Name = "ux_users_employee", Unique = true, PartialFilterExpression = new BsonDocument("employeeId", new BsonDocument("$type", "binData")) }), cancellationToken: ct);
        await RecordAsync(Db, Id, 3, Checksum, ct);
    }
    internal static async Task EnsureCollectionAsync(IMongoDatabase db, string name, BsonDocument validator, CancellationToken ct)
    {
        using var names = await db.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = new BsonDocument("name", name) }, ct);
        if (!(await names.ToListAsync(ct)).Contains(name))
            try { await db.CreateCollectionAsync(name, new CreateCollectionOptions<BsonDocument> { Validator = validator, ValidationLevel = DocumentValidationLevel.Strict, ValidationAction = DocumentValidationAction.Error }, ct); }
            catch (MongoCommandException e) when (e.Code == 48) { }
        using var current = await db.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", name) }, ct);
        var options = (await current.FirstAsync(ct))["options"].AsBsonDocument;
        if (options.GetValue("validator", new BsonDocument()) != validator || options.GetValue("validationLevel", "") != "strict" || options.GetValue("validationAction", "") != "error") throw new InvalidOperationException("Evolução divergente; revisão necessária.");
    }
    internal static async Task RecordAsync(IMongoDatabase db, string id, int version, string checksum, CancellationToken ct)
    {
        var catalog = db.GetCollection<BsonDocument>("persistence_versions");
        try { await catalog.InsertOneAsync(new BsonDocument { { "_id", id }, { "schemaVersion", 1 }, { "version", version }, { "checksum", checksum }, { "appliedAt", DateTime.UtcNow } }, cancellationToken: ct); }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { }
        var r = await catalog.Find(new BsonDocument("_id", id)).SingleAsync(ct);
        if (r["checksum"] != checksum || r["version"] != version) throw new InvalidOperationException("Evolução incompatível, não sobrescrita.");
    }
}
