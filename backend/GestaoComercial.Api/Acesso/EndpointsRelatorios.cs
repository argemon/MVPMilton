using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Relatorios;
using GestaoComercial.Contracts;
using static GestaoComercial.Api.Acesso.CsvSeguro;
namespace GestaoComercial.Api.Acesso;

public sealed class RelatorioQuery
{
    public DateOnly? Inicio {get;set;} public DateOnly? Fim {get;set;}
    public Guid? OperadorId {get;set;} public Guid? ProdutoId {get;set;}
    public string? Tipo {get;set;} public string? Search {get;set;} public string? Ativo {get;set;} public string? Ordenacao {get;set;}
    public int? Page {get;set;} public int? PageSize {get;set;}
    public ConsultaRelatorio Filter(HttpContext c,string kind,bool export)
    {
        string[] allowed=kind switch{"estoque"=>["search","ativo","ordenacao"],"movimentos"=>["inicio","fim","produtoId","tipo","ordenacao"],_=>["inicio","fim","operadorId","ordenacao"]};
        foreach(var (key,value) in c.Request.Query)
            if(value.Count!=1||(!allowed.Contains(key,StringComparer.OrdinalIgnoreCase)&&(export||(!key.Equals("page",StringComparison.OrdinalIgnoreCase)&&!key.Equals("pageSize",StringComparison.OrdinalIgnoreCase)))))throw new CadastroException("VALIDACAO");
        return new(Inicio,Fim,OperadorId,ProdutoId,Tipo,Search,kind=="estoque"?EndpointsFuncionarios.Ativo(Ativo):null,Ordenacao,Page??1,PageSize??20,export);
    }
}
public static class EndpointsRelatorios
{
    private static FiltrosRelatorioResponse Public(MetadadosRelatorio m)=>new(m.Inicio,m.Fim,m.Fuso,m.GeradoEm,m.Ordenacao,m.OperadorId,m.ProdutoId,m.Tipo,m.Search,m.Ativo);
    private static async Task<object> Consultar(string kind,IRelatorios service,SessaoAtual actor,ConsultaRelatorio filter,CancellationToken ct)
    {
        switch(kind)
        {
            case "vendas":{var r=await service.VendasAsync(actor,filter,ct);var p=r.Dados;return new RelatorioVendasResponse(p.Items.Select(EndpointsDashboard.Public).ToArray(),p.Page,p.PageSize,p.TotalItems,EndpointsDashboard.Public(r.Totais),Public(p.Meta));}
            case "operadores":{var r=await service.OperadoresAsync(actor,filter,ct);var p=r.Dados;return new RelatorioOperadoresResponse(p.Items.Select(x=>new OperadorRelatorioResponse(x.OperadorId,x.Nome,x.Resumo.QuantidadeConfirmada,x.Resumo.QuantidadeCancelada,x.Resumo.ReceitaCentavos/100m,x.Resumo.TicketCentavos/100m)).ToArray(),p.Page,p.PageSize,p.TotalItems,EndpointsDashboard.Public(r.Totais),Public(p.Meta));}
            case "produtos":{var r=await service.ProdutosAsync(actor,filter,ct);var p=r.Dados;return new RelatorioProdutosResponse(p.Items.Select(EndpointsDashboard.Public).ToArray(),p.Page,p.PageSize,p.TotalItems,EndpointsDashboard.Public(r.Totais),Public(p.Meta));}
            case "estoque":{var r=await service.EstoqueAsync(actor,filter,ct);var p=r.Dados;return new RelatorioEstoqueResponse(p.Items.Select(x=>new SaldoProdutoResponse(x.ProdutoId,x.Sku,x.Nome,x.Saldo,x.Minimo,x.Ativo)).ToArray(),p.Page,p.PageSize,p.TotalItems,new(r.Totais.Saldo,r.Totais.Baixos),Public(p.Meta));}
            default:{var r=await service.MovimentosAsync(actor,filter,ct);var p=r.Dados;return new RelatorioMovimentosResponse(p.Items.Select(EndpointsEstoque.Public).ToArray(),p.Page,p.PageSize,p.TotalItems,new(r.Totais.Entradas,r.Totais.Saidas,r.Totais.Delta),Public(p.Meta));}
        }
    }
    private static byte[] Csv(object result)
    {
        var (header,rows,meta)=result switch
        {
            RelatorioVendasResponse r=>(new[]{"vendaId","confirmadoEm","clienteNome","operadorNome","status","totalOriginal","contribuicaoLiquida"},r.Items.Select(x=>new[]{Texto(x.VendaId.ToString()),Texto(x.ConfirmadoEm.ToString("O")),Texto(x.ClienteNome),Texto(x.OperadorNome),Texto(x.Status),Dinheiro(x.TotalOriginal),Dinheiro(x.ContribuicaoLiquida)}),r.Filtros),
            RelatorioOperadoresResponse r=>(new[]{"operadorId","nome","quantidadeConfirmada","quantidadeCancelada","receitaLiquida","ticketMedio"},r.Items.Select(x=>new[]{Texto(x.OperadorId.ToString()),Texto(x.Nome),Numero(x.QuantidadeConfirmada),Numero(x.QuantidadeCancelada),Dinheiro(x.ReceitaLiquida),Dinheiro(x.TicketMedio)}),r.Filtros),
            RelatorioProdutosResponse r=>(new[]{"produtoId","sku","nomeSnapshot","quantidade","receitaLiquida"},r.Items.Select(x=>new[]{Texto(x.ProdutoId.ToString()),Texto(x.Sku),Texto(x.Nome),Numero(x.Quantidade),Dinheiro(x.ReceitaLiquida)}),r.Filtros),
            RelatorioEstoqueResponse r=>(new[]{"produtoId","sku","nome","saldo","minimo","ativo"},r.Items.Select(x=>new[]{Texto(x.ProdutoId.ToString()),Texto(x.Sku),Texto(x.Nome),Numero(x.Saldo),Numero(x.Minimo),Texto(x.Ativo?"true":"false")}),r.Filtros),
            RelatorioMovimentosResponse r=>(new[]{"movimentoId","produtoId","ocorridoEm","tipo","delta","saldoAntes","saldoDepois","atorId","atorNome","motivo","vendaId","itemId"},r.Items.Select(x=>new[]{Texto(x.Id.ToString()),Texto(x.ProdutoId.ToString()),Texto(x.OcorridoEm.ToString("O")),Texto(x.Tipo),Numero(x.Delta),Numero(x.SaldoAntes),Numero(x.SaldoDepois),Texto(x.Ator.Id.ToString()),Texto(x.Ator.Nome),Texto(x.Motivo),Texto(x.VendaId?.ToString()),Texto(x.ItemId?.ToString())}),r.Filtros),
            _=>throw new InvalidOperationException()
        };
        var metadata=new[]{Texto(meta.Inicio?.ToString("yyyy-MM-dd")),Texto(meta.Fim?.ToString("yyyy-MM-dd")),Texto(meta.Fuso),Texto(meta.GeradoEm.ToString("O"))};
        return Gerar(header.Concat(new[]{"inicio","fim","fuso","geradoEm"}),rows.Select(x=>x.Concat(metadata)));
    }
    public static void MapRelatorios(this WebApplication app)
    {
        foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})
        {
            var type=kind;var policy=kind is "estoque" or "movimentos"?"Relatorios."+(kind=="estoque"?"Estoque":"Movimentos"):"Relatorios.Vendas";
            var route=app.MapGet("/api/v1/relatorios/"+type,async(HttpContext c,IRelatorios service,[AsParameters]RelatorioQuery q,CancellationToken ct)=>
            {c.Response.Headers.CacheControl="no-store";return Results.Ok(await Consultar(type,service,(SessaoAtual)c.Items[typeof(SessaoAtual)]!,q.Filter(c,type,false),ct));}).RequireAuthorization(policy).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(500).ProducesProblem(503);
            switch(type){case "vendas":route.Produces<RelatorioVendasResponse>();break;case "operadores":route.Produces<RelatorioOperadoresResponse>();break;case "produtos":route.Produces<RelatorioProdutosResponse>();break;case "estoque":route.Produces<RelatorioEstoqueResponse>();break;default:route.Produces<RelatorioMovimentosResponse>();break;}
            app.MapGet("/api/v1/relatorios/"+type+"/exportacao",async(HttpContext c,IRelatorios service,[AsParameters]RelatorioQuery q,CancellationToken ct)=>
            {c.Response.Headers.CacheControl="no-store";var data=await Consultar(type,service,(SessaoAtual)c.Items[typeof(SessaoAtual)]!,q.Filter(c,type,true),ct);return Results.File(Csv(data),"text/csv; charset=utf-8","relatorio-"+type+".csv");}).RequireAuthorization(policy).Produces(200,contentType:"text/csv").ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(500).ProducesProblem(503);
        }
    }
}
