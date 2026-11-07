using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Funcionarios;
using GestaoComercial.Contracts;
namespace GestaoComercial.Api.Acesso;

internal record CriarContaInput(Guid FuncionarioId, string? Email, string? SenhaTemporaria, string? Perfil);
public static class EndpointsFuncionarios
{
    private static SessaoAtual Actor(HttpContext c) => (SessaoAtual)c.Items[typeof(SessaoAtual)]!;
    private static FuncionarioResponse Public(Funcionario f) => new(f.Id, f.Nome, f.Email, f.Telefone, f.Ativo, f.Versao, f.CreatedAt, f.UpdatedAt);
    internal static bool? Ativo(string? value) => value switch { null or "true" => true, "false" => false, "todos" => null, _ => throw new AcessoException(FalhaAcesso.Validacao) };
    public static void MapFuncionarios(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/funcionarios").RequireAuthorization("Funcionarios.Gerenciar");
        group.MapGet("", async (HttpContext c, IFuncionarios service, string? search, string? ativo, int? page, int? pageSize, CancellationToken ct) =>
        {
            var p = await service.ListarFuncionariosAsync(Actor(c), search, Ativo(ativo), page ?? 1, pageSize ?? 20, ct);
            return Results.Ok(new FuncionariosResponse(p.Items.Select(Public).ToArray(), p.Page, p.PageSize, p.TotalItems));
        }).Produces<FuncionariosResponse>();
        group.MapGet("/{id:guid}", async (Guid id, HttpContext c, IFuncionarios service, CancellationToken ct) => Results.Ok(Public(await service.ConsultarFuncionarioAsync(Actor(c), id, ct)))).Produces<FuncionarioResponse>();
        group.MapPost("", async (CriarFuncionarioRequest d, HttpContext c, IFuncionarios service, CancellationToken ct) =>
        {
            var f = Public(await service.CriarFuncionarioAsync(Actor(c), new(d.Nome, d.Email, d.Telefone), ct)); return Results.Created($"/api/v1/funcionarios/{f.Id}", f);
        }).Produces<FuncionarioResponse>(201);
        group.MapPut("/{id:guid}", async (Guid id, AtualizarFuncionarioRequest d, HttpContext c, IFuncionarios service, CancellationToken ct) => Results.Ok(Public(await service.AtualizarFuncionarioAsync(Actor(c), id, new(d.Nome, d.Email, d.Telefone), d.Ativo, d.Versao, ct)))).Produces<FuncionarioResponse>();
        app.MapPost("/api/v1/usuarios", async (CriarContaInput d, HttpContext c, IFuncionarios service, CancellationToken ct) =>
        {
            if (d.Email is null || d.SenhaTemporaria is null || d.Perfil is null) throw new AcessoException(FalhaAcesso.Validacao);
            var u = await service.CriarContaAsync(Actor(c), d.FuncionarioId, d.Email, d.SenhaTemporaria, d.Perfil, ct);
            return Results.Created($"/api/v1/usuarios/{u.Id}", new UsuarioResponse(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, u.Versao, u.DeveAlterarSenha, u.FuncionarioId));
        }).RequireAuthorization("Usuarios.Gerenciar").Produces<UsuarioResponse>(201);
    }
}
