using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Dashboard;
using GestaoComercial.Contracts;
namespace GestaoComercial.Api.Acesso;

public static class EndpointsDashboard
{
    internal static ResumoComercialResponse Public(ResumoComercial r)=>new(r.QuantidadeConfirmada,r.QuantidadeCancelada,r.ReceitaCentavos/100m,r.TicketCentavos/100m);
    internal static VendaRecenteResponse Public(VendaRecente r)=>new(r.VendaId,r.ConfirmadoEm,r.ClienteNome,r.OperadorNome,r.Status,r.TotalCentavos/100m,r.ContribuicaoCentavos/100m);
    internal static ProdutoVendidoResponse Public(ProdutoVendido p)=>new(p.ProdutoId,p.Sku,p.Nome,p.Quantidade,p.ReceitaCentavos/100m);
    public static void MapDashboard(this WebApplication app)
    {
        app.MapGet("/api/v1/dashboard",async(HttpContext c,IDashboard service,DateOnly? inicio,DateOnly? fim,CancellationToken ct)=>
        {
            var p=await service.ConsultarAsync((SessaoAtual)c.Items[typeof(SessaoAtual)]!,inicio,fim,ct);
            return Results.Ok(new DashboardResponse(p.Inicio,p.Fim,p.Fuso,p.GeradoEm,Public(p.Resumo),p.ProdutosCadastrados,p.ProdutosAtivos,new(p.EstoqueBaixo.Select(x=>new ProdutoBaixoResponse(x.ProdutoId,x.Sku,x.Nome,x.Saldo,x.Minimo)).ToArray(),1,20,p.TotalEstoqueBaixo),p.Recentes.Select(Public).ToArray(),p.Evolucao.Select(x=>new EvolucaoVendaResponse(x.Dia,x.Quantidade,x.ReceitaCentavos/100m)).ToArray(),p.MaisVendidos.Select(Public).ToArray()));
        }).RequireAuthorization("Dashboard.Ler").Produces<DashboardResponse>();
    }
}
