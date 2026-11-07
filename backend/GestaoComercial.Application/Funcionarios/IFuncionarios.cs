using GestaoComercial.Application.Acesso;
namespace GestaoComercial.Application.Funcionarios;

public record Funcionario(Guid Id, string Nome, string? Email, string? Telefone, bool Ativo, long Versao, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record PaginaFuncionarios(IReadOnlyList<Funcionario> Items, int Page, int PageSize, long TotalItems);
public record DadosFuncionario(string? Nome, string? Email, string? Telefone);
public interface IFuncionarios
{
    Task<PaginaFuncionarios> ListarFuncionariosAsync(SessaoAtual ator, string? search, bool? ativo, int page, int pageSize, CancellationToken ct);
    Task<Funcionario> ConsultarFuncionarioAsync(SessaoAtual ator, Guid id, CancellationToken ct);
    Task<Funcionario> CriarFuncionarioAsync(SessaoAtual ator, DadosFuncionario dados, CancellationToken ct);
    Task<Funcionario> AtualizarFuncionarioAsync(SessaoAtual ator, Guid id, DadosFuncionario dados, bool ativo, long versao, CancellationToken ct);
    Task<UsuarioResumo> CriarContaAsync(SessaoAtual ator, Guid funcionarioId, string email, string senha, string perfil, CancellationToken ct);
}
