using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Identity;

public sealed class AuthSchema(IMongoClient client, MongoConfiguration configuration)
{
    private IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    public const string Id = "01-authentication";
    private static BsonDocument Type(string type) => new("bsonType", type);
    private static BsonDocument String(int min, int max) => new() { { "bsonType", "string" }, { "minLength", min }, { "maxLength", max } };
    private static BsonDocument Schema(BsonDocument properties) => new("$jsonSchema", new BsonDocument { { "bsonType", "object" }, { "additionalProperties", false }, { "required", new BsonArray(properties.Names) }, { "properties", properties } });
    private static BsonDocument User => Schema(new BsonDocument
    {
        { "_id", Type("binData") }, { "schemaVersion", new BsonDocument("enum", new BsonArray { 1 }) },
        { "nome", String(2,150) }, { "nomeNormalizado", String(2,150) }, { "email", String(3,254) }, { "emailNormalizado", String(3,254) },
        { "passwordHash", String(1,1024) }, { "perfil", new BsonDocument("enum", new BsonArray { "Administrador", "Vendedor", "Estoquista" }) },
        { "ativo", Type("bool") }, { "deveAlterarSenha", Type("bool") }, { "securityStamp", String(32,64) },
        { "failedAccessCount", new BsonDocument { { "bsonType", "int" }, { "minimum", 0 }, { "maximum", 5 } } },
        { "lockoutEnd", new BsonDocument("bsonType", new BsonArray { "date", "null" }) },
        { "employeeId", new BsonDocument("bsonType", new BsonArray { "binData", "null" }) },
        { "versao", new BsonDocument { { "bsonType", "long" }, { "minimum", 1 } } },
        { "guardVersion", new BsonDocument { { "bsonType", "long" }, { "minimum", 0 } } }, { "createdAt", Type("date") }, { "updatedAt", Type("date") }
    });
    private static BsonDocument Sessions => Schema(new BsonDocument
    {
        { "_id", Type("binData") }, { "schemaVersion", new BsonDocument("enum", new BsonArray { 1 }) }, { "usuarioId", Type("binData") },
        { "tokenHash", Type("binData") }, { "securityStamp", String(32,64) }, { "criadoEm", Type("date") }, { "expiraEm", Type("date") }, { "ultimoUsoEm", Type("date") },
        { "revogadaEm", new BsonDocument("bsonType", new BsonArray { "date", "null" }) }
    });
    private static BsonDocument Audit => Schema(new BsonDocument
    {
        { "_id", Type("binData") }, { "schemaVersion", new BsonDocument("enum", new BsonArray { 1 }) },
        { "atorId", new BsonDocument("bsonType", new BsonArray { "binData", "null" }) }, { "origem", new BsonDocument("enum", new BsonArray { "Autenticado", "Anonimo", "Bootstrap" }) },
        { "traceId", String(1,150) }, { "acao", String(1,50) }, { "recursoTipo", String(1,30) }, { "recursoId", new BsonDocument("bsonType", new BsonArray { "binData", "null" }) }, { "ocorridoEm", Type("date") }
    });
    private static BsonDocument Control => Schema(new BsonDocument { { "_id", new BsonDocument("enum", new BsonArray { "administration" }) }, { "guardVersion", new BsonDocument { { "bsonType", "long" }, { "minimum", 0 } } } });
    private static string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(User + Sessions.ToJson() + Audit.ToJson() + Control.ToJson() + ":emailUnique:nameId:tokenUnique:userExpires:auditResource:sessionExpires:v1")));
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        try { var r = await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id", Id)).FirstOrDefaultAsync(ct); return r is not null && r.GetValue("version", 0) == 2 && r.GetValue("checksum", "") == Checksum; }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await new MongoFoundation(client, configuration).IsReadyAsync(ct)) throw new InvalidOperationException("Fundação00 ausente.");
        foreach (var (name, validator) in new[] { ("users", User), ("api_sessions", Sessions), ("audit_events", Audit), ("administration_control", Control) })
        {
            using var names = await Db.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = new BsonDocument("name", name) }, ct);
            if (!(await names.ToListAsync(ct)).Contains(name))
                try { await Db.CreateCollectionAsync(name, new CreateCollectionOptions<BsonDocument> { Validator = validator, ValidationAction = DocumentValidationAction.Error, ValidationLevel = DocumentValidationLevel.Strict }, ct); }
                catch (MongoCommandException e) when (e.Code == 48) { }
            using var current = await Db.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", name) }, ct);
            var options = (await current.FirstAsync(ct))["options"].AsBsonDocument;
            if (options.GetValue("validator", new BsonDocument()) != validator || options.GetValue("validationLevel", "") != "strict" || options.GetValue("validationAction", "") != "error") throw new InvalidOperationException("Definição01 divergente; revisão operacional necessária.");
        }
        await IndexAsync("users", "ux_users_email", new BsonDocument("emailNormalizado", 1), true, ct);
        await IndexAsync("users", "ix_users_nome_id", new BsonDocument { { "nomeNormalizado", 1 }, { "_id", 1 } }, false, ct);
        await IndexAsync("api_sessions", "ux_sessions_token", new BsonDocument("tokenHash", 1), true, ct);
        await IndexAsync("api_sessions", "ix_sessions_usuario_expira", new BsonDocument { { "usuarioId", 1 }, { "expiraEm", 1 } }, false, ct);
        await IndexAsync("api_sessions", "ix_sessions_expira", new BsonDocument("expiraEm", 1), false, ct);
        await IndexAsync("audit_events", "ix_audit_recurso_data_id", new BsonDocument { { "recursoTipo", 1 }, { "recursoId", 1 }, { "ocorridoEm", 1 }, { "_id", 1 } }, false, ct);
        await Db.GetCollection<BsonDocument>("administration_control").UpdateOneAsync(new BsonDocument("_id", "administration"), new BsonDocument("$setOnInsert", new BsonDocument("guardVersion", 0L)), new UpdateOptions { IsUpsert = true }, ct);
        try { await Db.GetCollection<BsonDocument>("persistence_versions").InsertOneAsync(new BsonDocument { { "_id", Id }, { "schemaVersion", 1 }, { "version", 2 }, { "checksum", Checksum }, { "appliedAt", DateTime.UtcNow } }, cancellationToken: ct); }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { }
        if (!await IsReadyAsync(ct)) throw new InvalidOperationException("Evolução01 incompatível, não sobrescrita.");
    }
    private Task<string> IndexAsync(string collection, string name, BsonDocument keys, bool unique, CancellationToken ct) => Db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = name, Unique = unique }), cancellationToken: ct);
}
