using GestaoComercial.Api.Acesso;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Infrastructure.Identity;
using GestaoComercial.Api;
using GestaoComercial.Infrastructure.Persistence;
using GestaoComercial.Contracts;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using GestaoComercial.Hosting;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddAcesso();
builder.Services.AddAutenticacaoHttp();
builder.Services.AddExceptionHandler<ErroSeguro>();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = c => { c.ProblemDetails.Extensions.TryAdd("traceId", c.HttpContext.TraceIdentifier); c.ProblemDetails.Extensions.TryAdd("code", "HTTP_" + c.HttpContext.Response.StatusCode); });

builder.Services.AddOpenApi();
var app = builder.Build();
if (args.Contains("--bootstrap-admin"))
{
    try
    {
        using var scope = app.Services.CreateScope(); var config = builder.Configuration;
        var name = config["Bootstrap:Nome"] ?? ""; var email = config["Bootstrap:Email"] ?? ""; var password = config["Bootstrap:Senha"] ?? "";
        if (name.Length == 0 || email.Length == 0 || password.Length == 0) throw new InvalidOperationException();
        var created = await scope.ServiceProvider.GetRequiredService<IAutenticacao>().ProvisionarAsync(name, email, password, CancellationToken.None);
        Console.WriteLine(created ? "Administrador provisionado; troca de senha obrigatória." : "Administrador existente; nenhuma credencial alterada.");
    }
    catch (Exception e) { Console.Error.WriteLine("Provisionamento não concluído (" + e.GetType().Name + "). Verifique configuração externa e evolução01."); Environment.ExitCode = 2; }
    await app.DisposeAsync(); return;
}
if (args.Contains("--probe-mongo") || args.Contains("--initialize-persistence"))
{
    try
    {
        var foundation = app.Services.GetRequiredService<MongoFoundation>();
        if (args.Contains("--initialize-persistence")) { await foundation.InitializeAsync(CancellationToken.None); await app.Services.GetRequiredService<AuthSchema>().InitializeAsync(CancellationToken.None); }
        if (!await foundation.SupportsTransactionsAsync(CancellationToken.None)) throw new InvalidOperationException();
        Console.WriteLine("MongoDB: conexão e topologia transacional disponíveis; origem " + (mongoConfiguration.IsAtlas ? "Atlas/SRV" : "servidor configurado") + ".");
    }
    catch (Exception e) { Console.Error.WriteLine("MongoDB: verificação indisponível (" + e.GetType().Name + "). Nenhuma credencial exibida."); Environment.ExitCode = 2; }
    await app.DisposeAsync();
    return;
}
var internalHealthPort = builder.Configuration.GetValue<int?>("Health:InternalPort") ?? 8081;
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
app.UseStatusCodePages(async c => await Results.Problem(statusCode: c.HttpContext.Response.StatusCode, title: "Solicitação não atendida", extensions: new Dictionary<string, object?> { ["code"] = c.HttpContext.Response.StatusCode == 403 && c.HttpContext.User.HasClaim("troca", "sim") ? "TROCA_SENHA_OBRIGATORIA" : "HTTP_" + c.HttpContext.Response.StatusCode, ["traceId"] = c.HttpContext.TraceIdentifier }).ExecuteAsync(c.HttpContext));
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Items.ContainsKey(typeof(SessaoAtual)) && context.Request.Path.StartsWithSegments("/api/v1/usuarios"))
    {
        var auth = context.RequestServices.GetRequiredService<IAutenticacao>();
        if (await auth.ValidarAsync(context.Request.Headers.Authorization.ToString()[7..], true, context.RequestAborted) is not { } session) { context.Response.StatusCode = 401; return; }
        context.Items[typeof(SessaoAtual)] = session;
    }
    await next(context);
});
app.MapAcesso();
app.MapGet("/health/live", () => Results.Ok(new HealthResponse("ok"))).AllowAnonymous();
app.MapGet("/health/ready", async (MongoFoundation mongo, AuthSchema auth, CancellationToken ct) =>
{
    var ready = await mongo.IsReadyAsync(ct) && await auth.IsReadyAsync(ct);
    return Results.Json(new HealthResponse(ready ? "ready" : "unavailable"), statusCode: ready ? 200 : 503);
}).AllowAnonymous();
if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();
app.Run();
public partial class Program;
