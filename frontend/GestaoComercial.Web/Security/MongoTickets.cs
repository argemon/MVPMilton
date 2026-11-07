using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Contracts;
using GestaoComercial.Web.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Web.Security;

public sealed class MongoTickets(IMongoClient client, MongoConfiguration configuration, IDataProtectionProvider protection) : ITicketStore
{
    public const string Scheme = "WebCookie", Claim = "ticket_id";
    private IMongoDatabase Db => client.GetDatabase(configuration.DatabaseName);
    private IMongoCollection<BsonDocument> Collection => Db.GetCollection<BsonDocument>("tickets");
    private IDataProtector Protector => protection.CreateProtector("GestaoComercial.Web.Tickets.v1");
    private static byte[] Hash(string key) => SHA256.HashData(Encoding.UTF8.GetBytes(key));
    private static BsonDocument Filter(string key) => new("handleHash", new BsonBinaryData(Hash(key)));
    private static BsonDocument Validator => BsonDocument.Parse("""
        {"$jsonSchema":{"bsonType":"object","additionalProperties":false,"required":["_id","schemaVersion","handleHash","ticketProtegido","tokenApiProtegido","expiraEm","createdAt"],"properties":{
        "_id":{"bsonType":"binData"},"schemaVersion":{"enum":[1]},"handleHash":{"bsonType":"binData"},"ticketProtegido":{"bsonType":"binData"},"tokenApiProtegido":{"bsonType":"binData"},"expiraEm":{"bsonType":"date"},"createdAt":{"bsonType":"date"}}}}
        """);
    private static string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Validator.ToJson() + ":ux_tickets_handle:unique:ix_tickets_expira:ascending:v1")));
    public static ClaimsPrincipal Principal(UsuarioAutenticado u, string key) => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Nome), new Claim(ClaimTypes.Role, u.Perfil),
        new Claim("troca", u.DeveAlterarSenha ? "sim" : "nao"), new Claim(Claim, key)
    }, Scheme));
    public async Task<ClaimsPrincipal> CriarAsync(string token, SessaoResponse session, CancellationToken ct)
    {
        var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); var principal = Principal(session.Usuario, key);
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = session.ExpiraEm, IsPersistent = false, AllowRefresh = false }, Scheme);
        await Collection.InsertOneAsync(new BsonDocument {
            { "_id", new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard) }, { "schemaVersion", 1 }, { "handleHash", new BsonBinaryData(Hash(key)) },
            { "ticketProtegido", new BsonBinaryData(Protector.Protect(TicketSerializer.Default.Serialize(ticket))) },
            { "tokenApiProtegido", new BsonBinaryData(Protector.Protect(Encoding.UTF8.GetBytes(token))) }, { "expiraEm", session.ExpiraEm.UtcDateTime }, { "createdAt", DateTime.UtcNow }
        }, cancellationToken: ct);
        return principal;
    }
    public async Task<string?> TokenAsync(string key, CancellationToken ct)
    {
        if (key.Length != 43) return null;
        var f = Filter(key).Add("expiraEm", new BsonDocument("$gt", DateTime.UtcNow));
        var r = await Collection.Find(f).SingleOrDefaultAsync(ct); if (r is null) return null;
        try { return Encoding.UTF8.GetString(Protector.Unprotect(r["tokenApiProtegido"].AsByteArray)); }
        catch (CryptographicException) { return null; }
    }
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = ticket.Principal.FindFirstValue(Claim) ?? throw new InvalidOperationException("Ticket de acesso ausente.");
        await RenewAsync(key, ticket); return key;
    }
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var changed = await Collection.UpdateOneAsync(Filter(key), new BsonDocument("$set", new BsonDocument("ticketProtegido", new BsonBinaryData(Protector.Protect(TicketSerializer.Default.Serialize(ticket))))));
        if (changed.MatchedCount != 1) throw new InvalidOperationException("Ticket expirado.");
    }
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (key.Length != 43) return null;
        var r = await Collection.Find(Filter(key).Add("expiraEm", new BsonDocument("$gt", DateTime.UtcNow))).SingleOrDefaultAsync();
        if (r is null) return null;
        try { return TicketSerializer.Default.Deserialize(Protector.Unprotect(r["ticketProtegido"].AsByteArray)); }
        catch (CryptographicException) { return null; }
    }
    public Task RemoveAsync(string key) => Collection.DeleteOneAsync(Filter(key));
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        try { var r = await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id", "01-web-tickets")).SingleOrDefaultAsync(ct); return r is not null && r.GetValue("version", 0) == 8 && r.GetValue("checksum", "") == Checksum; }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await new MongoFoundation(client, configuration).IsReadyAsync(ct)) throw new InvalidOperationException("Fundação Web ausente.");
        var validator = Validator;
        using var names = await Db.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = new BsonDocument("name", "tickets") }, ct);
        if (!(await names.ToListAsync(ct)).Contains("tickets"))
            try { await Db.CreateCollectionAsync("tickets", new CreateCollectionOptions<BsonDocument> { Validator = validator, ValidationAction = DocumentValidationAction.Error, ValidationLevel = DocumentValidationLevel.Strict }, ct); }
            catch (MongoCommandException e) when (e.Code == 48) { }
        using var collections = await Db.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", "tickets") }, ct);
        var options = (await collections.FirstAsync(ct))["options"].AsBsonDocument;
        if (options.GetValue("validator", new BsonDocument()) != validator || options.GetValue("validationLevel", "") != "strict" || options.GetValue("validationAction", "") != "error") throw new InvalidOperationException("Schema de tickets incompatível.");
        await Collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("handleHash", 1), new CreateIndexOptions { Name = "ux_tickets_handle", Unique = true }), cancellationToken: ct);
        await Collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("expiraEm", 1), new CreateIndexOptions { Name = "ix_tickets_expira" }), cancellationToken: ct);
        await Db.GetCollection<BsonDocument>("persistence_versions").UpdateOneAsync(new BsonDocument{{"_id","01-web-tickets"},{"version",2},{"checksum",Checksum}},new BsonDocument("$set",new BsonDocument("version",8)),cancellationToken:ct);
        try { await Db.GetCollection<BsonDocument>("persistence_versions").InsertOneAsync(new BsonDocument { { "_id", "01-web-tickets" }, { "schemaVersion", 1 }, { "version", 8 }, { "checksum", Checksum }, { "appliedAt", DateTime.UtcNow } }, cancellationToken: ct); }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { }
        if (!await IsReadyAsync(ct)) throw new InvalidOperationException("Evolução de tickets incompatível.");
    }
}
