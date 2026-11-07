namespace GestaoComercial.Contracts;

// No password, API token, persistence model or backend dependency in shared UI contracts.
public record UsuarioAutenticado(Guid Id, string Nome, string Perfil, bool DeveAlterarSenha);
public record SessaoResponse(UsuarioAutenticado Usuario, DateTimeOffset ExpiraEm);
public record UsuarioResponse(Guid Id, string Nome, string Email, string Perfil, bool Ativo, long Versao, bool DeveAlterarSenha, Guid? FuncionarioId);
public record UsuariosResponse(IReadOnlyList<UsuarioResponse> Items, int Page, int PageSize, long Total);
public record AtualizarUsuarioRequest(string Perfil, bool Ativo, long Versao);
