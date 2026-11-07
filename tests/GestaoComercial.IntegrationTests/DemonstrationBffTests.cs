extern alias WebHost;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;
public sealed class DemonstrationBffTests(BffFixture f):IClassFixture<BffFixture>
{
    [Fact]
    public Task PageNavigationAndConfirmationAreAdministratorOnly()=>f.Mongo.SafeAsync(async()=>
    {
        var versions=f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("persistence_versions");
        await versions.UpdateOneAsync(new BsonDocument("_id","01-web-tickets"),new BsonDocument("$set",new BsonDocument("version",2)));
        await f.Web.Services.GetRequiredService<WebHost::GestaoComercial.Web.Security.MongoTickets>().InitializeAsync(default);
        Assert.Equal(8,(await versions.Find(new BsonDocument("_id","01-web-tickets")).SingleAsync())["version"].AsInt32);
        foreach(var role in new[]{"Administrador","Vendedor","Estoquista"})
        {
            var user=await f.Backend.SeedAsync(role);using var c=f.Client();using var login=await f.Login(c,user.Email,AuthFixture.Password);
            using var page=await c.GetAsync("/administracao/dados-demonstracao");
            if(role=="Administrador"){Assert.Equal(HttpStatusCode.OK,page.StatusCode);var html=WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());Assert.Contains("Gerar carga",html);Assert.Contains("Restaurar carga",html);Assert.Contains("Confirmo remover",html);Assert.DoesNotContain("accessToken",html,StringComparison.OrdinalIgnoreCase);}
            else Assert.NotEqual(HttpStatusCode.OK,page.StatusCode);
        }
    });
}
