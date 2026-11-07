namespace GestaoComercial.Contracts;

public sealed record FiltrosRelatorioResponse(DateOnly? Inicio,DateOnly? Fim,string Fuso,DateTimeOffset GeradoEm,string Ordenacao,Guid? OperadorId,Guid? ProdutoId,string? Tipo,string? Search,bool? Ativo);
public sealed record OperadorRelatorioResponse(Guid OperadorId,string Nome,long QuantidadeConfirmada,long QuantidadeCancelada,decimal ReceitaLiquida,decimal TicketMedio);
public sealed record TotaisEstoqueResponse(long Saldo,long Baixos);
public sealed record TotaisMovimentosResponse(long Entradas,long Saidas,long Delta);
public sealed record RelatorioVendasResponse(IReadOnlyList<VendaRecenteResponse> Items,int Page,int PageSize,long TotalItems,ResumoComercialResponse Totais,FiltrosRelatorioResponse Filtros);
public sealed record RelatorioOperadoresResponse(IReadOnlyList<OperadorRelatorioResponse> Items,int Page,int PageSize,long TotalItems,ResumoComercialResponse Totais,FiltrosRelatorioResponse Filtros);
public sealed record RelatorioProdutosResponse(IReadOnlyList<ProdutoVendidoResponse> Items,int Page,int PageSize,long TotalItems,ResumoComercialResponse Totais,FiltrosRelatorioResponse Filtros);
public sealed record RelatorioEstoqueResponse(IReadOnlyList<SaldoProdutoResponse> Items,int Page,int PageSize,long TotalItems,TotaisEstoqueResponse Totais,FiltrosRelatorioResponse Filtros);
public sealed record RelatorioMovimentosResponse(IReadOnlyList<MovimentoResponse> Items,int Page,int PageSize,long TotalItems,TotaisMovimentosResponse Totais,FiltrosRelatorioResponse Filtros);
