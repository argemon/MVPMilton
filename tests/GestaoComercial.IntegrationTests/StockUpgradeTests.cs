using GestaoComercial.Infrastructure.Identity;
using GestaoComercial.Infrastructure.Funcionarios;
using GestaoComercial.Infrastructure.Clientes;
using GestaoComercial.Infrastructure.Produtos;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class StockUpgradeTests(MongoFixture f) : IClassFixture<MongoFixture>
{
    [Fact]public Task Upgrade04To05IsExplicitPreservesProductsAndRejectsDivergentCatalog()=>f.SafeAsync(async()=>
    {
        await f.Foundation.InitializeAsync(default);var config=new MongoConfiguration(f.Connection,f.Name,"Testing");var auth=new AuthSchema(f.Client,config);await auth.InitializeAsync(default);var employees=new FuncionariosSchema(f.Client,config,auth);await employees.InitializeAsync(default);var customers=new ClientesSchema(f.Client,config,employees);await customers.InitializeAsync(default);var products=new ProdutosSchema(f.Client,config,customers);await products.InitializeAsync(default);
        var db=f.Client.GetDatabase(f.Name);var p=new ProdutoDocumento{Sku="PRESERVADO",SkuNormalizado="PRESERVADO",Nome="Produto anterior",NomeNormalizado="PRODUTO ANTERIOR",CategoriaId=Guid.NewGuid(),PrecoCentavos=1250,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};await db.GetCollection<ProdutoDocumento>("products").InsertOneAsync(p);var before=p.ToBsonDocument();
        var schema=new EstoqueSchema(f.Client,config,products);Assert.False(await schema.IsReadyAsync(default));await schema.InitializeAsync(default);await schema.InitializeAsync(default);Assert.True(await schema.IsReadyAsync(default));
        Assert.Equal(before,await db.GetCollection<BsonDocument>("products").Find(new BsonDocument("_id",new BsonBinaryData(p.Id,GuidRepresentation.Standard))).SingleAsync());
        var catalog=db.GetCollection<BsonDocument>("persistence_versions");var filter=new BsonDocument("_id",EstoqueSchema.Id);var original=await catalog.Find(filter).SingleAsync();
        try{await catalog.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("checksum",new string('0',64)));Assert.False(await schema.IsReadyAsync(default));await Assert.ThrowsAsync<InvalidOperationException>(()=>schema.InitializeAsync(default));Assert.Equal(new string('0',64),(await catalog.Find(filter).SingleAsync())["checksum"].AsString);}finally{await catalog.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("checksum",original["checksum"]));}
        using var indexes=await db.GetCollection<BsonDocument>("stock_movements").Indexes.ListAsync();Assert.Contains(await indexes.ToListAsync(),x=>x["name"]=="ux_stock_operacao"&&x["unique"].AsBoolean);
    });
}
