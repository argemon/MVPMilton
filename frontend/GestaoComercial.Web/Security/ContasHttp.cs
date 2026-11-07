using System.Security.Claims;
using GestaoComercial.Contracts;
using GestaoComercial.Web.ApiClients;
using GestaoComercial.Web.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
namespace GestaoComercial.Web.Security;

public static class ContasHttp
{
    public static string RetornoLocal(string? value) => value is not null && value.StartsWith('/') && !value.StartsWith("//") && !value.Contains('\\') && !value.Any(char.IsControl) && value.Length <= 1000 ? value : "/";
    private static IResult Form(int status, string error, bool password = false, string? returnUrl = null)
        => new RazorComponentResult<DocumentoConta>(new { Erro = error, Senha = password, ReturnUrl = RetornoLocal(returnUrl) }) { StatusCode = status, PreventStreamingRendering = true };
    private static IResult Redirect(HttpContext c, string path) { c.Response.StatusCode = 303; c.Response.Headers.Location = path; return Results.Empty; }
    private static async Task<bool> CsrfAsync(HttpContext c, IAntiforgery anti)
    {
        if (c.Request.ContentLength > 16384 || !c.Request.HasFormContentType) return false;
        try { await anti.ValidateRequestAsync(c); return true; } catch (AntiforgeryValidationException) { return false; }
    }
    public static IServiceCollection AddContasHttp(this IServiceCollection services)
    {
        services.AddSingleton<MongoTickets>();
        services.AddAuthentication(MongoTickets.Scheme).AddCookie(MongoTickets.Scheme, o =>
        {
            o.Cookie.Name = "__Host-GestaoComercial"; o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.Path = "/"; o.LoginPath = "/conta/entrar"; o.AccessDeniedPath = "/acesso-negado";
            o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = false;
            o.Events.OnValidatePrincipal = async context =>
            {
                // Logout remains available offline; antiforgery validates the cookie identity.
                if (context.HttpContext.Request.Path == "/conta/sair") return;
                var key = context.Principal?.FindFirstValue(MongoTickets.Claim);
                async Task RejectAsync()
                {
                    context.RejectPrincipal();
                    context.HttpContext.Response.Cookies.Delete("__Host-GestaoComercial", new CookieOptions { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/" });
                    if (key is not null) await context.HttpContext.RequestServices.GetRequiredService<MongoTickets>().RemoveAsync(key);
                }
                try
                {
                    var tickets = context.HttpContext.RequestServices.GetRequiredService<MongoTickets>();
                    var api = context.HttpContext.RequestServices.GetRequiredService<AcessoApiClient>();
                    var token = key is null ? null : await tickets.TokenAsync(key, context.HttpContext.RequestAborted);
                    if (token is null) { await RejectAsync(); return; }
                    var me = await api.MeAsync(token, context.HttpContext.RequestAborted);
                    if (me.Usuario.Id.ToString() != context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)) { await RejectAsync(); return; }
                    context.ReplacePrincipal(MongoTickets.Principal(me.Usuario, key!));
                }
                catch (Exception) when (!context.HttpContext.RequestAborted.IsCancellationRequested) { await RejectAsync(); }
            };
        });
        services.AddOptions<CookieAuthenticationOptions>(MongoTickets.Scheme).Configure<MongoTickets>((o, store) => o.SessionStore = store);
        services.AddAuthorization(o =>
        {
            o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            o.AddPolicy("Usuarios.Gerenciar", p => p.RequireAuthenticatedUser().RequireRole("Administrador").RequireClaim("troca", "nao"));
            o.AddPolicy("Funcionarios.Gerenciar", p => p.RequireAuthenticatedUser().RequireRole("Administrador").RequireClaim("troca", "nao"));
            o.AddPolicy("Clientes.Gerenciar", p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Vendedor").RequireClaim("troca", "nao"));
            o.AddPolicy("Produtos.Ler", p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Vendedor", "Estoquista").RequireClaim("troca", "nao"));
            o.AddPolicy("Produtos.Gerenciar", p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Estoquista").RequireClaim("troca", "nao"));
            o.AddPolicy("Estoque.Ler", p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Vendedor", "Estoquista").RequireClaim("troca", "nao"));
            o.AddPolicy("Estoque.Historico", p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Estoquista").RequireClaim("troca", "nao"));
            o.AddPolicy("Estoque.Movimentar", p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Estoquista").RequireClaim("troca", "nao"));
            o.AddPolicy("Estoque.Ajustar", p => p.RequireAuthenticatedUser().RequireRole("Administrador").RequireClaim("troca", "nao"));
            foreach(var policy in new[]{"Vendas.Ler","Vendas.Criar","Vendas.Editar","Vendas.Confirmar"})o.AddPolicy(policy,p=>p.RequireAuthenticatedUser().RequireClaim("troca","nao").RequireRole("Administrador","Vendedor"));
            o.AddPolicy("Relatorios.Vendas",p=>p.RequireAuthenticatedUser().RequireClaim("troca","nao").RequireRole("Administrador"));
            foreach(var policy in new[]{"Relatorios.Ler","Relatorios.Estoque","Relatorios.Movimentos"})o.AddPolicy(policy,p=>p.RequireAuthenticatedUser().RequireClaim("troca","nao").RequireRole("Administrador","Estoquista"));
            o.AddPolicy("Dashboard.Ler",p=>p.RequireAuthenticatedUser().RequireClaim("troca","nao").RequireRole("Administrador"));
            o.AddPolicy("Vendas.Cancelar",p=>p.RequireAuthenticatedUser().RequireClaim("troca","nao").RequireRole("Administrador"));
        });
        services.AddCascadingAuthenticationState();
        services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, EstadoAutenticacao>();
        services.AddScoped<SessaoCircuito>();
        return services;
    }
    private static async Task ClearAsync(HttpContext c, MongoTickets tickets)
    {
        var key = c.User.FindFirstValue(MongoTickets.Claim);
        // Expire the browser cookie even if the backing store is unavailable.
        c.Response.Cookies.Delete("__Host-GestaoComercial", new CookieOptions { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/" });
        if (key is not null) await tickets.RemoveAsync(key);
        await c.SignOutAsync(MongoTickets.Scheme);
    }
    public static void MapContas(this WebApplication app)
    {
        app.MapGet("/imagens/produtos/{id:guid}",async(Guid id,HttpContext c,MongoTickets tickets,AcessoApiClient api)=>{
            c.Response.Headers.CacheControl="no-store";
            try {
                var key=c.User.FindFirstValue(MongoTickets.Claim);var token=key is null?null:await tickets.TokenAsync(key,c.RequestAborted);
                if(token is null) return Results.Unauthorized();var bytes=await api.ImagemProdutoAsync(token,id,c.RequestAborted);return Results.Bytes(bytes,"image/png");
            }catch(ApiFailure e){return Results.Problem(statusCode:e.Status,title:"Imagem indisponível.");}
        }).RequireAuthorization("Produtos.Ler");
        app.MapPost("/usuarios/criar", async (HttpContext c, IAntiforgery anti, MongoTickets tickets, AcessoApiClient api) =>
        {
            c.Response.Headers.CacheControl = "no-store";
            if (!await CsrfAsync(c, anti)) return Results.Problem(statusCode: 400, title: "Formulário inválido. Recarregue a página.");
            var form = await c.Request.ReadFormAsync(c.RequestAborted);
            if (!Guid.TryParse(form["funcionarioId"], out var id)) return Redirect(c, "/usuarios/novo?erro=true");
            try {
                var key = c.User.FindFirstValue(MongoTickets.Claim); var token = key is null ? null : await tickets.TokenAsync(key, c.RequestAborted);
                if (token is null) return Redirect(c, "/conta/entrar");
                var u = await api.CriarContaAsync(token, id, form["email"].ToString(), form["senha"].ToString(), form["perfil"].ToString(), c.RequestAborted);
                return Redirect(c, $"/usuarios/{u.Id}");
            } catch (ApiFailure e) { return Redirect(c, e.Status == 401 ? "/conta/entrar" : e.Status == 403 ? "/acesso-negado" : $"/usuarios/novo?funcionarioId={id}&erro=true"); }
        }).RequireAuthorization("Usuarios.Gerenciar");
        app.MapPost("/conta/entrar", async (HttpContext c, IAntiforgery anti, MongoTickets tickets, AcessoApiClient api) =>
        {
            c.Response.Headers.CacheControl = "no-store";
            if (!await CsrfAsync(c, anti)) return Results.Problem(statusCode: 400, title: "Formulário inválido. Recarregue a página.");
            if (c.User.Identity?.IsAuthenticated == true) return Form(409, "Encerre a sessão atual antes de entrar novamente.");
            var form = await c.Request.ReadFormAsync(c.RequestAborted); var email = form["email"].ToString(); var password = form["senha"].ToString(); var returnUrl = RetornoLocal(form["returnUrl"]);
            try
            {
                if (email.Length > 254 || password.Length > 128) return Form(400, "Verifique os limites dos campos.");
                var login = await api.EntrarAsync(email, password, c.RequestAborted);
                var principal = await tickets.CriarAsync(login.AccessToken, new SessaoResponse(login.Usuario, login.ExpiraEm), c.RequestAborted);
                await c.SignInAsync(MongoTickets.Scheme, principal, new AuthenticationProperties { ExpiresUtc = login.ExpiraEm, IsPersistent = false, AllowRefresh = false });
                return Redirect(c, login.Usuario.DeveAlterarSenha ? "/conta/senha" : returnUrl);
            }
            catch (ApiFailure e) { return Form(e.Status, e.Status == 401 ? "Não foi possível entrar com as credenciais informadas." : e.Status == 429 ? "Aguarde antes de tentar novamente." : "Serviço indisponível. Tente novamente.", returnUrl: returnUrl); }
        }).AllowAnonymous().RequireRateLimiting("login-web").WithOrder(-1);
        app.MapPost("/conta/sair", async (HttpContext c, IAntiforgery anti, MongoTickets tickets, AcessoApiClient api, ILoggerFactory logs) =>
        {
            if (!await CsrfAsync(c, anti)) return Results.Problem(statusCode: 400, title: "Formulário inválido. Recarregue a página.");
            try
            {
                var key = c.User.FindFirstValue(MongoTickets.Claim); var token = key is null ? null : await tickets.TokenAsync(key, c.RequestAborted);
                if (token is not null) await api.SairAsync(token, c.RequestAborted);
            }
            catch (Exception) when (!c.RequestAborted.IsCancellationRequested) { logs.CreateLogger("Logout").LogWarning("Revogação remota indisponível; acesso local encerrado. TraceId {TraceId}", c.TraceIdentifier); }
            finally { await ClearAsync(c, tickets); }
            return Redirect(c, "/conta/entrar");
        }).AllowAnonymous().WithOrder(-1);
        app.MapPost("/conta/senha", async (HttpContext c, IAntiforgery anti, MongoTickets tickets, AcessoApiClient api) =>
        {
            if (!await CsrfAsync(c, anti)) return Results.Problem(statusCode: 400, title: "Formulário inválido. Recarregue a página.");
            var form = await c.Request.ReadFormAsync(c.RequestAborted);
            try
            {
                var key = c.User.FindFirstValue(MongoTickets.Claim); var token = key is null ? null : await tickets.TokenAsync(key, c.RequestAborted); if (token is null) return Redirect(c, "/conta/entrar");
                await api.SenhaAsync(token, form["senhaAtual"].ToString(), form["novaSenha"].ToString(), c.RequestAborted);
                await ClearAsync(c, tickets); return Redirect(c, "/conta/entrar?alterada=1");
            }
            catch (ApiFailure e) { return Form(e.Status, e.Status == 400 ? "Confira a senha atual e a política da nova senha: 12 a 128 caracteres, maiúscula, minúscula, número e símbolo." : "Serviço indisponível. Tente novamente.", true); }
        }).RequireAuthorization().WithOrder(-1);
    }
}
