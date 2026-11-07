using System.Diagnostics;
using GestaoComercial.Infrastructure.Produtos;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

[Trait("Category","ContainerScale")]
public sealed class StockQueryTests(StockFixture f,ITestOutputHelper output) : IClassFixture<StockFixture>
{
    [Fact]public Task LowStockPlanIsMeasuredWithTenThousandProducts()=>f.Auth.Mongo.SafeAsync(async()=>
    {
        var db=f.Auth.Mongo.Client.GetDatabase(f.Auth.Mongo.Name);var now=DateTime.UtcNow;var category=Guid.NewGuid();
        await db.GetCollection<ProdutoDocumento>("products").InsertManyAsync(Enumerable.Range(0,10000).Select(i=>new ProdutoDocumento{Sku="VOLUME-"+i,SkuNormalizado="VOLUME-"+i,Nome="Produto volume "+i,NomeNormalizado="PRODUTO VOLUME "+i,CategoriaId=category,PrecoCentavos=1250,CreatedAt=now,UpdatedAt=now}));
        var filter=new BsonDocument{{"ativo",true},{"$expr",new BsonDocument("$lte",new BsonArray{"$saldo.quantidade","$estoqueMinimo"})}};
        var query=new BsonDocument{{"find","products"},{"filter",filter},{"sort",new BsonDocument{{"saldo.quantidade",1},{"nomeNormalizado",1},{"_id",1}}},{"limit",20},{"projection",new BsonDocument{{"_id",1},{"sku",1},{"nome",1},{"saldo.quantidade",1},{"estoqueMinimo",1},{"ativo",1}}}};
        var sw=Stopwatch.StartNew();var plan=await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"explain",query},{"verbosity","executionStats"}});sw.Stop();
        Assert.Equal(20,plan["executionStats"]["nReturned"].AsInt32);Assert.True(plan["executionStats"]["executionSuccess"].AsBoolean);
        output.WriteLine("10.000 produtos: explain roundtrip={0}ms, executionTimeMillis={1}, docsExamined={2}, nReturned=20. Sem alegar p95/carga20 usuários.",sw.ElapsedMilliseconds,plan["executionStats"]["executionTimeMillis"],plan["executionStats"]["totalDocsExamined"]);
        Assert.InRange(sw.ElapsedMilliseconds,0,2000);
    });
}
