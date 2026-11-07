extern alias ApiHost;
extern alias WebHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed record LoginTeste(string AccessToken, DateTimeOffset ExpiraEm, UsuarioAutenticado Usuario);
public sealed class AuthClock : TimeProvider
{
    public TimeSpan Offset { get; set; }
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + Offset;
}
public sealed class AuthFixture : IAsyncLifetime
{
    public MongoFixture Mongo { get; } = new();
    public WebApplicationFactory<ApiHost::Program> Api { get; private set; } = null!;
    public AuthClock Clock { get; } = new();
    public IMongoCollection<UsuarioIdentity> Users => Mongo.Client.GetDatabase(Mongo.Name).GetCollection<UsuarioIdentity>("users");
    public static string Password => "Sintetica-Teste!725";
    public async Task InitializeAsync()
    {
        await Mongo.InitializeAsync();
        Api = new WebApplicationFactory<ApiHost::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing"); b.UseSetting("Mongo:ConnectionString", Mongo.Connection); b.UseSetting("Mongo:Database", Mongo.Name);
            b.ConfigureServices(s => s.AddSingleton<TimeProvider>(Clock));
        });
        await Mongo.Foundation.InitializeAsync(default);
        await Api.Services.GetRequiredService<AuthSchema>().InitializeAsync(default);
    }
    public async Task DisposeAsync() { await Api.DisposeAsync(); await Mongo.DisposeAsync(); }
    public async Task<UsuarioIdentity> SeedAsync(string perfil = "Vendedor", bool ativo = true, bool mustChange = false)
    {
        var u = new UsuarioIdentity { Nome = "Pessoa Sintética", NomeNormalizado = "PESSOA SINTÉTICA", Email = Guid.NewGuid().ToString("N") + "@example.test", Perfil = perfil, Ativo = ativo, DeveAlterarSenha = mustChange, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        u.EmailNormalizado = u.Email.ToUpperInvariant(); u.PasswordHash = new PasswordHasher<UsuarioIdentity>().HashPassword(u, Password);
        await Users.InsertOneAsync(u); return u;
    }
    public HttpClient Client(string? token = null)
    {
        var c = Api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (token is not null) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }
    public async Task<LoginTeste> LoginAsync(UsuarioIdentity u, string? password = null)
    {
        using var c = Client(); using var r = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = u.Email, senha = password ?? Password });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<LoginTeste>())!;
    }
}
public sealed class AuthenticationTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    [Fact]
    public Task ValidLoginPersistsHashOnlyAndMeDoesNotRefreshIdle() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(); var login = await f.LoginAsync(u); Assert.Equal(u.Id, login.Usuario.Id); Assert.Equal(43, login.AccessToken.Length);
        var sessions = f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("api_sessions");
        var filter = new BsonDocument("usuarioId", new BsonBinaryData(u.Id, GuidRepresentation.Standard));
        var before = await sessions.Find(filter).SingleAsync();
        Assert.Equal(32, before["tokenHash"].AsByteArray.Length); Assert.DoesNotContain(login.AccessToken, before.ToJson());
        using var c = f.Client(login.AccessToken); Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        var after = await sessions.Find(filter).SingleAsync(); Assert.Equal(before["ultimoUsoEm"], after["ultimoUsoEm"]);
        Assert.NotEqual(AuthFixture.Password, (await f.Users.Find(x => x.Id == u.Id).SingleAsync()).PasswordHash);
    });
    [Theory]
    [InlineData("wrong")]
    [InlineData("missing")]
    [InlineData("inactive")]
    public Task InvalidCredentialsAreIndistinguishable(string mode) => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(ativo: mode != "inactive"); using var c = f.Client();
        using var r = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = mode == "missing" ? Guid.NewGuid() + "@example.test" : u.Email, senha = mode == "wrong" ? "Incorreta-Teste!99" : AuthFixture.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("CREDENCIAIS_INVALIDAS", body); Assert.DoesNotContain(u.Email, body); Assert.DoesNotContain(AuthFixture.Password, body);
    });
    [Fact]
    public Task LogoutRevokesOnlyCurrentSession() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(); var a = await f.LoginAsync(u); var b = await f.LoginAsync(u);
        using var ca = f.Client(a.AccessToken); using var cb = f.Client(b.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await ca.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ca.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cb.GetAsync("/api/v1/auth/me")).StatusCode);
    });
    [Theory]
    [InlineData("Administrador", 200)]
    [InlineData("Vendedor", 403)]
    [InlineData("Estoquista", 403)]
    public Task BackendEnforcesCurrentRole(string role, int expected) => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(role); var l = await f.LoginAsync(u); using var c = f.Client(l.AccessToken);
        Assert.Equal(expected, (int)(await c.GetAsync("/api/v1/usuarios")).StatusCode);
        using var anon = f.Client(); Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/v1/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.NotEqual(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/v1/usuarios", new { })).StatusCode);
    });
    [Fact]
    public Task UniqueLoginIncludesInactiveAccountsAndInvalidProfilesAreRejected() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(ativo: false); u.Id = Guid.NewGuid();
        var e = await Assert.ThrowsAsync<MongoWriteException>(() => f.Users.InsertOneAsync(u)); Assert.Equal(ServerErrorCategory.DuplicateKey, e.WriteError.Category);
        u.EmailNormalizado = Guid.NewGuid().ToString(); u.Perfil = "Gerente";
        var v = await Assert.ThrowsAsync<MongoWriteException>(() => f.Users.InsertOneAsync(u)); Assert.Equal(121, v.WriteError.Code);
    });
    [Fact]
    public Task FiveFailedAttemptsLockAccountAndCounterIsNotLost() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(); using var c = f.Client();
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = u.Email, senha = "Incorreta-Teste!99" })).StatusCode);
        var locked = await f.Users.Find(x => x.Id == u.Id).SingleAsync(); Assert.True(locked.LockoutEnd > DateTime.UtcNow.AddMinutes(14));
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = u.Email, senha = AuthFixture.Password })).StatusCode);
    });
    [Fact]
    public Task PasswordChangeIsMandatoryAndRevokesEverySession() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync("Administrador", mustChange: true); var a = await f.LoginAsync(u); var b = await f.LoginAsync(u);
        using var ca = f.Client(a.AccessToken); using var cb = f.Client(b.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await ca.GetAsync("/api/v1/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ca.PostAsJsonAsync("/api/v1/auth/alterar-senha", new { senhaAtual = AuthFixture.Password, novaSenha = "Nova-Sintetica!624" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cb.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ca.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.False((await f.LoginAsync(u, "Nova-Sintetica!624")).Usuario.DeveAlterarSenha);
    });
    [Fact]
    public Task ProfileChangeResetAndOptimisticVersionAreAtomic() => f.Mongo.SafeAsync(async () =>
    {
        var admin = await f.SeedAsync("Administrador"); var u = await f.SeedAsync(); var a = await f.LoginAsync(admin); var l = await f.LoginAsync(u);
        using var c = f.Client(a.AccessToken); using var current = f.Client(l.AccessToken);
        var row = (await c.GetFromJsonAsync<UsuarioResponse>($"/api/v1/usuarios/{u.Id}"))!;
        using var changed = await c.PutAsJsonAsync($"/api/v1/usuarios/{u.Id}", new AtualizarUsuarioRequest("Estoquista", true, row.Versao)); Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await current.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v1/usuarios/{u.Id}", new AtualizarUsuarioRequest("Vendedor", true, row.Versao))).StatusCode);
        row = (await changed.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync($"/api/v1/usuarios/{u.Id}/redefinir-senha", new { senhaTemporaria = "Temporaria-Nova!34", versaoEsperada = row.Versao })).StatusCode);
        var reset = await f.LoginAsync(u, "Temporaria-Nova!34"); Assert.True(reset.Usuario.DeveAlterarSenha); Assert.Equal("Estoquista", reset.Usuario.Perfil);
    });
    [Fact]
    public Task IdleAndAbsoluteExpirationFailClosedWithControlledClock() => f.Mongo.SafeAsync(async () =>
    {
        try
        {
            var u = await f.SeedAsync(); var l = await f.LoginAsync(u); using var c = f.Client(l.AccessToken);
            f.Clock.Offset = TimeSpan.FromMinutes(31); Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
            f.Clock.Offset = TimeSpan.FromHours(9); Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/auth/me")).StatusCode);
        }
        finally { f.Clock.Offset = TimeSpan.Zero; }
    });
    [Fact]
    public Task IdentityStoreNormalizesEmailAndRejectsStaleWrites() => f.Mongo.SafeAsync(async () =>
    {
        using var scope = f.Api.Services.CreateScope(); var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var email = "Pessoa-" + Guid.NewGuid().ToString("N") + "@Example.Test";
        var u = new UsuarioIdentity { Nome = "Conta Identity", NomeNormalizado = "CONTA IDENTITY", Email = email, Perfil = "Vendedor", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        Assert.True((await manager.CreateAsync(u, AuthFixture.Password)).Succeeded);
        Assert.Equal(email.ToUpperInvariant(), (await f.Users.Find(x => x.Id == u.Id).SingleAsync()).EmailNormalizado);
        var duplicate = new UsuarioIdentity { Nome = "Conta Duplicada", NomeNormalizado = "CONTA DUPLICADA", Email = email.ToLowerInvariant(), Perfil = "Vendedor", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        Assert.False((await manager.CreateAsync(duplicate, AuthFixture.Password)).Succeeded);
        using var other = f.Api.Services.CreateScope(); var first = scope.ServiceProvider.GetRequiredService<MongoUserStore>(); var second = other.ServiceProvider.GetRequiredService<MongoUserStore>();
        var a = (await first.FindByIdAsync(u.Id.ToString(), default))!; var b = (await second.FindByIdAsync(u.Id.ToString(), default))!;
        a.Nome = "Primeira versão"; b.Nome = "Versão obsoleta"; Assert.True((await first.UpdateAsync(a, default)).Succeeded);
        var failed = await second.UpdateAsync(b, default); Assert.False(failed.Succeeded); Assert.Contains(failed.Errors, x => x.Code == "ConcurrencyFailure");
        Assert.Equal("Primeira versão", (await f.Users.Find(x => x.Id == u.Id).SingleAsync()).Nome);
    });
    [Fact]
    public Task ConcurrentFailuresPreserveBothAttempts() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(); using var a = f.Client(); using var b = f.Client();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Attempt(HttpClient c) { await gate.Task; using var r = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = u.Email, senha = "Errada-Sintetica!54" }); return r.StatusCode; }
        var first = Attempt(a); var second = Attempt(b); gate.SetResult();
        Assert.All(await Task.WhenAll(first, second), s => Assert.Equal(HttpStatusCode.Unauthorized, s));
        Assert.Equal(2, (await f.Users.Find(x => x.Id == u.Id).SingleAsync()).FailedAccessCount);
    });
    [Fact]
    public Task LoginIdentifierRateLimitReturns429() => f.Mongo.SafeAsync(async () =>
    {
        using var c = f.Client(); var email = Guid.NewGuid() + "@example.test";
        for (var i = 0; i < 10; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = AuthFixture.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = AuthFixture.Password })).StatusCode);
    });
    [Fact]
    public Task DisablingAccountRevokesCurrentSessions() => f.Mongo.SafeAsync(async () =>
    {
        var admin = await f.SeedAsync("Administrador"); var u = await f.SeedAsync(); var a = await f.LoginAsync(admin); var l = await f.LoginAsync(u);
        using var c = f.Client(a.AccessToken); using var session = f.Client(l.AccessToken);
        var row = (await c.GetFromJsonAsync<UsuarioResponse>($"/api/v1/usuarios/{u.Id}"))!;
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/v1/usuarios/{u.Id}", new AtualizarUsuarioRequest("Vendedor", false, row.Versao))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/v1/auth/me")).StatusCode);
    });
    [Fact]
    public Task SchemaUpgradeIsIdempotentPreservesUsersAndRejectsDivergence() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.SeedAsync(); var schema = f.Api.Services.GetRequiredService<AuthSchema>();
        await schema.InitializeAsync(default); await schema.InitializeAsync(default); Assert.True(await schema.IsReadyAsync(default));
        Assert.NotNull(await f.Users.Find(x => x.Id == u.Id).SingleOrDefaultAsync());
        using var indexes = await f.Users.Indexes.ListAsync(); Assert.Contains(await indexes.ToListAsync(), i => i["name"] == "ux_users_email" && i["unique"].AsBoolean);
        var catalog = f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("persistence_versions"); var filter = new BsonDocument("_id", AuthSchema.Id); var original = await catalog.Find(filter).SingleAsync();
        try
        {
            await catalog.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("checksum", new string('0', 64)));
            Assert.False(await schema.IsReadyAsync(default)); await Assert.ThrowsAsync<InvalidOperationException>(() => schema.InitializeAsync(default));
            Assert.Equal(new string('0', 64), (await catalog.Find(filter).SingleAsync())["checksum"].AsString);
        }
        finally { await catalog.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("checksum", original["checksum"])); }
    });
}
