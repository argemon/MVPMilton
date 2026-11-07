namespace GestaoComercial.Application.Acesso;

public record UsuarioAcesso(Guid Id, string Nome, string Perfil, bool DeveAlterarSenha);
public record UsuarioResumo(Guid Id, string Nome, string Email, string Perfil, bool Ativo, long Versao, bool DeveAlterarSenha, Guid? FuncionarioId);
public record PaginaUsuarios(IReadOnlyList<UsuarioResumo> Items, int Page, int PageSize, long Total);
public record SessaoAtual(Guid Id, UsuarioAcesso Usuario, string Stamp, DateTimeOffset ExpiraEm);
public record EntradaSessao(string AccessToken, DateTimeOffset ExpiraEm, UsuarioAcesso Usuario);
public enum FalhaAcesso { Validacao, Credenciais, Proibido, TrocaObrigatoria, NaoEncontrado, Versao, UltimoAdministrador, EmailDuplicado }
public sealed class AcessoException(FalhaAcesso falha) : Exception("Operação de acesso não permitida.")
{
    public FalhaAcesso Falha { get; } = falha;
}
public interface IAutenticacao
{
    Task<EntradaSessao?> EntrarAsync(string email, string senha, CancellationToken ct);
    Task<SessaoAtual?> ValidarAsync(string token, bool registrarUso, CancellationToken ct);
    Task SairAsync(SessaoAtual ator, CancellationToken ct);
    Task AlterarSenhaAsync(SessaoAtual ator, string atual, string nova, CancellationToken ct);
    Task<PaginaUsuarios> ListarAsync(SessaoAtual ator, int page, int pageSize, string? search, bool? ativo, CancellationToken ct);
    Task<UsuarioResumo> ConsultarAsync(SessaoAtual ator, Guid id, CancellationToken ct);
    Task<UsuarioResumo> AtualizarAsync(SessaoAtual ator, Guid id, string perfil, bool ativo, long versao, CancellationToken ct);
    Task RedefinirAsync(SessaoAtual ator, Guid id, string senha, long versao, CancellationToken ct);
    Task<bool> ProvisionarAsync(string nome, string email, string senha, CancellationToken ct);
}
