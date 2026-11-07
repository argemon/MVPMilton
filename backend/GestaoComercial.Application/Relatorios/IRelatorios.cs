using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Dashboard;
using GestaoComercial.Application.Estoque;
namespace GestaoComercial.Application.Relatorios;

public sealed record ConsultaRelatorio(DateOnly? Inicio=null,DateOnly? Fim=null,Guid? OperadorId=null,Guid? ProdutoId=null,string? Tipo=null,string? Search=null,bool? Ativo=true,string? Ordenacao=null,int Page=1,int PageSize=20,bool Exportar=false);
public sealed record MetadadosRelatorio(DateOnly? Inicio,DateOnly? Fim,string Fuso,DateTimeOffset GeradoEm,string Ordenacao,Guid? OperadorId,Guid? ProdutoId,string? Tipo,string? Search,bool? Ativo);
public sealed record PaginaRelatorio<T>(IReadOnlyList<T> Items,int Page,int PageSize,long TotalItems,MetadadosRelatorio Meta);
public sealed record OperadorRelatorio(Guid OperadorId,string Nome,ResumoComercial Resumo);
public sealed record TotaisMovimentos(long Entradas,long Saidas,long Delta);
public sealed record TotaisEstoque(long Saldo,long Baixos);
public sealed record RelatorioVendas(PaginaRelatorio<VendaRecente> Dados,ResumoComercial Totais);
public sealed record RelatorioOperadores(PaginaRelatorio<OperadorRelatorio> Dados,ResumoComercial Totais);
public sealed record RelatorioProdutos(PaginaRelatorio<ProdutoVendido> Dados,ResumoComercial Totais);
public sealed record RelatorioEstoque(PaginaRelatorio<SaldoProduto> Dados,TotaisEstoque Totais);
public sealed record RelatorioMovimentos(PaginaRelatorio<Movimento> Dados,TotaisMovimentos Totais);
public interface IRelatorios
{
    Task<RelatorioVendas> VendasAsync(SessaoAtual ator,ConsultaRelatorio filtro,CancellationToken ct);
    Task<RelatorioOperadores> OperadoresAsync(SessaoAtual ator,ConsultaRelatorio filtro,CancellationToken ct);
    Task<RelatorioProdutos> ProdutosAsync(SessaoAtual ator,ConsultaRelatorio filtro,CancellationToken ct);
    Task<RelatorioEstoque> EstoqueAsync(SessaoAtual ator,ConsultaRelatorio filtro,CancellationToken ct);
    Task<RelatorioMovimentos> MovimentosAsync(SessaoAtual ator,ConsultaRelatorio filtro,CancellationToken ct);
}
