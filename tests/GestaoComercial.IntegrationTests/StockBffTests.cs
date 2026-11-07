using System.Net;
using GestaoComercial.Infrastructure.Estoque;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class StockBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact] public Task StockPagesUseProtectedBffAndSellerOnlySeesBalances()=>f.Mongo.SafeAsync(async()=>
    {
        await f.Backend.Api.Services.GetRequiredService<EstoqueSchema>().InitializeAsync(default);
        var admin=await f.Backend.SeedAsync("Administrador");using var c=f.Client();using var login=await f.Login(c,admin.Email,AuthFixture.Password);
        foreach(var path in new[]{"/estoque","/estoque/movimentos","/estoque/entrada","/estoque/saida","/estoque/ajuste"})
        { var html=await c.GetStringAsync(path);Assert.DoesNotContain("accessToken",html,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain(AuthFixture.Password,html);Assert.Contains("Estoque",html,StringComparison.OrdinalIgnoreCase); }
        using var anon=f.Client();Assert.Equal(HttpStatusCode.Redirect,(await anon.GetAsync("/estoque")).StatusCode);
        var seller=await f.Backend.SeedAsync();using var read=f.Client();using var other=await f.Login(read,seller.Email,AuthFixture.Password);
        var balance=await read.GetStringAsync("/estoque");Assert.DoesNotContain("Registrar entrada",balance);Assert.DoesNotContain("Ajustar estoque",balance);
        Assert.NotEqual(HttpStatusCode.OK,(await read.GetAsync("/estoque/entrada")).StatusCode);
        var employee=await f.Backend.SeedAsync("Estoquista");using var worker=f.Client();using var wl=await f.Login(worker,employee.Email,AuthFixture.Password);Assert.DoesNotContain("id=\"motivo\"",await worker.GetStringAsync("/estoque/ajuste"));
    });
}
