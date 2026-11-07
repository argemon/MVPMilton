using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Vendas;
using GestaoComercial.Contracts;
namespace GestaoComercial.Api.Acesso;

public static class EndpointsVendas
{
    private static SessaoAtual Actor(HttpContext c)=>(SessaoAtual)c.Items[typeof(SessaoAtual)]!;
    private static EventoVendaResponse? Event(EventoVenda? e)=>e is null?null:new(new(e.Ator.Id,e.Ator.Nome),e.Data,e.Motivo);
    private static VendaResumoResponse Public(VendaResumo v)=>new(v.Id,v.ClienteId,v.ClienteNome,new(v.Criador.Id,v.Criador.Nome),v.Status,v.TotalCentavos/100m,v.Versao,v.CriadoEm);
    internal static VendaResponse Public(Venda v)=>new(v.Id,v.ClienteId,v.ClienteSnapshot is null?null:new(v.ClienteSnapshot.Nome,v.ClienteSnapshot.Documento),new(v.Criador.Id,v.Criador.Nome),v.Status,v.Itens.Select(i=>new ItemVendaResponse(i.Id,i.ProdutoId,i.SkuSnapshot,i.NomeSnapshot,i.Quantidade,i.PrecoUnitarioCentavos/100m,i.TotalCentavos/100m)).ToArray(),v.TotalCentavos/100m,v.Versao,v.CriadoEm,Event(v.Confirmacao),Event(v.Cancelamento),Event(v.Descarte));
    public static void MapVendas(this WebApplication app)
    {
        var group=app.MapGroup("/api/v1/vendas").RequireAuthorization("Vendas.Ler");
        group.MapGet("",async(HttpContext c,IVendas service,Guid? clienteId,string? status,DateOnly? inicio,DateOnly? fim,int? page,int? pageSize,CancellationToken ct)=>
        {var p=await service.ListarAsync(Actor(c),clienteId,status,inicio,fim,page??1,pageSize??20,ct);return Results.Ok(new VendasResponse(p.Items.Select(Public).ToArray(),p.Page,p.PageSize,p.TotalItems));}).Produces<VendasResponse>();
        group.MapGet("/{id:guid}",async(Guid id,HttpContext c,IVendas service,CancellationToken ct)=>Results.Ok(Public(await service.ConsultarAsync(Actor(c),id,ct)))).Produces<VendaResponse>();
        group.MapPost("",async(CriarVendaRequest d,HttpContext c,IVendas service,[Microsoft.AspNetCore.Mvc.FromHeader(Name="Idempotency-Key")]string idempotencyKey,CancellationToken ct)=>
        {var v=Public(await service.SalvarAsync(Actor(c),null,EndpointsEstoque.Key(c),d.ClienteId,d.Itens?.Select(x=>new ItemComando(x.ProdutoId,x.Quantidade)).ToArray(),0,ct));return Results.Created($"/api/v1/vendas/{v.Id}",v);}).RequireAuthorization("Vendas.Criar").Produces<VendaResponse>(201);
        group.MapPut("/{id:guid}",async(Guid id,AtualizarVendaRequest d,HttpContext c,IVendas service,CancellationToken ct)=>Results.Ok(Public(await service.SalvarAsync(Actor(c),id,Guid.Empty,d.ClienteId,d.Itens?.Select(x=>new ItemComando(x.ProdutoId,x.Quantidade)).ToArray(),d.Versao,ct)))).RequireAuthorization("Vendas.Editar").Produces<VendaResponse>();
        group.MapPost("/{id:guid}/confirmacao",async(Guid id,ConfirmarVendaRequest d,HttpContext c,IVendas service,[Microsoft.AspNetCore.Mvc.FromHeader(Name="Idempotency-Key")]string idempotencyKey,CancellationToken ct)=>Results.Ok(Public(await service.TransicionarAsync(Actor(c),id,EndpointsEstoque.Key(c),"confirmacao",d.VersaoEsperada,null,ct)))).RequireAuthorization("Vendas.Confirmar").Produces<VendaResponse>();
        foreach(var action in new[]{"cancelamento","descarte"})
        {
            var operation=action;
            group.MapPost("/{id:guid}/"+operation,async(Guid id,EncerrarVendaRequest d,HttpContext c,IVendas service,[Microsoft.AspNetCore.Mvc.FromHeader(Name="Idempotency-Key")]string idempotencyKey,CancellationToken ct)=>Results.Ok(Public(await service.TransicionarAsync(Actor(c),id,EndpointsEstoque.Key(c),operation,d.VersaoEsperada,d.Motivo,ct)))).RequireAuthorization(operation=="cancelamento"?"Vendas.Cancelar":"Vendas.Editar").Produces<VendaResponse>();
        }
    }
}
