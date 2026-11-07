using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
namespace GestaoComercial.Api.Acesso;

// These credential transports are host-private and are never rendered or logged.
internal record LoginInput(string? Email, string? Senha);
internal record PasswordInput(string? SenhaAtual, string? NovaSenha);
internal record ResetInput(string? SenhaTemporaria, long VersaoEsperada);
public sealed class IdentificadorThrottle : IDisposable
{
    private readonly MemoryCache _entries = new(new MemoryCacheOptions { SizeLimit = 10000 });
    private readonly object _gate = new();
    public bool Permitir(string email)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant())));
        lock (_gate)
        {
            var count = _entries.Get<int?>(key) ?? 0; if (count >= 10) return false;
            _entries.Set(key, count + 1, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1), Size = 1 });
            return true;
        }
    }
    public void Dispose() => _entries.Dispose();
}
public static class EndpointsAcesso
{
    public static IServiceCollection AddAutenticacaoHttp(this IServiceCollection services)
    {
        services.AddAuthentication("Sessao").AddScheme<AuthenticationSchemeOptions, SessaoHandler>("Sessao", _ => { });
        services.AddAuthorization(o =>
        {
            o.AddPolicy("Conta", new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("troca", "nao").Build();
            o.AddPolicy("Usuarios.Gerenciar", p => p.RequireAuthenticatedUser().RequireClaim("troca", "nao").RequireRole("Administrador"));
        });
        services.AddSingleton<IdentificadorThrottle>();
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        services.AddExceptionHandler<AcessoErro>();
        return services;
    }
    private static SessaoAtual Actor(HttpContext c) => (SessaoAtual)c.Items[typeof(SessaoAtual)]!;
    private static UsuarioAutenticado Public(UsuarioAcesso u) => new(u.Id, u.Nome, u.Perfil, u.DeveAlterarSenha);
    private static UsuarioResponse Public(UsuarioResumo u) => new(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, u.Versao, u.DeveAlterarSenha, u.FuncionarioId);
    public static void MapAcesso(this WebApplication app)
    {
        var auth = app.MapGroup("/api/v1/auth");
        auth.MapPost("/login", async (LoginInput input, IdentificadorThrottle throttle, IAutenticacao service, HttpContext c, CancellationToken ct) =>
        {
            c.Response.Headers.CacheControl = "no-store";
            if (input.Email is null || input.Senha is null || input.Email.Length > 254 || input.Senha.Length > 128 || !System.Net.Mail.MailAddress.TryCreate(input.Email.Trim(), out var address) || address.Address != input.Email.Trim()) throw new AcessoException(FalhaAcesso.Validacao);
            if (!throttle.Permitir(input.Email)) return Results.Problem(statusCode: 429, title: "Aguarde antes de tentar novamente.", extensions: new Dictionary<string, object?> { ["code"] = "LIMITE_TENTATIVAS", ["traceId"] = c.TraceIdentifier });
            var result = await service.EntrarAsync(input.Email, input.Senha, ct);
            if (result is null) throw new AcessoException(FalhaAcesso.Credenciais);
            return Results.Ok(new { result.AccessToken, result.ExpiraEm, Usuario = Public(result.Usuario) });
        }).AllowAnonymous().RequireRateLimiting("login");
        auth.MapGet("/me", (HttpContext c) => { c.Response.Headers.CacheControl = "no-store"; var a = Actor(c); return Results.Ok(new SessaoResponse(Public(a.Usuario), a.ExpiraEm)); }).RequireAuthorization("Conta");
        auth.MapPost("/logout", async (HttpContext c, IAutenticacao service, CancellationToken ct) => { await service.SairAsync(Actor(c), ct); return Results.NoContent(); }).RequireAuthorization("Conta");
        auth.MapPost("/alterar-senha", async (PasswordInput input, HttpContext c, IAutenticacao service, CancellationToken ct) =>
        {
            if (input.SenhaAtual is null || input.NovaSenha is null) throw new AcessoException(FalhaAcesso.Validacao);
            await service.AlterarSenhaAsync(Actor(c), input.SenhaAtual, input.NovaSenha, ct); return Results.NoContent();
        }).RequireAuthorization("Conta");
        var users = app.MapGroup("/api/v1/usuarios").RequireAuthorization("Usuarios.Gerenciar");
        users.MapGet("", async (HttpContext c, IAutenticacao service, int? page, int? pageSize, string? search, bool? ativo, CancellationToken ct) =>
        { var p = await service.ListarAsync(Actor(c), page ?? 1, pageSize ?? 20, search, ativo, ct); return Results.Ok(new UsuariosResponse(p.Items.Select(Public).ToArray(), p.Page, p.PageSize, p.Total)); });
        users.MapGet("/{id:guid}", async (Guid id, HttpContext c, IAutenticacao service, CancellationToken ct) => Results.Ok(Public(await service.ConsultarAsync(Actor(c), id, ct))));
        users.MapPut("/{id:guid}", async (Guid id, AtualizarUsuarioRequest input, HttpContext c, IAutenticacao service, CancellationToken ct) => Results.Ok(Public(await service.AtualizarAsync(Actor(c), id, input.Perfil, input.Ativo, input.Versao, ct))));
        users.MapPost("/{id:guid}/redefinir-senha", async (Guid id, ResetInput input, HttpContext c, IAutenticacao service, CancellationToken ct) =>
        { if (input.SenhaTemporaria is null) throw new AcessoException(FalhaAcesso.Validacao); await service.RedefinirAsync(Actor(c), id, input.SenhaTemporaria, input.VersaoEsperada, ct); return Results.NoContent(); });
    }
}
