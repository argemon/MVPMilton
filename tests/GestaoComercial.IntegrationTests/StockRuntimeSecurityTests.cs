using System.Security.Cryptography;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

// Requires the explicitly authenticated Testcontainers profile; Atlas accounts are not altered.
[Trait("Category","ContainerSecurity")]
public sealed class StockRuntimeSecurityTests(StockFixture f) : IClassFixture<StockFixture>
{
    [Fact] public Task RuntimeCannotRewriteHistoryChangeSchemaOrReadAnotherDatabase()=>f.Auth.Mongo.SafeAsync(async()=>
    {
        Assert.Equal("true",Environment.GetEnvironmentVariable("TEST_MONGO_CONTAINER"));
        var mongo=f.Auth.Mongo;var db=mongo.Client.GetDatabase(mongo.Name);
        var privileges=new BsonArray();
        foreach(var collection in new[]{"products","users","api_sessions","idempotency_receipts","sales"})privileges.Add(new BsonDocument{{"resource",new BsonDocument{{"db",mongo.Name},{"collection",collection}}},{"actions",new BsonArray{"find","insert","update"}}});
        foreach(var collection in new[]{"stock_movements","audit_events"})privileges.Add(new BsonDocument{{"resource",new BsonDocument{{"db",mongo.Name},{"collection",collection}}},{"actions",new BsonArray{"find","insert"}}});
        await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"createRole","stock_runtime_test"},{"privileges",privileges},{"roles",new BsonArray()}});
        var password=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"createUser","stock_runtime_test"},{"pwd",password},{"roles",new BsonArray{"stock_runtime_test"}}});
        var settings=MongoClientSettings.FromConnectionString(mongo.Connection);settings.Credential=MongoCredential.CreateCredential(mongo.Name,"stock_runtime_test",password);
        using var runtime=new MongoClient(settings);var own=runtime.GetDatabase(mongo.Name);
        // A permitted read proves this is a valid authenticated account rather than a broken connection.
        Assert.Equal(0,await own.GetCollection<BsonDocument>("stock_movements").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        foreach(var collection in new[]{"stock_movements","audit_events"})
        {
            var rows=own.GetCollection<BsonDocument>(collection);
            await Denied(()=>rows.UpdateManyAsync(FilterDefinition<BsonDocument>.Empty,new BsonDocument("$set",new BsonDocument("motivo","alterado"))));
            await Denied(()=>rows.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty));
        }
        await Denied(()=>own.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","stock_movements"},{"validator",new BsonDocument()}}));
        await Denied(()=>own.GetCollection<BsonDocument>("stock_movements").InsertOneAsync(new BsonDocument("_id","bad"),new InsertOneOptions{BypassDocumentValidation=true}));
        await Denied(()=>runtime.GetDatabase(mongo.Name+"x").GetCollection<BsonDocument>("tickets").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        await Denied(()=>own.GetCollection<BsonDocument>("sales").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty));
    });
    private static async Task Denied(Func<Task> command)
    {
        var error=await Assert.ThrowsAnyAsync<MongoException>(command);
        Assert.True(error is MongoCommandException{Code:13} or MongoWriteException{WriteError.Code:13},"Expected MongoDB authorization denial.");
    }
}
