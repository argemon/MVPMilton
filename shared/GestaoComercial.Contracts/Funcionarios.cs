namespace GestaoComercial.Contracts;

public record FuncionarioResponse(Guid Id, string Nome, string? Email, string? Telefone, bool Ativo, long Versao, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record FuncionariosResponse(IReadOnlyList<FuncionarioResponse> Items, int Page, int PageSize, long TotalItems);
public record CriarFuncionarioRequest(string? Nome, string? Email, string? Telefone);
public record AtualizarFuncionarioRequest(string? Nome, string? Email, string? Telefone, bool Ativo, long Versao);
