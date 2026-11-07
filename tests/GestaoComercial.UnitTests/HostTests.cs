extern alias ApiHost;
extern alias WebHost;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ApiFactory : WebApplicationFactory<ApiHost::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Testing");
        b.UseSetting("Mongo:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=50&connectTimeoutMS=50");
        b.UseSetting("Mongo:Database", "gc_host_test");
        b.ConfigureServices(s => s.AddSingleton<IStartupFilter, TestFilter>());
    }
}
public sealed class WebFactory : WebApplicationFactory<WebHost::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Testing");
        b.UseSetting("Mongo:ConnectionString", "mongodb://127.0.0.1:1/");
        b.UseSetting("Mongo:Database", "gc_host_test");
        b.UseSetting("Api:BaseUrl", "https://127.0.0.1:1/");
    }
}
public sealed class TestFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (c, n) => { if (c.Request.Headers.ContainsKey("X-Test-Internal")) c.Connection.LocalPort = 8081; await n(c); });
        next(app);
        app.UseEndpoints(e => { e.MapGet("/denied-test", () => "never"); e.MapGet("/failure-test", (Func<string>)(() => throw new InvalidOperationException("secret-must-not-leak"))).AllowAnonymous(); });
        app.Use(async (c, n) => { if (c.Request.Path == "/failure-test") throw new InvalidOperationException("secret-must-not-leak"); await n(c); });
    };
}
public sealed class HostTests
{
    [Fact]
    public async Task LivePublicReadyPrivateAndFallbackDenied()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        Assert.Equal("{\"status\":\"ok\"}", await c.GetStringAsync("/health/live"));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/denied-test")).StatusCode);
    }
    [Fact]
    public async Task UnavailableMongoDoesNotAffectLiveness()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready"); request.Headers.Add("X-Test-Internal", "1");
        using var response = await c.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("{\"status\":\"unavailable\"}", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health/live")).StatusCode);
    }
    [Fact]
    public async Task ExceptionIsSafeProblemDetails()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient(); using var r = await c.GetAsync("/failure-test");
        var body = await r.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType); Assert.Contains("traceId", body); Assert.DoesNotContain("secret", body); Assert.DoesNotContain("InvalidOperationException", body);
    }
    [Fact]
    public async Task MissingConfigurationFailsAtStartupWithoutSecrets()
    {
        await using var f = new ApiFactory(); await using var missing = f.WithWebHostBuilder(b => b.UseSetting("Mongo:ConnectionString", ""));
        var error = Assert.Throws<InvalidOperationException>(() => missing.CreateClient()); Assert.Contains("Configuração", error.Message);
    }
    [Theory]
    [InlineData("/conta/entrar")]
    [InlineData("/erro")]
    [InlineData("/indisponivel")]
    public async Task TechnicalPagesArePublicAndSafe(string route)
    {
        await using var f = new WebFactory(); using var c = f.CreateClient(); var html = await c.GetStringAsync(route);
        Assert.Contains("pt-BR", html); Assert.Contains("viewport", html); Assert.DoesNotContain("mongodb://", html); Assert.DoesNotContain("mongodb+srv://", html);
        Assert.DoesNotContain("MONGODB_PASSWORD", html);
    }
    [Fact]
    public async Task OpenApiExposesCurrentContractsWithoutFutureModules()
    {
        await using var f = new ApiFactory(); await using var dev = f.WithWebHostBuilder(b => b.UseEnvironment("Development")); using var c = dev.CreateClient();
        using var doc = System.Text.Json.JsonDocument.Parse(await c.GetStringAsync("/openapi/v1.json")); var paths = doc.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/v1/auth/login").TryGetProperty("post", out _)); Assert.True(paths.GetProperty("/api/v1/auth/me").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/v1/usuarios").TryGetProperty("post", out _)); Assert.True(paths.TryGetProperty("/api/v1/funcionarios", out _));
        Assert.True(paths.TryGetProperty("/api/v1/clientes", out _)); Assert.True(paths.TryGetProperty("/api/v1/produtos", out _)); Assert.True(paths.TryGetProperty("/api/v1/estoque/saldos", out _));
        Assert.False(paths.TryGetProperty("/api/v1/vendas", out _));
    }
    [Fact]
    public async Task LoginFailsClosedWhenMongoIsUnavailable()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        using var r = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "teste@example.test", senha = "Sintetica-Teste!725" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode); var body = await r.Content.ReadAsStringAsync(); Assert.DoesNotContain("mongodb", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health/live")).StatusCode);
    }
    [Fact]
    public async Task CssIsServed()
    {
        await using var f = new WebFactory(); using var c = f.CreateClient(); Assert.Contains("focus-visible", await c.GetStringAsync("/app.css"));
    }
}
