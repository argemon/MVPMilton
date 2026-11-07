using GestaoComercial.Application.Acesso;
namespace GestaoComercial.Application.Dashboard;

public sealed record ResumoComercial(long QuantidadeConfirmada,long QuantidadeCancelada,long ReceitaCentavos,long TicketCentavos);
public sealed record ProdutoBaixo(Guid ProdutoId,string Sku,string Nome,int Saldo,int Minimo);
public sealed record VendaRecente(Guid VendaId,DateTimeOffset ConfirmadoEm,string ClienteNome,string OperadorNome,string Status,long TotalCentavos,long ContribuicaoCentavos);
public sealed record EvolucaoVenda(DateOnly Dia,long Quantidade,long ReceitaCentavos);
public sealed record ProdutoVendido(Guid ProdutoId,string Sku,string Nome,long Quantidade,long ReceitaCentavos);
public sealed record Painel(DateOnly Inicio,DateOnly Fim,string Fuso,DateTimeOffset GeradoEm,ResumoComercial Resumo,long ProdutosCadastrados,long ProdutosAtivos,long TotalEstoqueBaixo,IReadOnlyList<ProdutoBaixo> EstoqueBaixo,IReadOnlyList<VendaRecente> Recentes,IReadOnlyList<EvolucaoVenda> Evolucao,IReadOnlyList<ProdutoVendido> MaisVendidos);
public interface IDashboard
{
    Task<Painel> ConsultarAsync(SessaoAtual ator,DateOnly? inicio,DateOnly? fim,CancellationToken ct);
}
