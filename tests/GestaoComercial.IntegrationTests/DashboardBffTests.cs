using System.Net;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Vendas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DashboardBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact]public Task DashboardConsumesRealApiAndHidesMetricsFromOtherRoles()=>f.Mongo.SafeAsync(async()=>
    {
        await f.Backend.Api.Services.GetRequiredService<EstoqueSchema>().InitializeAsync(default);await f.Backend.Api.Services.GetRequiredService<VendasSchema>().InitializeAsync(default);
        using var c=f.Client();var admin=await f.Backend.SeedAsync("Administrador");using var login=await f.Login(c,admin.Email,AuthFixture.Password);
        var html=WebUtility.HtmlDecode(await c.GetStringAsync("/dashboard"));Assert.Contains("Sem vendas confirmadas no período",html);Assert.Contains("Faturamento líquido",html);Assert.DoesNotContain("accessToken",html,StringComparison.OrdinalIgnoreCase);
        f.OfflinePath="/api/v1/dashboard";try{Assert.Contains("Não foi possível consultar os indicadores",WebUtility.HtmlDecode(await c.GetStringAsync("/dashboard")));}finally{f.OfflinePath=null;}
        using var anon=f.Client();Assert.Equal(HttpStatusCode.Redirect,(await anon.GetAsync("/dashboard")).StatusCode);
        foreach(var role in new[]{"Vendedor","Estoquista"}){using var denied=f.Client();var u=await f.Backend.SeedAsync(role);using var l=await f.Login(denied,u.Email,AuthFixture.Password);Assert.NotEqual(HttpStatusCode.OK,(await denied.GetAsync("/dashboard")).StatusCode);Assert.DoesNotContain("href=\"/dashboard\"",await denied.GetStringAsync("/"));}
    });
}
