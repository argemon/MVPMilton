using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class AuthAtomicityTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    [Fact]
    public Task AuditFailureRollsBackProfileStampAndSessionRevocation() => f.Mongo.SafeAsync(async () =>
    {
        var admin = await f.SeedAsync("Administrador"); var u = await f.SeedAsync(); var a = await f.LoginAsync(admin); var l = await f.LoginAsync(u);
        using var c = f.Client(a.AccessToken); using var current = f.Client(l.AccessToken); var row = (await c.GetFromJsonAsync<UsuarioResponse>($"/api/v1/usuarios/{u.Id}"))!;
        var db = f.Mongo.Client.GetDatabase(f.Mongo.Name);
        using var collections = await db.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", "audit_events") });
        var validator = (await collections.SingleAsync())["options"]["validator"].AsBsonDocument;
        try
        {
            await db.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", "audit_events" }, { "validator", new BsonDocument("$and", new BsonArray { validator, new BsonDocument("acao", new BsonDocument("$ne", "ContaAlterada")) }) } });
            using var failed = await c.PutAsJsonAsync($"/api/v1/usuarios/{u.Id}", new AtualizarUsuarioRequest("Estoquista", false, row.Versao));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            var after = await f.Users.Find(x => x.Id == u.Id).SingleAsync(); Assert.Equal("Vendedor", after.Perfil); Assert.True(after.Ativo); Assert.Equal(row.Versao, after.Versao); Assert.Equal(u.SecurityStamp, after.SecurityStamp);
            Assert.Equal(HttpStatusCode.OK, (await current.GetAsync("/api/v1/auth/me")).StatusCode);
        }
        finally { await db.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", "audit_events" }, { "validator", validator } }); }
    });
}
