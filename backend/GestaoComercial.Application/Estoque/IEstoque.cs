using GestaoComercial.Application.Acesso;
namespace GestaoComercial.Application.Estoque;

public sealed record SaldoProduto(Guid ProdutoId, string Sku, string Nome, int Saldo, int Minimo, bool Ativo);
public sealed record Movimento(Guid Id, Guid ProdutoId, string Tipo, int Delta, int SaldoAntes, int SaldoDepois, string Motivo, Guid AtorId, string AtorNome, DateTimeOffset OcorridoEm);
public sealed record PaginaEstoque<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalItems);
public interface IEstoque
{
    Task<PaginaEstoque<SaldoProduto>> SaldosAsync(SessaoAtual ator, string? search, bool? ativo, bool abaixoMinimo, int page, int pageSize, CancellationToken ct);
    Task<PaginaEstoque<Movimento>> HistoricoAsync(SessaoAtual ator, Guid? produtoId, string? tipo, DateOnly? inicio, DateOnly? fim, int page, int pageSize, CancellationToken ct);
    Task<Movimento> ConsultarAsync(SessaoAtual ator, Guid id, CancellationToken ct);
    Task<Movimento> MovimentarAsync(SessaoAtual ator, Guid chave, Guid produtoId, string tipo, int quantidade, string? motivo, CancellationToken ct);
}
