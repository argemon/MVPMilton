using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Estoque;
using GestaoComercial.Contracts;
namespace GestaoComercial.Api.Acesso;

public static class EndpointsEstoque
{
    private static SessaoAtual Actor(HttpContext c) => (SessaoAtual)c.Items[typeof(SessaoAtual)]!;
    private static MovimentoResponse Public(Movimento m) => new(m.Id,m.ProdutoId,m.Tipo,m.Delta,m.SaldoAntes,m.SaldoDepois,m.Motivo,new(m.AtorId,m.AtorNome),m.OcorridoEm,m.VendaId,m.ItemId);
    internal static Guid Key(HttpContext c) => c.Request.Headers["Idempotency-Key"] is var header && header.Count==1 && Guid.TryParse(header[0],out var key) && key!=Guid.Empty ? key : throw new CadastroException("VALIDACAO");
    public static void MapEstoque(this WebApplication app)
    {
        var group=app.MapGroup("/api/v1/estoque").RequireAuthorization("Estoque.Ler");
        group.MapGet("/saldos",async(HttpContext c,IEstoque service,string? search,string? ativo,bool? abaixoMinimo,int? page,int? pageSize,CancellationToken ct)=>
        {
            var p=await service.SaldosAsync(Actor(c),search,EndpointsFuncionarios.Ativo(ativo),abaixoMinimo??false,page??1,pageSize??20,ct);
            return Results.Ok(new SaldosResponse(p.Items.Select(x=>new SaldoProdutoResponse(x.ProdutoId,x.Sku,x.Nome,x.Saldo,x.Minimo,x.Ativo)).ToArray(),p.Page,p.PageSize,p.TotalItems));
        }).Produces<SaldosResponse>();
        group.MapGet("/movimentos",async(HttpContext c,IEstoque service,Guid? produtoId,string? tipo,DateOnly? inicio,DateOnly? fim,int? page,int? pageSize,CancellationToken ct)=>
        {
            var p=await service.HistoricoAsync(Actor(c),produtoId,tipo,inicio,fim,page??1,pageSize??20,ct);
            return Results.Ok(new MovimentosResponse(p.Items.Select(Public).ToArray(),p.Page,p.PageSize,p.TotalItems));
        }).RequireAuthorization("Estoque.Historico").Produces<MovimentosResponse>();
        group.MapGet("/movimentos/{id:guid}",async(Guid id,HttpContext c,IEstoque service,CancellationToken ct)=>Results.Ok(Public(await service.ConsultarAsync(Actor(c),id,ct)))).RequireAuthorization("Estoque.Historico").Produces<MovimentoResponse>();
        foreach(var (path,type) in new[]{("entradas","Entrada"),("saidas","SaidaManual")})
        {
            var kind=type;
            group.MapPost("/"+path,async(MovimentoRequest d,HttpContext c,IEstoque service,[Microsoft.AspNetCore.Mvc.FromHeader(Name="Idempotency-Key")] string idempotencyKey,CancellationToken ct)=>
            {
                var result=Public(await service.MovimentarAsync(Actor(c),Key(c),d.ProdutoId,kind,d.Quantidade,d.Motivo,ct));
                return Results.Created($"/api/v1/estoque/movimentos/{result.Id}",result);
            }).RequireAuthorization("Estoque.Movimentar").Produces<MovimentoResponse>(201);
        }
        group.MapPost("/ajustes",async(AjusteRequest d,HttpContext c,IEstoque service,[Microsoft.AspNetCore.Mvc.FromHeader(Name="Idempotency-Key")] string idempotencyKey,CancellationToken ct)=>
        {
            var result=Public(await service.MovimentarAsync(Actor(c),Key(c),d.ProdutoId,"Ajuste",d.Delta,d.Motivo,ct));
            return Results.Created($"/api/v1/estoque/movimentos/{result.Id}",result);
        }).RequireAuthorization("Estoque.Ajustar").Produces<MovimentoResponse>(201);
    }
}
