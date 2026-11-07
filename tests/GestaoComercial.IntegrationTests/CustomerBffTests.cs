using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using Xunit;

public sealed class CustomerBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact] public Task CustomerPagesRespectPolicyAndRealApiFailure() => f.Mongo.SafeAsync(async()=>
    {
        var seller=await f.Backend.SeedAsync(); using var api=f.Backend.Client((await f.Backend.LoginAsync(seller)).AccessToken);
        using var created=await api.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente BFF","PF",null,null,null)); var row=(await created.Content.ReadFromJsonAsync<ClienteResponse>())!;
        using var c=f.Client(); using var login=await f.Login(c,seller.Email,AuthFixture.Password);
        foreach(var path in new[]{"/clientes","/clientes/novo",$"/clientes/{row.Id}"}) Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(path)).StatusCode);
        Assert.Contains("Cliente BFF",WebUtility.HtmlDecode(await c.GetStringAsync($"/clientes/{row.Id}")));
        using var denied=f.Client(); var stock=await f.Backend.SeedAsync("Estoquista"); using var l=await f.Login(denied,stock.Email,AuthFixture.Password);
        Assert.Contains("acesso-negado",(await denied.GetAsync("/clientes")).Headers.Location!.ToString());
        f.Offline=true; try { Assert.Equal(HttpStatusCode.Redirect,(await c.GetAsync("/clientes")).StatusCode); } finally { f.Offline=false; }
    });
}
