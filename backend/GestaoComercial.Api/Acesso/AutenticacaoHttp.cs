using System.Security.Claims;
using System.Text.Encodings.Web;
using GestaoComercial.Application.Acesso;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace GestaoComercial.Api.Acesso;

public sealed class SessaoHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IAutenticacao auth)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.Ordinal)) return AuthenticateResult.NoResult();
        var session = await auth.ValidarAsync(header[7..], false, Context.RequestAborted);
        if (session is null) return AuthenticateResult.Fail("Sessão inválida.");
        Context.Items[typeof(SessaoAtual)] = session;
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, session.Usuario.Id.ToString()), new Claim(ClaimTypes.Name, session.Usuario.Nome),
            new Claim(ClaimTypes.Role, session.Usuario.Perfil), new Claim("troca", session.Usuario.DeveAlterarSenha ? "sim" : "nao")
        }, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
public sealed class AcessoErro : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext c, Exception e, CancellationToken ct)
    {
        var (status, code) = e switch
        {
            AcessoException a => a.Falha switch
            {
                FalhaAcesso.Credenciais => (401, "CREDENCIAIS_INVALIDAS"),
                FalhaAcesso.Proibido => (403, "ACESSO_NEGADO"),
                FalhaAcesso.TrocaObrigatoria => (403, "TROCA_SENHA_OBRIGATORIA"),
                FalhaAcesso.NaoEncontrado => (404, "USUARIO_NAO_ENCONTRADO"),
                FalhaAcesso.Versao => (409, "VERSAO_DESATUALIZADA"),
                FalhaAcesso.UltimoAdministrador => (409, "ULTIMO_ADMINISTRADOR"),
                FalhaAcesso.EmailDuplicado => (409, "EMAIL_DUPLICADO"),
                _ => (400, "VALIDACAO")
            },
            MongoException => (503, "PERSISTENCIA_INDISPONIVEL"),
            OperationCanceledException when !ct.IsCancellationRequested => (503, "OPERACAO_INDISPONIVEL"),
            _ => (0, "")
        };
        if (status == 0) return false;
        if (status == 503) c.Response.Headers.RetryAfter = "5";
        await Results.Problem(statusCode: status, title: status == 401 ? "Não foi possível entrar com as credenciais informadas." : "Solicitação não atendida", extensions: new Dictionary<string, object?> { ["code"] = code, ["traceId"] = c.TraceIdentifier }).ExecuteAsync(c);
        return true;
    }
}
