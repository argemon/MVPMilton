using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Produtos;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using SkiaSharp;
using Xunit;

public sealed class ProductTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    private async Task<HttpClient> User(string role="Estoquista")=>f.Client((await f.LoginAsync(await f.SeedAsync(role))).AccessToken);
    internal static async Task<ReferenciaResponse> Reference(HttpClient c,bool brand=false){using var r=await c.PostAsJsonAsync(brand?"/api/v1/marcas":"/api/v1/categorias",new CriarReferenciaRequest("Referência "+Guid.NewGuid()));Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<ReferenciaResponse>())!;}
    internal static CriarProdutoRequest Data(Guid category,Guid? brand=null)=>new("SKU-"+Guid.NewGuid().ToString("N"),"Produto Sintético",null,category,brand,12.50m,0);
    internal static async Task<ProdutoResponse> Create(HttpClient c,CriarProdutoRequest data){using var r=await c.PostAsJsonAsync("/api/v1/produtos",data);Assert.Equal(HttpStatusCode.Created,r.StatusCode);Assert.NotNull(r.Headers.Location);return(await r.Content.ReadFromJsonAsync<ProdutoResponse>())!;}
    internal static AtualizarProdutoRequest Update(ProdutoResponse p,bool active=true)=>new(p.Sku,p.Nome,p.Descricao,p.CategoriaId,p.MarcaId,p.Preco,p.EstoqueMinimo,active,p.Versao);
    internal static byte[] Png(){using var bitmap=new SKBitmap(3,3);bitmap.Erase(SKColors.Red);using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,100);return data.ToArray();}
    internal static async Task<HttpResponseMessage> Upload(HttpClient c,Guid id,long version,byte[] bytes){using var form=new MultipartFormDataContent();form.Add(new StringContent(version.ToString()),"versao");form.Add(new ByteArrayContent(bytes),"arquivo","photo.jpg");return await c.PostAsync($"/api/v1/produtos/{id}/imagem",form);}
    [Fact]public Task ProductCrudFiltersPreserveZeroBalanceAndStoreInt64() => f.Mongo.SafeAsync(async()=>{
        using var c=await User();var cat=await Reference(c);var brand=await Reference(c,true);var d=Data(cat.Id,brand.Id);var p=await Create(c,d);Assert.Equal(0,p.Saldo);Assert.Equal(12.50m,p.Preco);
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name);var raw=await db.GetCollection<BsonDocument>("products").Find(new BsonDocument("_id",new BsonBinaryData(p.Id,GuidRepresentation.Standard))).SingleAsync();Assert.Equal(BsonType.Int64,raw["precoCentavos"].BsonType);Assert.Equal(1250L,raw["precoCentavos"].AsInt64);Assert.Equal(0,raw["saldo"]["quantidade"].AsInt32);
        foreach(var query in new[]{"search="+p.Sku.ToLowerInvariant(),"search=Produto&categoriaId="+cat.Id+"&marcaId="+brand.Id})Assert.Contains((await c.GetFromJsonAsync<ProdutosResponse>("/api/v1/produtos?"+query))!.Items,x=>x.Id==p.Id);
        Assert.Empty((await c.GetFromJsonAsync<ProdutosResponse>("/api/v1/produtos?categoriaId="+Guid.NewGuid()))!.Items);Assert.Empty((await c.GetFromJsonAsync<ProdutosResponse>("/api/v1/produtos?page=999&pageSize=1"))!.Items);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/v1/produtos",d with{Sku=" "+d.Sku!.ToLowerInvariant()+" "})).StatusCode);
        using var changed=await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p,false) with{Nome="Nome alterado",Preco=15.01m});Assert.Equal(HttpStatusCode.OK,changed.StatusCode);var row=(await changed.Content.ReadFromJsonAsync<ProdutoResponse>())!;Assert.Equal(0,row.Saldo);Assert.Equal(15.01m,row.Preco);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p))).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<ProdutosResponse>("/api/v1/produtos?search="+p.Sku))!.Items);Assert.Single((await c.GetFromJsonAsync<ProdutosResponse>("/api/v1/produtos?ativo=todos&search="+p.Sku))!.Items);
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(row))).StatusCode);
        using var names=await db.ListCollectionNamesAsync();var collections=await names.ToListAsync();Assert.DoesNotContain("stock_movements",collections);Assert.DoesNotContain("sales",collections);
    });
    [Theory][InlineData(false)][InlineData(true)]public Task ReferenceCrudUniqueIncludingInactiveAndInUseProtection(bool brand)=>f.Mongo.SafeAsync(async()=>{
        using var c=await User();var r=await Reference(c,brand);var path="/api/v1/"+(brand?"marcas":"categorias");
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(path,new CriarReferenciaRequest(" "+r.Nome.ToUpperInvariant()+" "))).StatusCode);
        var cat=brand?await Reference(c):r;var p=await Create(c,Data(cat.Id,brand?r.Id:null));
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"{path}/{r.Id}",new AtualizarReferenciaRequest(r.Nome,false,r.Versao))).StatusCode);
        await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p,false));
        using var disabled=await c.PutAsJsonAsync($"{path}/{r.Id}",new AtualizarReferenciaRequest(r.Nome,false,r.Versao));Assert.Equal(HttpStatusCode.OK,disabled.StatusCode);
        var current=(await disabled.Content.ReadFromJsonAsync<ReferenciaResponse>())!;Assert.False(current.Ativo);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(path,new CriarReferenciaRequest(r.Nome))).StatusCode);
        p=(await c.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!;
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"{path}/{r.Id}",new AtualizarReferenciaRequest(r.Nome,true,current.Versao))).StatusCode);
        Assert.Contains((await c.GetFromJsonAsync<ReferenciasResponse>(path+"?search="+Uri.EscapeDataString(r.Nome)))!.Items,x=>x.Id==r.Id);
    });
    [Fact]public Task RejectsInvalidPriceFieldsReferencesAndMassAssignment()=>f.Mongo.SafeAsync(async()=>{
        using var c=await User();var cat=await Reference(c);var d=Data(cat.Id);
        foreach(var bad in new[]{d with{Sku="bad sku"},d with{Nome=null},d with{Preco=0},d with{Preco=12.505m},d with{Preco=10000000m},d with{EstoqueMinimo=-1},d with{Descricao="<b>bad</b>"},d with{CategoriaId=Guid.Empty}})Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/v1/produtos",bad)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync("/api/v1/produtos",d with{CategoriaId=Guid.NewGuid()})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/v1/produtos",new{sku="SKU",nome="Produto",categoriaId=cat.Id,preco=12.50m,estoqueMinimo=0,saldo=10})).StatusCode);
        foreach(var path in new[]{"/api/v1/produtos?pageSize=101","/api/v1/produtos?categoriaId=bad","/api/v1/categorias?ativo=bad","/api/v1/marcas?page=0"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(path)).StatusCode);
    });
    [Fact]public Task ConcurrentSkuAndReferenceInactivationHaveSafeOutcomes()=>f.Mongo.SafeAsync(async()=>{
        using var c=await User();var cat=await Reference(c);var d=Data(cat.Id);var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Add(){await gate.Task;using var r=await c.PostAsJsonAsync("/api/v1/produtos",d);return r.StatusCode;}
        var a=Add();var b=Add();gate.SetResult();var results=await Task.WhenAll(a,b);Assert.Single(results,x=>x==HttpStatusCode.Created);Assert.Single(results,x=>x==HttpStatusCode.Conflict);
        cat=await Reference(c);d=Data(cat.Id);var barrier=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> AddWithGuard(){await barrier.Task;using var r=await c.PostAsJsonAsync("/api/v1/produtos",d);return r.StatusCode;}
        async Task<HttpStatusCode> Disable(){await barrier.Task;using var r=await c.PutAsJsonAsync($"/api/v1/categorias/{cat.Id}",new AtualizarReferenciaRequest(cat.Nome,false,cat.Versao));return r.StatusCode;}
        var creation=AddWithGuard();var disabling=Disable();barrier.SetResult();var outcomes=await Task.WhenAll(creation,disabling);
        Assert.True(outcomes.SequenceEqual(new[]{HttpStatusCode.Created,HttpStatusCode.Conflict})||outcomes.SequenceEqual(new[]{HttpStatusCode.Conflict,HttpStatusCode.OK}));
        var rows=(await c.GetFromJsonAsync<ProdutosResponse>("/api/v1/produtos?categoriaId="+cat.Id))!;var category=(await c.GetFromJsonAsync<ReferenciaResponse>($"/api/v1/categorias/{cat.Id}"))!;Assert.True(rows.Items.Count==0||category.Ativo);
        var name="Nome concorrente "+Guid.NewGuid();var nameGate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Named(){await nameGate.Task;using var r=await c.PostAsJsonAsync("/api/v1/categorias",new CriarReferenciaRequest(name));return r.StatusCode;}
        var n1=Named();var n2=Named();nameGate.SetResult();var names=await Task.WhenAll(n1,n2);Assert.Single(names,x=>x==HttpStatusCode.Created);Assert.Single(names,x=>x==HttpStatusCode.Conflict);
    });
    [Theory][InlineData("Administrador",true)][InlineData("Estoquista",true)][InlineData("Vendedor",false)]public Task EnforcesPoliciesForEveryRoute(string role,bool write)=>f.Mongo.SafeAsync(async()=>{
        using var setup=await User();var cat=await Reference(setup);var brand=await Reference(setup,true);var p=await Create(setup,Data(cat.Id));
        using var c=await User(role);using var anon=f.Client();
        foreach(var path in new[]{"/api/v1/produtos",$"/api/v1/produtos/{p.Id}","/api/v1/categorias",$"/api/v1/categorias/{cat.Id}","/api/v1/marcas",$"/api/v1/marcas/{brand.Id}"}){Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(path)).StatusCode);}
        var denied=HttpStatusCode.Forbidden;
        Assert.Equal(write?HttpStatusCode.Created:denied,(await c.PostAsJsonAsync("/api/v1/produtos",Data(cat.Id))).StatusCode);
        Assert.Equal(write?HttpStatusCode.OK:denied,(await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p))).StatusCode);
        foreach(var path in new[]{"/api/v1/categorias","/api/v1/marcas"})Assert.Equal(write?HttpStatusCode.Created:denied,(await c.PostAsJsonAsync(path,new CriarReferenciaRequest("Nome "+Guid.NewGuid()))).StatusCode);
        Assert.Equal(write?HttpStatusCode.OK:denied,(await c.PutAsJsonAsync($"/api/v1/categorias/{cat.Id}",new AtualizarReferenciaRequest(cat.Nome,true,cat.Versao))).StatusCode);
        Assert.Equal(write?HttpStatusCode.OK:denied,(await c.PutAsJsonAsync($"/api/v1/marcas/{brand.Id}",new AtualizarReferenciaRequest(brand.Nome,true,brand.Versao))).StatusCode);
        p=(await setup.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!;
        using var upload=await Upload(c,p.Id,p.Versao,Png());Assert.Equal(write?HttpStatusCode.OK:denied,upload.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PostAsJsonAsync("/api/v1/produtos",Data(cat.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync($"/api/v1/produtos/{p.Id}/imagem")).StatusCode);
        using var anonUpload=await Upload(anon,p.Id,p.Versao,Png());Assert.Equal(HttpStatusCode.Unauthorized,anonUpload.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.DeleteAsync($"/api/v1/produtos/{p.Id}/imagem?versao={p.Versao}")).StatusCode);
        if(!write)Assert.Equal(denied,(await c.DeleteAsync($"/api/v1/produtos/{p.Id}/imagem?versao={p.Versao}")).StatusCode);
    });
    [Fact]public Task ImagesArePrivateVersionedReencodedAndBadUploadsPreservePrevious()=>f.Mongo.SafeAsync(async()=>{
        using var c=await User();var cat=await Reference(c);var p=await Create(c,Data(cat.Id));
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/produtos/{p.Id}/imagem")).StatusCode);
        using var first=await Upload(c,p.Id,p.Versao,Png());Assert.Equal(HttpStatusCode.OK,first.StatusCode);p=(await first.Content.ReadFromJsonAsync<ProdutoResponse>())!;Assert.NotNull(p.ImagemUrl);
        using var picture=await c.GetAsync(p.ImagemUrl);Assert.Equal("image/png",picture.Content.Headers.ContentType?.MediaType);var bytes=await picture.Content.ReadAsByteArrayAsync();
        using var fake=await Upload(c,p.Id,p.Versao,"<svg/>"u8.ToArray());Assert.Equal(HttpStatusCode.UnsupportedMediaType,fake.StatusCode);
        using var huge=await Upload(c,p.Id,p.Versao,new byte[2097153]);Assert.Equal(HttpStatusCode.RequestEntityTooLarge,huge.StatusCode);
        using var stale=await Upload(c,p.Id,p.Versao-1,Png());Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        Assert.Equal(bytes,await c.GetByteArrayAsync(p.ImagemUrl));
        Assert.Equal(HttpStatusCode.Conflict,(await c.DeleteAsync(p.ImagemUrl+"?versao="+(p.Versao-1))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync(p.ImagemUrl+"?versao="+p.Versao)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(p.ImagemUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.DeleteAsync(p.ImagemUrl+"?versao="+(p.Versao+1))).StatusCode);
    });
    [Fact]public Task ConcurrentImageReplacementsKeepCommittedFileAndCleanupNeverRemovesReferences()=>f.Mongo.SafeAsync(async()=>{
        using var c=await User();var cat=await Reference(c);var p=await Create(c,Data(cat.Id));using var first=await Upload(c,p.Id,p.Versao,Png());p=(await first.Content.ReadFromJsonAsync<ProdutoResponse>())!;
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name);var products=db.GetCollection<ProdutoDocumento>("products");var old=(await products.Find(x=>x.Id==p.Id).SingleAsync()).Imagem!.Chave;
        var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Replace(){await gate.Task;using var r=await Upload(c,p.Id,p.Versao,Png());return r.StatusCode;}
        var a=Replace();var b=Replace();gate.SetResult();var results=await Task.WhenAll(a,b);Assert.Single(results,x=>x==HttpStatusCode.OK);Assert.Single(results,x=>x==HttpStatusCode.Conflict);
        var current=await products.Find(x=>x.Id==p.Id).SingleAsync();Assert.Equal(p.Versao+1,current.Versao);var images=f.Api.Services.GetRequiredService<ImagensPrivadas>();Assert.True(File.Exists(images.Caminho(current.Imagem!.Chave)));Assert.False(File.Exists(images.Caminho(old)));
        using var scope=f.Api.Services.CreateScope();Assert.True(await scope.ServiceProvider.GetRequiredService<MongoProdutos>().LimparOrfaosAsync(default)>0);Assert.True(File.Exists(images.Caminho(current.Imagem.Chave)));Assert.Equal(Png(),await c.GetByteArrayAsync($"/api/v1/produtos/{p.Id}/imagem"));
    });
    [Fact]public Task AuditFailureAbortsProductAndImageMetadataAndRetainsOldFile()=>f.Mongo.SafeAsync(async()=>{
        using var c=await User();var cat=await Reference(c);var p=await Create(c,Data(cat.Id));using var upload=await Upload(c,p.Id,p.Versao,Png());p=(await upload.Content.ReadFromJsonAsync<ProdutoResponse>())!;var oldBytes=await c.GetByteArrayAsync(p.ImagemUrl);
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name);using var coll=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")});var validator=(await coll.SingleAsync())["options"]["validator"].AsBsonDocument;
        try{
            await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("recursoTipo",new BsonDocument("$ne","Produto"))})}});
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",Update(p)with{Preco=16m})).StatusCode);
            using var failed=await Upload(c,p.Id,p.Versao,Png());Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);
            Assert.Equal(p,(await c.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!);Assert.Equal(oldBytes,await c.GetByteArrayAsync(p.ImagemUrl));
        }finally{await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}});}
        using var scope=f.Api.Services.CreateScope();var service=scope.ServiceProvider.GetRequiredService<MongoProdutos>();Assert.True(await service.LimparOrfaosAsync(default)>0);Assert.Equal(oldBytes,await c.GetByteArrayAsync(p.ImagemUrl));
    });
}
