using GestaoComercial.Application.Acesso;
namespace GestaoComercial.Application.Clientes;

public record Cliente(Guid Id, string Nome, string Tipo, string? Documento, string? Email, string? Telefone, bool Ativo, long Versao, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record DadosCliente(string? Nome, string? Tipo, string? Documento, string? Email, string? Telefone);
public record PaginaClientes(IReadOnlyList<Cliente> Items, int Page, int PageSize, long TotalItems);
public record VendaCliente(Guid Id, DateTimeOffset CriadoEm, string Status, long TotalCentavos);
public record HistoricoCliente(IReadOnlyList<VendaCliente> Items, int Page, int PageSize, long TotalItems);
public interface IClientes
{
    Task<PaginaClientes> ListarAsync(SessaoAtual ator, string? search, string? tipo, bool? ativo, int page, int pageSize, CancellationToken ct);
    Task<Cliente> ConsultarAsync(SessaoAtual ator, Guid id, CancellationToken ct);
    Task<Cliente> SalvarAsync(SessaoAtual ator, Guid? id, DadosCliente dados, bool ativo, long versao, CancellationToken ct);
    Task<HistoricoCliente> HistoricoAsync(SessaoAtual ator, Guid id, int page, int pageSize, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
}
