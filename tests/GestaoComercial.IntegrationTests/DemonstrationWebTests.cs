extern alias WebHost;
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DemonstrationWebTests(SalesFixture fixture):IClassFixture<SalesFixture>
{
    [Fact]
    public Task SharedDatabaseTicketsUpgradeAndAdminPageAuthorization()=>fixture.Auth.Mongo.SafeAsync(async()=>
    {
        var f=fixture.Auth;var config=new WebHost::GestaoComercial.Web.Persistence.MongoConfiguration(f.Mongo.Connection,f.Mongo.Name,"Testing");
        var protection=Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create(new DirectoryInfo(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"demo-keys-"+f.Mongo.Name)));
        var tickets=new WebHost::GestaoComercial.Web.Security.MongoTickets(f.Mongo.Client,config,protection);
        await tickets.InitializeAsync(default);await tickets.InitializeAsync(default);Assert.True(await tickets.IsReadyAsync(default));
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name);var versions=db.GetCollection<BsonDocument>("persistence_versions");
        Assert.Equal(2,(await versions.Find(new BsonDocument("_id","01-authentication")).SingleAsync())["version"].AsInt32);
        Assert.Equal(8,(await versions.Find(new BsonDocument("_id","01-web-tickets")).SingleAsync())["version"].AsInt32);
        using var web=new WebApplicationFactory<WebHost::Program>().WithWebHostBuilder(b=>{b.UseEnvironment("Testing");b.UseSetting("Mongo:ConnectionString",f.Mongo.Connection);b.UseSetting("Mongo:Database",f.Mongo.Name);b.UseSetting("Api:BaseUrl","https://api.test/");});
        using var anonymous=web.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        Assert.Equal(HttpStatusCode.Redirect,(await anonymous.GetAsync("/administracao/dados-demonstracao")).StatusCode);
    });
}
