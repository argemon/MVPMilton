using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
namespace GestaoComercial.Infrastructure.Persistence;

public sealed class MongoFoundation(IMongoClient client, MongoConfiguration configuration)
{
    public const string BaselineId = "00-foundation";
    private const string IndexName = "ux_persistence_version";
    public static string Checksum { get; } = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("M00:persistence_versions:v1:strict:error:unique-version"))).ToLowerInvariant();
    private IMongoDatabase Database => client.GetDatabase(configuration.DatabaseName);
    private IMongoCollection<BsonDocument> Versions => Database.GetCollection<BsonDocument>("persistence_versions");
    public static BsonDocument Validator => BsonDocument.Parse("""
    { "$jsonSchema": { "bsonType": "object", "required": ["_id","schemaVersion","version","checksum","appliedAt"], "additionalProperties": false, "properties": {
      "_id": {"bsonType":"string"}, "schemaVersion":{"bsonType":"int","minimum":1}, "version":{"bsonType":"int","minimum":1},
      "checksum":{"bsonType":"string","minLength":64,"maxLength":64}, "appliedAt":{"bsonType":"date"}
    } } }
    """);

    public async Task<bool> SupportsTransactionsAsync(CancellationToken ct)
    {
        var hello = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: ct);
        await Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
        var topology = hello.Contains("setName") || (hello.TryGetValue("msg", out var msg) && msg == "isdbgrid");
        return topology && hello.GetValue("isWritablePrimary", false).ToBoolean() && hello.Contains("logicalSessionTimeoutMinutes") && hello.GetValue("maxWireVersion", 0).ToInt32() >= 8;
    }

    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        try
        {
            if (!await SupportsTransactionsAsync(ct)) return false;
            var baseline = await Versions.Find(new BsonDocument("_id", BaselineId)).FirstOrDefaultAsync(ct);
            return baseline is not null && baseline.GetValue("version", 0) == 1 && baseline.GetValue("checksum", "") == Checksum;
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await SupportsTransactionsAsync(ct)) throw new InvalidOperationException("MongoDB precisa de topologia compatível com transações e primary disponível.");
        using var cursor = await Database.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", "persistence_versions") }, ct);
        var existing = await cursor.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            try { await Database.CreateCollectionAsync("persistence_versions", new CreateCollectionOptions<BsonDocument> { Validator = Validator, ValidationAction = DocumentValidationAction.Error, ValidationLevel = DocumentValidationLevel.Strict }, ct); }
            catch (MongoCommandException e) when (e.Code == 48) { } // Another explicit initializer created it; verify below.
        }
        using var verification = await Database.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", "persistence_versions") }, ct);
        var collection = await verification.FirstAsync(ct);
        var options = collection["options"].AsBsonDocument;
        if (!options.GetValue("validator", new BsonDocument()).Equals(Validator) || options.GetValue("validationLevel", "") != "strict" || options.GetValue("validationAction", "") != "error") throw new InvalidOperationException("Definição incompatível: persistence_versions. Revisão operacional necessária.");
        await Versions.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("version", 1), new CreateIndexOptions { Name = IndexName, Unique = true }), cancellationToken: ct);
        var record = new BsonDocument { { "_id", BaselineId }, { "schemaVersion", 1 }, { "version", 1 }, { "checksum", Checksum }, { "appliedAt", DateTime.UtcNow } };
        try { await Versions.InsertOneAsync(record, cancellationToken: ct); }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { }
        if (!await IsReadyAsync(ct)) throw new InvalidOperationException("Baseline MongoDB incompatível. Não foi sobrescrita.");
    }
}
