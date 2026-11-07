using System.Net;
using GestaoComercial.Infrastructure.Vendas;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class AnalyticsReadinessTests(SalesFixture fixture) : IClassFixture<SalesFixture>
{
    [Fact]public Task IncompletePersistenceCannotLookLikeValidEmptyDashboardOrCsv()=>fixture.Auth.Mongo.SafeAsync(async()=>
    {
        var f=fixture.Auth;using var c=f.Client((await f.LoginAsync(await f.SeedAsync("Administrador"))).AccessToken);
        var catalog=f.Mongo.Client.GetDatabase(f.Mongo.Name).GetCollection<BsonDocument>("persistence_versions");var filter=new BsonDocument("_id",VendasSchema.Id);var original=await catalog.Find(filter).SingleAsync();
        try
        {
            await catalog.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("checksum",new string('0',64))));
            var paths=new List<string>{"/api/v1/dashboard"};foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})foreach(var suffix in new[]{"","/exportacao"})paths.Add("/api/v1/relatorios/"+kind+suffix+(kind=="estoque"?"":"?inicio=2026-01-01&fim=2027-01-01"));
            foreach(var path in paths){using var r=await c.GetAsync(path);Assert.Equal(HttpStatusCode.ServiceUnavailable,r.StatusCode);Assert.Equal("application/problem+json",r.Content.Headers.ContentType?.MediaType);Assert.Null(r.Content.Headers.ContentDisposition);Assert.Contains("PERSISTENCIA_NAO_INICIALIZADA",await r.Content.ReadAsStringAsync());}
        }
        finally{await catalog.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("checksum",original["checksum"])));}
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/dashboard")).StatusCode);
    });
}
