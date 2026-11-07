using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Vendas;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class SalesUpgradeTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    [Fact]public Task UpgradePreservesManualMovementsAndReceiptsAndReinitializesWholeChain()=>f.Mongo.SafeAsync(async()=>
    {
        var stock=f.Api.Services.GetRequiredService<EstoqueSchema>();using var c=f.Client((await f.LoginAsync(await f.SeedAsync("Administrador"))).AccessToken);var p=await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await StockTests.Move(c,p.Id,"entradas",4)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await SalesTests.Command(c,"/api/v1/vendas",new CriarVendaRequest(Guid.NewGuid(),[]))).StatusCode);
        using(var names=await f.Mongo.Client.GetDatabase(f.Mongo.Name).ListCollectionNamesAsync()){var list=await names.ToListAsync();Assert.DoesNotContain("sales",list);Assert.DoesNotContain("stock_movements",list);Assert.DoesNotContain("idempotency_receipts",list);}
        await stock.InitializeAsync(default);var key=Guid.NewGuid();using var entry=await StockTests.Move(c,p.Id,"entradas",4,key);Assert.Equal(HttpStatusCode.Created,entry.StatusCode);var movement=(await entry.Content.ReadFromJsonAsync<MovimentoResponse>())!;
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name);var movements=db.GetCollection<BsonDocument>("stock_movements");var receipts=db.GetCollection<BsonDocument>("idempotency_receipts");var mf=new BsonDocument("_id",new BsonBinaryData(movement.Id,GuidRepresentation.Standard));var rf=new BsonDocument("chave",new BsonBinaryData(key,GuidRepresentation.Standard));var before=await movements.Find(mf).SingleAsync();var receipt=await receipts.Find(rf).SingleAsync();var schema=f.Api.Services.GetRequiredService<VendasSchema>();Assert.False(await schema.IsReadyAsync(default));await schema.InitializeAsync(default);await stock.InitializeAsync(default);await schema.InitializeAsync(default);Assert.True(await schema.IsReadyAsync(default));Assert.Equal(before,await movements.Find(mf).SingleAsync());Assert.Equal(receipt,await receipts.Find(rf).SingleAsync());
        using var replay=await StockTests.Move(c,p.Id,"entradas",4,key);Assert.Equal(movement,(await replay.Content.ReadFromJsonAsync<MovimentoResponse>())!);Assert.Equal(4,(await c.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!.Saldo);
        var catalog=db.GetCollection<BsonDocument>("persistence_versions");var filter=new BsonDocument("_id",VendasSchema.Id);var marker=await catalog.Find(filter).SingleAsync();try{await catalog.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("checksum",new string('0',64)));Assert.False(await schema.IsReadyAsync(default));await Assert.ThrowsAsync<InvalidOperationException>(()=>schema.InitializeAsync(default));Assert.Equal(new string('0',64),(await catalog.Find(filter).SingleAsync())["checksum"].AsString);}finally{await catalog.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("checksum",marker["checksum"]));}
    });
}
