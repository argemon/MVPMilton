namespace GestaoComercial.Contracts;

public sealed record ResumoComercialResponse(long QuantidadeConfirmada,long QuantidadeCancelada,decimal ReceitaLiquida,decimal TicketMedio);
public sealed record ProdutoBaixoResponse(Guid ProdutoId,string Sku,string Nome,int Saldo,int Minimo);
public sealed record EstoqueBaixoResponse(IReadOnlyList<ProdutoBaixoResponse> Items,int Page,int PageSize,long TotalItems);
public sealed record VendaRecenteResponse(Guid VendaId,DateTimeOffset ConfirmadoEm,string ClienteNome,string OperadorNome,string Status,decimal TotalOriginal,decimal ContribuicaoLiquida);
public sealed record EvolucaoVendaResponse(DateOnly Dia,long Quantidade,decimal ReceitaLiquida);
public sealed record ProdutoVendidoResponse(Guid ProdutoId,string Sku,string Nome,long Quantidade,decimal ReceitaLiquida);
public sealed record DashboardResponse(DateOnly Inicio,DateOnly Fim,string Fuso,DateTimeOffset GeradoEm,ResumoComercialResponse Resumo,long ProdutosCadastrados,long ProdutosAtivos,EstoqueBaixoResponse EstoqueBaixo,IReadOnlyList<VendaRecenteResponse> Recentes,IReadOnlyList<EvolucaoVendaResponse> Evolucao,IReadOnlyList<ProdutoVendidoResponse> MaisVendidos);
