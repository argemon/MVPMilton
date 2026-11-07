using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using Xunit;

public sealed class ProductBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact]public Task PagesAndImageProxyUseRealSessionWithoutExposingApiToken()=>f.Mongo.SafeAsync(async()=>{
        var stock=await f.Backend.SeedAsync("Estoquista");using var api=f.Backend.Client((await f.Backend.LoginAsync(stock)).AccessToken);var cat=await ProductTests.Reference(api);var p=await ProductTests.Create(api,ProductTests.Data(cat.Id));
        using var upload=await ProductTests.Upload(api,p.Id,p.Versao,ProductTests.Png());p=(await upload.Content.ReadFromJsonAsync<ProdutoResponse>())!;
        using var c=f.Client();using var login=await f.Login(c,stock.Email,AuthFixture.Password);
        foreach(var path in new[]{"/produtos","/produtos/novo",$"/produtos/{p.Id}","/categorias","/marcas"}){var html=await c.GetStringAsync(path);Assert.DoesNotContain("accessToken",html,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain(AuthFixture.Password,html);}
        var detail=await c.GetStringAsync($"/produtos/{p.Id}");Assert.Contains("/imagens/produtos/",detail);
        using var proxy=await c.GetAsync($"/imagens/produtos/{p.Id}");Assert.Equal(HttpStatusCode.OK,proxy.StatusCode);Assert.Equal("image/png",proxy.Content.Headers.ContentType?.MediaType);Assert.Equal(await api.GetByteArrayAsync(p.ImagemUrl),await proxy.Content.ReadAsByteArrayAsync());
        using var anon=f.Client();Assert.Equal(HttpStatusCode.Redirect,(await anon.GetAsync($"/imagens/produtos/{p.Id}")).StatusCode);
        var seller=await f.Backend.SeedAsync();using var read=f.Client();using var l=await f.Login(read,seller.Email,AuthFixture.Password);var readonlyHtml=await read.GetStringAsync($"/produtos/{p.Id}");Assert.DoesNotContain("id=\"sku\"",readonlyHtml);Assert.DoesNotContain("id=\"arquivo\"",readonlyHtml);Assert.Equal(HttpStatusCode.OK,(await read.GetAsync($"/imagens/produtos/{p.Id}")).StatusCode);
        f.Offline=true;try{Assert.NotEqual(HttpStatusCode.OK,(await c.GetAsync($"/imagens/produtos/{p.Id}")).StatusCode);}finally{f.Offline=false;}
    });
}
