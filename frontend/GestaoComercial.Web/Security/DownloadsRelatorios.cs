using System.Security.Claims;
using GestaoComercial.Web.ApiClients;
namespace GestaoComercial.Web.Security;

public static class DownloadsRelatorios
{
    public static void MapDownloadsRelatorios(this WebApplication app)
    {
        foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})
        {
            var type=kind;var policy=kind is "estoque" or "movimentos"?"Relatorios."+(kind=="estoque"?"Estoque":"Movimentos"):"Relatorios.Vendas";
            app.MapGet("/downloads/relatorios/"+type,async(HttpContext c,MongoTickets tickets,AcessoApiClient api)=>
            {
                c.Response.Headers.CacheControl="no-store";
                string[] allowed=type switch{"estoque"=>["search","ativo","ordenacao"],"movimentos"=>["inicio","fim","produtoId","tipo","ordenacao"],_=>["inicio","fim","operadorId","ordenacao"]};
                if(c.Request.Query.Any(x=>x.Value.Count!=1||!allowed.Contains(x.Key,StringComparer.OrdinalIgnoreCase)))return Results.Problem(statusCode:400,title:"Filtros inválidos.");
                try
                {
                    var key=c.User.FindFirstValue(MongoTickets.Claim);var token=key is null?null:await tickets.TokenAsync(key,c.RequestAborted);if(token is null)return Results.Unauthorized();
                    var query=QueryString.Create(c.Request.Query.Select(x=>new KeyValuePair<string,string?>(x.Key,x.Value.ToString()))).ToString();
                    var bytes=await api.ExportarRelatorioAsync(token,type,query,c.RequestAborted);return Results.File(bytes,"text/csv; charset=utf-8","relatorio-"+type+".csv");
                }
                catch(ApiFailure e){return Results.Problem(statusCode:e.Status,title:e.Code=="EXPORTACAO_EXCEDE_LIMITE"?"O relatório excede 10.000 linhas. Reduza os filtros.":"Não foi possível exportar o relatório.",extensions:new Dictionary<string,object?>{["code"]=e.Code??"EXPORTACAO_INDISPONIVEL"});}
            }).RequireAuthorization(policy);
        }
    }
}
