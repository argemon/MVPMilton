namespace GestaoComercial.Contracts;

public record ClienteResponse(Guid Id, string Nome, string Tipo, string? Documento, string? Email, string? Telefone, bool Ativo, long Versao, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record ClientesResponse(IReadOnlyList<ClienteResponse> Items, int Page, int PageSize, long TotalItems);
public record CriarClienteRequest(string? Nome, string? Tipo, string? Documento, string? Email, string? Telefone);
public record AtualizarClienteRequest(string? Nome, string? Tipo, string? Documento, string? Email, string? Telefone, bool Ativo, long Versao);
public record VendaClienteResponse(Guid Id, DateTimeOffset CriadoEm, string Status, decimal Total);
public record HistoricoClienteResponse(IReadOnlyList<VendaClienteResponse> Items, int Page, int PageSize, long TotalItems);
