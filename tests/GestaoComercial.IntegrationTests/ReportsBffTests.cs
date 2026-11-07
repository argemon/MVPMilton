using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Vendas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ReportsBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    private const string Period="inicio=2026-01-01&fim=2027-01-01";
    [Fact]public Task PagesAndAuthenticatedDownloadUseRealApiProtectRolesAndKeepErrorsAsJson()=>f.Mongo.SafeAsync(async()=>
    {
        await f.Backend.Api.Services.GetRequiredService<EstoqueSchema>().InitializeAsync(default);await f.Backend.Api.Services.GetRequiredService<VendasSchema>().InitializeAsync(default);
        var admin=await f.Backend.SeedAsync("Administrador");using var c=f.Client();using var login=await f.Login(c,admin.Email,AuthFixture.Password);
        foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})
        {
            var html=WebUtility.HtmlDecode(await c.GetStringAsync("/relatorios/"+kind));Assert.Contains("Nenhum resultado para os filtros aplicados",html);Assert.Contains("Exportar CSV",html);Assert.DoesNotContain("accessToken",html,StringComparison.OrdinalIgnoreCase);
            using var csv=await c.GetAsync("/downloads/relatorios/"+kind+(kind=="estoque"?"":"?"+Period));Assert.Equal(HttpStatusCode.OK,csv.StatusCode);Assert.Equal("text/csv",csv.Content.Headers.ContentType?.MediaType);Assert.Equal("attachment",csv.Content.Headers.ContentDisposition?.DispositionType);Assert.Equal("no-store",csv.Headers.CacheControl?.ToString());
        }
        using(var invalid=await c.GetAsync("/downloads/relatorios/vendas?inicio=2026-01-01")){Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);Assert.Equal("application/problem+json",invalid.Content.Headers.ContentType?.MediaType);Assert.Null(invalid.Content.Headers.ContentDisposition);}
        using(var paged=await c.GetAsync("/downloads/relatorios/estoque?page=1"))Assert.Equal(HttpStatusCode.BadRequest,paged.StatusCode);
        f.OfflinePath="/api/v1/relatorios/estoque/exportacao";try{using var bad=await c.GetAsync("/downloads/relatorios/estoque");Assert.Equal(HttpStatusCode.ServiceUnavailable,bad.StatusCode);Assert.Equal("application/problem+json",bad.Content.Headers.ContentType?.MediaType);Assert.Null(bad.Content.Headers.ContentDisposition);}finally{f.OfflinePath=null;}
        f.OfflinePath="/api/v1/relatorios/estoque";try{Assert.Contains("Não foi possível consultar o relatório",WebUtility.HtmlDecode(await c.GetStringAsync("/relatorios/estoque")));}finally{f.OfflinePath=null;}
        using var anon=f.Client();Assert.Equal(HttpStatusCode.Redirect,(await anon.GetAsync("/downloads/relatorios/estoque")).StatusCode);Assert.Equal(HttpStatusCode.Redirect,(await anon.GetAsync("/relatorios/estoque")).StatusCode);
        foreach(var role in new[]{"Vendedor","Estoquista"})
        {
            var u=await f.Backend.SeedAsync(role);using var denied=f.Client();using var signIn=await f.Login(denied,u.Email,AuthFixture.Password);
            foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})
            {
                var permitted=role=="Estoquista"&&kind is "estoque" or "movimentos";Assert.Equal(permitted,(await denied.GetAsync("/relatorios/"+kind)).StatusCode==HttpStatusCode.OK);using var r=await denied.GetAsync("/downloads/relatorios/"+kind+(kind=="estoque"?"":"?"+Period));Assert.Equal(permitted,r.StatusCode==HttpStatusCode.OK);if(!permitted)Assert.NotEqual("text/csv",r.Content.Headers.ContentType?.MediaType);
            }
            if(role=="Vendedor")Assert.DoesNotContain("href=\"/relatorios/",await denied.GetStringAsync("/"));
        }
    });
}
