extern alias WebHost;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Tickets = WebHost::GestaoComercial.Web.Security.MongoTickets;
using ApiClient = WebHost::GestaoComercial.Web.ApiClients.AcessoApiClient;

public sealed class BffFixture : IAsyncLifetime
{
    public AuthFixture Backend { get; } = new();
    public MongoFixture Mongo { get; } = new();
    public bool Offline { get; set; }
    public WebApplicationFactory<WebHost::Program> Web { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        await Backend.InitializeAsync(); await Mongo.InitializeAsync();
        Web = new WebApplicationFactory<WebHost::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing"); b.UseSetting("Mongo:ConnectionString", Mongo.Connection); b.UseSetting("Mongo:Database", Mongo.Name); b.UseSetting("Api:BaseUrl", "https://api.test/");
            b.UseSetting("DataProtection:KeysPath", Path.Combine(Path.GetTempPath(), "gc_auth_keys_" + Mongo.Name));
            b.ConfigureServices(s => s.AddHttpClient<ApiClient>().ConfigurePrimaryHttpMessageHandler(() => new BridgeHandler(Backend.Api.Server.CreateHandler(), () => Offline)));
        });
        await Web.Services.GetRequiredService<WebHost::GestaoComercial.Web.Persistence.MongoFoundation>().InitializeAsync(default);
        await Web.Services.GetRequiredService<Tickets>().InitializeAsync(default);
    }
    public async Task DisposeAsync() { await Web.DisposeAsync(); await Mongo.DisposeAsync(); await Backend.DisposeAsync(); }
    public HttpClient Client() => Web.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://web.test/"), AllowAutoRedirect = false, HandleCookies = true });
    private sealed class BridgeHandler(HttpMessageHandler inner, Func<bool> offline) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { if (offline()) throw new HttpRequestException("Conexão indisponível."); return base.SendAsync(request, ct); }
    }
    public static string Csrf(string html)
    {
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""); Assert.True(m.Success, "Formulário com token antiforgery esperado."); return WebUtility.HtmlDecode(m.Groups[1].Value);
    }
    public static Task<HttpResponseMessage> Form(HttpClient c, string path, string csrf, params (string, string)[] values)
        => c.PostAsync(path, new FormUrlEncodedContent(values.Append(("__RequestVerificationToken", csrf)).Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2))));
    public async Task<HttpResponseMessage> Login(HttpClient c, string email, string password, string returnUrl = "/")
    {
        var page = await c.GetStringAsync("/conta/entrar");
        return await Form(c, "/conta/entrar", Csrf(page), ("email", email), ("senha", password), ("returnUrl", returnUrl));
    }
}
public sealed class BffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact]
    public Task RealBffLoginCookieSessionCsrfLogoutAndNoTokenInHtml() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.Backend.SeedAsync("Administrador"); using var c = f.Client();
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/")).StatusCode);
        using var login = await f.Login(c, u.Email, AuthFixture.Password, "https://externo.test/");
        Assert.Equal(HttpStatusCode.SeeOther, login.StatusCode); Assert.Equal("/", login.Headers.Location?.ToString());
        var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-GestaoComercial="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
        var html = await c.GetStringAsync("/"); Assert.Contains("Pessoa Sintética", WebUtility.HtmlDecode(html)); Assert.Contains("Gerenciar usuários", WebUtility.HtmlDecode(html)); Assert.DoesNotContain("accessToken", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenApi", html, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain(AuthFixture.Password, html);
        var tickets = f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("tickets");
        Assert.True(await tickets.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty) > 0);
        using var forged = await c.PostAsync("/conta/sair", new FormUrlEncodedContent([])); Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/")).StatusCode);
        using var logout = await BffFixture.Form(c, "/conta/sair", BffFixture.Csrf(html)); Assert.Equal(HttpStatusCode.SeeOther, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/")).StatusCode);
    });
    [Fact]
    public Task MandatoryPasswordChangeUsesHttpAndRequiresNewLogin() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.Backend.SeedAsync("Administrador", mustChange: true); using var c = f.Client();
        using var login = await f.Login(c, u.Email, AuthFixture.Password); Assert.Equal("/conta/senha", login.Headers.Location?.ToString());
        var page = await c.GetStringAsync("/conta/senha");
        using var changed = await BffFixture.Form(c, "/conta/senha", BffFixture.Csrf(page), ("senhaAtual", AuthFixture.Password), ("novaSenha", "Senha-Trocada!463"));
        Assert.Equal(HttpStatusCode.SeeOther, changed.StatusCode); Assert.Contains("alterada=1", changed.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/")).StatusCode);
        using var again = await f.Login(c, u.Email, "Senha-Trocada!463"); Assert.Equal("/", again.Headers.Location?.ToString());
    });
    [Fact]
    public Task TwoUsersAreIsolatedAndSellerCannotOpenAdministration() => f.Mongo.SafeAsync(async () =>
    {
        var admin = await f.Backend.SeedAsync("Administrador"); var seller = await f.Backend.SeedAsync();
        using var a = f.Client(); using var b = f.Client();
        using var la = await f.Login(a, admin.Email, AuthFixture.Password); using var lb = await f.Login(b, seller.Email, AuthFixture.Password);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/usuarios")).StatusCode);
        using var denied = await b.GetAsync("/usuarios"); Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode); Assert.Contains("acesso-negado", denied.Headers.Location?.ToString());
        Assert.Contains("Vendedor", await b.GetStringAsync("/")); Assert.Contains("Administrador", await a.GetStringAsync("/"));
    });
    [Fact]
    public Task InvalidLoginReturnsGenericFormWithoutPasswordAndOfflineFailsClosed() => f.Mongo.SafeAsync(async () =>
    {
        using var c = f.Client(); using var invalid = await f.Login(c, "missing@example.test", AuthFixture.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode); var html = await invalid.Content.ReadAsStringAsync(); Assert.Contains("name=\"senha\"", html); Assert.DoesNotContain(AuthFixture.Password, html);
        f.Offline = true;
        try { using var unavailable = await f.Login(c, "missing@example.test", AuthFixture.Password); Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode); }
        finally { f.Offline = false; }
    });
    [Fact]
    public Task RevokedCookieCanLoginAgainAndRevalidationRejectsOldState() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.Backend.SeedAsync(); using var c = f.Client(); using var login = await f.Login(c, u.Email, AuthFixture.Password);
        var tickets = f.Web.Services.GetRequiredService<Tickets>();
        var docs = f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("tickets");
        // Obtain the matching principal only inside the server test; never output tokens or handles.
        var protector = f.Web.Services.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>().CreateProtector("GestaoComercial.Web.Tickets.v1");
        System.Security.Claims.ClaimsPrincipal? principal = null;
        foreach (var d in await docs.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
        {
            var ticket = Microsoft.AspNetCore.Authentication.TicketSerializer.Default.Deserialize(protector.Unprotect(d["ticketProtegido"].AsByteArray));
            if (ticket?.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == u.Id.ToString()) principal = ticket.Principal;
        }
        Assert.NotNull(principal);
        using var scope = f.Web.Services.CreateScope(); var provider = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>();
        var validate = provider.GetType().GetMethod("ValidateAuthenticationStateAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var state = new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(principal);
        Assert.True(await (Task<bool>)validate.Invoke(provider, [state, CancellationToken.None])!);
        await f.Backend.Users.UpdateOneAsync(x => x.Id == u.Id, MongoDB.Driver.Builders<GestaoComercial.Infrastructure.Identity.UsuarioIdentity>.Update.Set(x => x.SecurityStamp, Guid.NewGuid().ToString("N")));
        Assert.False(await (Task<bool>)validate.Invoke(provider, [state, CancellationToken.None])!);
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/")).StatusCode);
        using var again = await f.Login(c, u.Email, AuthFixture.Password); Assert.Equal(HttpStatusCode.SeeOther, again.StatusCode); Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/")).StatusCode);
    });
    [Fact]
    public async Task CrossOriginCircuitIsRejected()
    {
        using var c = f.Client(); using var request = new HttpRequestMessage(HttpMethod.Get, "/_blazor"); request.Headers.Add("Origin", "https://outro.example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.SendAsync(request)).StatusCode);
    }
    [Fact]
    public Task LogoutOfflineRemovesLocalTicketAndCookie() => f.Mongo.SafeAsync(async () =>
    {
        var u = await f.Backend.SeedAsync(); using var c = f.Client(); using var login = await f.Login(c, u.Email, AuthFixture.Password);
        var html = await c.GetStringAsync("/"); var tickets = f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("tickets"); var before = await tickets.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        f.Offline = true;
        try { using var logout = await BffFixture.Form(c, "/conta/sair", BffFixture.Csrf(html)); Assert.Equal(HttpStatusCode.SeeOther, logout.StatusCode); Assert.Equal(before - 1, await tickets.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)); }
        finally { f.Offline = false; }
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/")).StatusCode);
    });
}
