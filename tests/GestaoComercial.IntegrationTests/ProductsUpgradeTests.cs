using GestaoComercial.Infrastructure.Identity;
using GestaoComercial.Infrastructure.Funcionarios;
using GestaoComercial.Infrastructure.Clientes;
using GestaoComercial.Infrastructure.Produtos;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class ProductsUpgradeTests(MongoFixture f) : IClassFixture<MongoFixture>
{
    [Fact]public Task CleanInstallAndUpgradePreserveEarlierDataRejectBadBsonAndDivergence()=>f.SafeAsync(async()=>{
        await f.Foundation.InitializeAsync(default);var config=new MongoConfiguration(f.Connection,f.Name,"Testing");var auth=new AuthSchema(f.Client,config);await auth.InitializeAsync(default);var employees=new FuncionariosSchema(f.Client,config,auth);await employees.InitializeAsync(default);var customers=new ClientesSchema(f.Client,config,employees);await customers.InitializeAsync(default);
        var db=f.Client.GetDatabase(f.Name);var people=db.GetCollection<ClienteDocumento>("customers");var before=new ClienteDocumento{Nome="Cliente Preservado",NomeNormalizado="CLIENTE PRESERVADO",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};await people.InsertOneAsync(before);
        var schema=new ProdutosSchema(f.Client,config,customers);Assert.False(await schema.IsReadyAsync(default));await schema.InitializeAsync(default);await schema.InitializeAsync(default);Assert.True(await schema.IsReadyAsync(default));Assert.Equal(before.Nome,(await people.Find(x=>x.Id==before.Id).SingleAsync()).Nome);
        var category=new ReferenciaDocumento{Nome="Categoria",NomeNormalizado="CATEGORIA",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};await db.GetCollection<ReferenciaDocumento>("categories").InsertOneAsync(category);
        var p=new ProdutoDocumento{Sku="SKU",SkuNormalizado="SKU",Nome="Produto",NomeNormalizado="PRODUTO",CategoriaId=category.Id,PrecoCentavos=1250,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};var products=db.GetCollection<ProdutoDocumento>("products");await products.InsertOneAsync(p);
        var raw=db.GetCollection<BsonDocument>("products");var invalid=p.ToBsonDocument();invalid["_id"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard);invalid["skuNormalizado"]="INVALID";invalid["saldo"]["quantidade"]=-1;
        Assert.Equal(121,(await Assert.ThrowsAsync<MongoWriteException>(()=>raw.InsertOneAsync(invalid))).WriteError.Code);
        invalid["saldo"]["quantidade"]=0;invalid["precoCentavos"]=12.50;Assert.Equal(121,(await Assert.ThrowsAsync<MongoWriteException>(()=>raw.InsertOneAsync(invalid))).WriteError.Code);
        using var indexes=await products.Indexes.ListAsync();Assert.Contains(await indexes.ToListAsync(),x=>x["name"]=="ux_products_sku"&&x["unique"].AsBoolean);
        var catalog=db.GetCollection<BsonDocument>("persistence_versions");var filter=new BsonDocument("_id",ProdutosSchema.Id);var original=await catalog.Find(filter).SingleAsync();
        try{await catalog.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("checksum",new string('0',64)));Assert.False(await schema.IsReadyAsync(default));await Assert.ThrowsAsync<InvalidOperationException>(()=>schema.InitializeAsync(default));Assert.Equal(new string('0',64),(await catalog.Find(filter).SingleAsync())["checksum"].AsString);}finally{await catalog.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("checksum",original["checksum"]));}
    });
}
