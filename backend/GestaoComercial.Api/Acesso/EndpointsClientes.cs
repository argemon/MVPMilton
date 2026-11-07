using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Contracts;
namespace GestaoComercial.Api.Acesso;

public static class EndpointsClientes
{
    private static SessaoAtual Actor(HttpContext c) => (SessaoAtual)c.Items[typeof(SessaoAtual)]!;
    private static ClienteResponse Public(Cliente c) => new(c.Id,c.Nome,c.Tipo,c.Documento,c.Email,c.Telefone,c.Ativo,c.Versao,c.CreatedAt,c.UpdatedAt);
    public static void MapClientes(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/clientes").RequireAuthorization("Clientes.Gerenciar");
        group.MapGet("",async (HttpContext c,IClientes service,string? search,string? tipo,string? ativo,int? page,int? pageSize,CancellationToken ct) => {
            var p=await service.ListarAsync(Actor(c),search,tipo,EndpointsFuncionarios.Ativo(ativo),page??1,pageSize??20,ct);
            return Results.Ok(new ClientesResponse(p.Items.Select(Public).ToArray(),p.Page,p.PageSize,p.TotalItems));
        }).Produces<ClientesResponse>();
        group.MapGet("/{id:guid}",async (Guid id,HttpContext c,IClientes service,CancellationToken ct) => Results.Ok(Public(await service.ConsultarAsync(Actor(c),id,ct)))).Produces<ClienteResponse>();
        group.MapPost("",async (CriarClienteRequest d,HttpContext c,IClientes service,CancellationToken ct) => {
            var row=Public(await service.SalvarAsync(Actor(c),null,new(d.Nome,d.Tipo,d.Documento,d.Email,d.Telefone),true,0,ct)); return Results.Created($"/api/v1/clientes/{row.Id}",row);
        }).Produces<ClienteResponse>(201);
        group.MapPut("/{id:guid}",async(Guid id,AtualizarClienteRequest d,HttpContext c,IClientes service,CancellationToken ct) => Results.Ok(Public(await service.SalvarAsync(Actor(c),id,new(d.Nome,d.Tipo,d.Documento,d.Email,d.Telefone),d.Ativo,d.Versao,ct)))).Produces<ClienteResponse>();
        group.MapGet("/{id:guid}/vendas",async(Guid id,HttpContext c,IClientes service,int? page,int? pageSize,DateOnly? inicio,DateOnly? fim,CancellationToken ct) => {
            var p=await service.HistoricoAsync(Actor(c),id,page??1,pageSize??20,inicio,fim,ct); return Results.Ok(new HistoricoClienteResponse(p.Items.Select(x=>new VendaClienteResponse(x.Id,x.CriadoEm,x.Status,x.TotalCentavos/100m)).ToArray(),p.Page,p.PageSize,p.TotalItems));
        }).Produces<HistoricoClienteResponse>();
    }
}
