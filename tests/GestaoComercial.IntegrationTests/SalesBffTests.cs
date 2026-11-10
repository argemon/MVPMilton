using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Vendas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class SalesBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact]public Task SalesAndCustomerHistoryUseRealBffSessionAndProtectStockRole()=>f.Mongo.SafeAsync(async()=>
    {
        await f.Backend.Api.Services.GetRequiredService<EstoqueSchema>().InitializeAsync(default);await f.Backend.Api.Services.GetRequiredService<VendasSchema>().InitializeAsync(default);var seller=await f.Backend.SeedAsync("Vendedor");using var api=f.Backend.Client((await f.Backend.LoginAsync(seller)).AccessToken);
        using var created=await api.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente BFF Venda","PF",null,null,null));var customer=(await created.Content.ReadFromJsonAsync<ClienteResponse>())!;using var draft=await SalesTests.Command(api,"/api/v1/vendas",new CriarVendaRequest(customer.Id,[]));var sale=(await draft.Content.ReadFromJsonAsync<VendaResponse>())!;
        using var c=f.Client();using var login=await f.Login(c,seller.Email,AuthFixture.Password);foreach(var path in new[]{"/vendas","/vendas/nova",$"/vendas/{sale.Id}"}){var html=await c.GetStringAsync(path);Assert.DoesNotContain("accessToken",html,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain(AuthFixture.Password,html);}
        var pdv=await c.GetStringAsync($"/vendas/{sale.Id}");
        Assert.Contains("Frente de caixa",pdv);Assert.Contains("Cupom de venda",pdv);
        Assert.Contains("pdv.css",pdv);Assert.Contains("Buscar por nome ou SKU",pdv);
        Assert.Contains("Salvar rascunho",pdv);Assert.DoesNotContain("id=\"produto\"",pdv);
        Assert.DoesNotContain("Concluir venda",pdv); // Empty draft cannot be confirmed.
        using var anon=f.Client();Assert.Equal(HttpStatusCode.Redirect,(await anon.GetAsync("/vendas")).StatusCode);
        var stock=await f.Backend.SeedAsync("Estoquista");using var denied=f.Client();using var second=await f.Login(denied,stock.Email,AuthFixture.Password);Assert.NotEqual(HttpStatusCode.OK,(await denied.GetAsync("/vendas")).StatusCode);Assert.DoesNotContain("href=\"/vendas\"",await denied.GetStringAsync("/estoque"));
    });
}
