using System.Net;
using GestaoComercial.Infrastructure.Produtos;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

[Trait("Category","ContainerScale")]
public sealed class ReportsScaleTests(SalesFixture fixture) : IClassFixture<SalesFixture>
{
    [Fact]public Task TenThousandRowsExportCompletelyAndTenThousandAndOneIsRejectedBeforeAttachment()=>fixture.Auth.Mongo.SafeAsync(async()=>
    {
        var f=fixture.Auth;using var c=f.Client((await f.LoginAsync(await f.SeedAsync("Administrador"))).AccessToken);var now=DateTime.UtcNow;var category=Guid.NewGuid();var products=f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<ProdutoDocumento>("products");
        await products.InsertManyAsync(Enumerable.Range(0,10000).Select(i=>new ProdutoDocumento{Sku="EXPORT-"+i,SkuNormalizado="EXPORT-"+i,Nome="Produto exportação "+i,NomeNormalizado="PRODUTO EXPORTAÇÃO "+i,CategoriaId=category,PrecoCentavos=1250,CreatedAt=now,UpdatedAt=now}));
        using(var ok=await c.GetAsync("/api/v1/relatorios/estoque/exportacao")){Assert.Equal(HttpStatusCode.OK,ok.StatusCode);Assert.Equal("text/csv",ok.Content.Headers.ContentType?.MediaType);Assert.Equal(10002,(await ok.Content.ReadAsStringAsync()).Split("\r\n").Length);}
        await products.InsertOneAsync(new ProdutoDocumento{Sku="EXPORT-10000",SkuNormalizado="EXPORT-10000",Nome="Produto extra",NomeNormalizado="PRODUTO EXTRA",CategoriaId=category,PrecoCentavos=1250,CreatedAt=now,UpdatedAt=now});
        using var rejected=await c.GetAsync("/api/v1/relatorios/estoque/exportacao");Assert.Equal(HttpStatusCode.BadRequest,rejected.StatusCode);Assert.Equal("application/problem+json",rejected.Content.Headers.ContentType?.MediaType);Assert.Null(rejected.Content.Headers.ContentDisposition);Assert.Contains("EXPORTACAO_EXCEDE_LIMITE",await rejected.Content.ReadAsStringAsync());
        var projection=new BsonDocument{{"_id",1},{"sku",1},{"nome",1}};var explain=await products.Database.RunCommandAsync<BsonDocument>(new BsonDocument{{"explain",new BsonDocument{{"find","products"},{"filter",new BsonDocument("ativo",true)},{"sort",new BsonDocument{{"nomeNormalizado",1},{"_id",1}}},{"limit",20},{"projection",projection}}},{"verbosity","executionStats"}});Assert.True(explain["executionStats"]["executionSuccess"].AsBoolean);Assert.Equal(20,explain["executionStats"]["nReturned"].AsInt32);
    });
}
