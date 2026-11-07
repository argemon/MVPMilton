using GestaoComercial.Web.Security;
using GestaoComercial.Web;
using GestaoComercial.Web.Persistence;
using GestaoComercial.Contracts;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using GestaoComercial.Hosting;
using Microsoft.AspNetCore.DataProtection;
using GestaoComercial.Web.Components;
using GestaoComercial.Web.ApiClients;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
// Do not let the default exception logger serialize connection exceptions with secrets.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.None);
builder.Logging.AddFilter("MongoDB", LogLevel.None);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
builder.Logging.AddJsonConsole();
LocalEnvironment.AddDevelopmentFile(builder.Configuration, builder.Environment.EnvironmentName, builder.Environment.ContentRootPath);
builder.Configuration.AddCommandLine(args);
if (args.Contains("--initialize-persistence"))
{
    var evolutionConnection = builder.Configuration["Evolution:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(evolutionConnection)) builder.Configuration["Mongo:ConnectionString"] = evolutionConnection;
    else if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing")) throw new InvalidOperationException("Configuração obrigatória: Evolution:ConnectionString para inicialização explícita.");
}
var mongoConfiguration = Configuracao.Mongo(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddSingleton(mongoConfiguration);
builder.Services.AddSingleton<IMongoClient>(_ => mongoConfiguration.CreateClient());
builder.Services.AddSingleton<MongoFoundation>();
builder.Services.AddExceptionHandler<ErroSeguro>();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = c => { c.ProblemDetails.Extensions["traceId"] = c.HttpContext.TraceIdentifier; c.ProblemDetails.Extensions["code"] = "HTTP_" + c.HttpContext.Response.StatusCode; });
builder.Services.AddContasHttp();
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("login-web", c => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection().SetApplicationName("GestaoComercial.Web").PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, ".local", "keys")));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient<EstadoApiClient>(c => { c.BaseAddress = new Uri(builder.Configuration["Api:HealthBaseUrl"] ?? builder.Configuration["Api:BaseUrl"]!); c.Timeout = TimeSpan.FromSeconds(10); }).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new SocketsHttpHandler();
    var ca = builder.Configuration["Api:CaCertificatePath"];
    if (!string.IsNullOrWhiteSpace(ca))
    {
        var policy = new System.Security.Cryptography.X509Certificates.X509ChainPolicy
        {
            TrustMode = System.Security.Cryptography.X509Certificates.X509ChainTrustMode.CustomRootTrust,
            RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
        };
        policy.CustomTrustStore.Add(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(ca));
        handler.SslOptions.CertificateChainPolicy = policy;
    }
    return handler;
});
builder.Services.AddHttpClient<AcessoApiClient>(c => { c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!); c.Timeout = TimeSpan.FromSeconds(10); }).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new SocketsHttpHandler { UseCookies = false };
    var ca = builder.Configuration["Api:CaCertificatePath"];
    if (!string.IsNullOrWhiteSpace(ca))
    {
        var policy = new System.Security.Cryptography.X509Certificates.X509ChainPolicy
        {
            TrustMode = System.Security.Cryptography.X509Certificates.X509ChainTrustMode.CustomRootTrust,
            RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
        };
        policy.CustomTrustStore.Add(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(ca));
        handler.SslOptions.CertificateChainPolicy = policy;
    }
    return handler;
});
var app = builder.Build();
if (args.Contains("--probe-mongo") || args.Contains("--initialize-persistence"))
{
    try
    {
        var foundation = app.Services.GetRequiredService<MongoFoundation>();
        if (args.Contains("--initialize-persistence")) { await foundation.InitializeAsync(CancellationToken.None); await app.Services.GetRequiredService<MongoTickets>().InitializeAsync(CancellationToken.None); }
        if (!await foundation.SupportsTransactionsAsync(CancellationToken.None)) throw new InvalidOperationException();
        Console.WriteLine("MongoDB: conexão e topologia transacional disponíveis; origem " + (mongoConfiguration.IsAtlas ? "Atlas/SRV" : "servidor configurado") + ".");
    }
    catch (Exception e) { Console.Error.WriteLine("MongoDB: verificação indisponível (" + e.GetType().Name + "). Nenhuma credencial exibida."); Environment.ExitCode = 2; }
    await app.DisposeAsync();
    return;
}
var internalHealthPort = builder.Configuration.GetValue<int?>("Health:InternalPort") ?? 8082;
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' wss:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    // Readiness is served only by the private listener, never by the browser listener.
    if (context.Request.Path == "/health/ready" && context.Connection.LocalPort != internalHealthPort) { context.Response.StatusCode = 404; return; }
    if (context.Connection.LocalPort == internalHealthPort && context.Request.Path != "/health/ready") { context.Response.StatusCode = 404; return; }
    await next(context);
});
if (!app.Environment.IsEnvironment("Testing")) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStatusCodePages(async c => await Results.Problem(statusCode: c.HttpContext.Response.StatusCode, title: "Solicitação não atendida", extensions: new Dictionary<string, object?> { ["code"] = "HTTP_" + c.HttpContext.Response.StatusCode, ["traceId"] = c.HttpContext.TraceIdentifier }).ExecuteAsync(c.HttpContext));
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/_blazor") && context.Request.Headers.Origin is { Count: > 0 } origin && !string.Equals(origin.ToString(), context.Request.Scheme + "://" + context.Request.Host, StringComparison.OrdinalIgnoreCase)) { context.Response.StatusCode = 403; return; }
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapContas();
app.MapDownloadsRelatorios();
app.UseStaticFiles();
app.MapGet("/health/live", () => Results.Ok(new HealthResponse("ok"))).AllowAnonymous();
app.MapGet("/health/ready", async (MongoFoundation mongo, MongoTickets tickets, CancellationToken ct) =>
{
    var ready = await mongo.IsReadyAsync(ct) && await tickets.IsReadyAsync(ct);
    return Results.Json(new HealthResponse(ready ? "ready" : "unavailable"), statusCode: ready ? 200 : 503);
}).AllowAnonymous();
app.UseAntiforgery();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
public partial class Program;
