using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Demonstracao;
using GestaoComercial.Contracts;
namespace GestaoComercial.Api.Acesso;
public static class EndpointsDemonstracao
{
    public static void MapDemonstracao(this WebApplication app)
    {
        var g=app.MapGroup("/api/v1/administracao/dados-demonstracao").RequireAuthorization("Funcionarios.Gerenciar");
        g.MapGet("",async(HttpContext c,IDadosDemonstracao service,CancellationToken ct)=>{var r=await service.ConsultarAsync((SessaoAtual)c.Items[typeof(SessaoAtual)]!,ct);c.Response.Headers.CacheControl="no-store";return new EstadoCargaResponse(r.Banco,r.UltimaExecucao);}).Produces<EstadoCargaResponse>().ProducesProblem(401).ProducesProblem(403);
        g.MapPost("/gerar",async(HttpContext c,IDadosDemonstracao service,CancellationToken ct)=>Public(await service.ExecutarAsync((SessaoAtual)c.Items[typeof(SessaoAtual)]!,false,false,ct))).Produces<CargaDemonstracaoResponse>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(409).ProducesProblem(503);
        g.MapPost("/restaurar",async(RestaurarCargaRequest body,HttpContext c,IDadosDemonstracao service,CancellationToken ct)=>Public(await service.ExecutarAsync((SessaoAtual)c.Items[typeof(SessaoAtual)]!,true,body.Confirmar,ct))).Produces<CargaDemonstracaoResponse>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(409).ProducesProblem(503);
        g.MapPost("/limpar",async(LimparDadosRequest body,HttpContext c,IDadosDemonstracao service,CancellationToken ct)=>
        {
            c.Response.Headers.CacheControl="no-store";
            await service.LimparAsync((SessaoAtual)c.Items[typeof(SessaoAtual)]!,body.OperacaoId,body.Confirmacao,ct);
            return Results.NoContent();
        }).Produces(204).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(409).ProducesProblem(503);
    }
    private static CargaDemonstracaoResponse Public(ResultadoCarga r)=>new(r.Funcionarios,r.Usuarios,r.Clientes,r.Produtos,r.Vendas,r.Canceladas);
}
