using GestaoComercial.Application.Acesso;
namespace GestaoComercial.Application.Vendas;

public sealed record ItemComando(Guid ProdutoId,int Quantidade);
public sealed record OperadorVenda(Guid Id,string Nome);
public sealed record ClienteVenda(string Nome,string? Documento);
public sealed record EventoVenda(OperadorVenda Ator,DateTimeOffset Data,string? Motivo);
public sealed record ItemVenda(Guid Id,Guid ProdutoId,string SkuSnapshot,string NomeSnapshot,int Quantidade,long PrecoUnitarioCentavos,long TotalCentavos);
public sealed record Venda(Guid Id,Guid ClienteId,ClienteVenda? ClienteSnapshot,OperadorVenda Criador,string Status,IReadOnlyList<ItemVenda> Itens,long TotalCentavos,long Versao,DateTimeOffset CriadoEm,EventoVenda? Confirmacao,EventoVenda? Cancelamento,EventoVenda? Descarte);
public sealed record VendaResumo(Guid Id,Guid ClienteId,string? ClienteNome,OperadorVenda Criador,string Status,long TotalCentavos,long Versao,DateTimeOffset CriadoEm);
public sealed record PaginaVendas(IReadOnlyList<VendaResumo> Items,int Page,int PageSize,long TotalItems);
public interface IVendas
{
    Task<PaginaVendas> ListarAsync(SessaoAtual ator,Guid? clienteId,string? status,DateOnly? inicio,DateOnly? fim,int page,int pageSize,CancellationToken ct);
    Task<Venda> ConsultarAsync(SessaoAtual ator,Guid id,CancellationToken ct);
    Task<Venda> SalvarAsync(SessaoAtual ator,Guid? id,Guid chave,Guid clienteId,IReadOnlyList<ItemComando>? itens,long versao,CancellationToken ct);
    Task<Venda> TransicionarAsync(SessaoAtual ator,Guid id,Guid chave,string acao,long versaoEsperada,string? motivo,CancellationToken ct);
}
