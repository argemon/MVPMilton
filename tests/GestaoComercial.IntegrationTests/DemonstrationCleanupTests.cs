using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class DemonstrationCleanupTests(SalesFixture fixture):IClassFixture<SalesFixture>
{
    private const string Path="/api/v1/administracao/dados-demonstracao";
    [Fact]
    public Task CleanupPreservesActorAndEmployeeRevokesOthersRollsBackAndAllowsFreshLoad()=>fixture.Auth.Mongo.SafeAsync(async()=>
    {
        var f=fixture.Auth;var db=f.Mongo.Client.GetDatabase(f.Mongo.Name);
        IMongoCollection<BsonDocument> Col(string n)=>db.GetCollection<BsonDocument>(n);
        var admin=await f.SeedAsync("Administrador");var login=await f.LoginAsync(admin);
        using var c=f.Client(login.AccessToken);c.Timeout=TimeSpan.FromMinutes(15);
        var other=await f.SeedAsync("Administrador");using var otherClient=f.Client((await f.LoginAsync(other)).AccessToken);
        using var employeeResponse=await c.PostAsJsonAsync("/api/v1/funcionarios",new CriarFuncionarioRequest("Administrador preservado",null,null));
        Assert.Equal(HttpStatusCode.Created,employeeResponse.StatusCode);
        var employee=(await employeeResponse.Content.ReadFromJsonAsync<FuncionarioResponse>())!;
        await f.Users.UpdateOneAsync(x=>x.Id==admin.Id,Builders<GestaoComercial.Infrastructure.Identity.UsuarioIdentity>.Update.Set(x=>x.EmployeeId,employee.Id));
        using var manual=await c.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente manual a apagar","PF",null,null,null));
        Assert.Equal(HttpStatusCode.Created,manual.StatusCode);
        var operation=Guid.NewGuid();var request=new LimparDadosRequest(operation,"APAGAR DADOS");
        using(var no=await c.PostAsJsonAsync(Path+"/limpar",request with {Confirmacao=""}))Assert.Equal(HttpStatusCode.BadRequest,no.StatusCode);
        using(var no=await c.PostAsJsonAsync(Path+"/limpar",request with {OperacaoId=Guid.Empty}))Assert.Equal(HttpStatusCode.BadRequest,no.StatusCode);
        Assert.Equal(1,await Col("customers").CountDocumentsAsync(new BsonDocument()));
        using(var cols=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")}))
        {
            var validator=(await cols.SingleAsync())["options"]["validator"].AsBsonDocument;
            try
            {
                await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("acao",new BsonDocument("$ne","DemoLimpa"))})}});
                using var failed=await c.PostAsJsonAsync(Path+"/limpar",request);
                Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);
            }
            finally {await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}});}
        }
        Assert.Equal(1,await Col("customers").CountDocumentsAsync(new BsonDocument()));
        Assert.NotNull(await f.Users.Find(x=>x.Id==other.Id).SingleOrDefaultAsync());
        using(var cleared=await c.PostAsJsonAsync(Path+"/limpar",request))Assert.Equal(HttpStatusCode.NoContent,cleared.StatusCode);
        Assert.Equal(admin.Id,(await f.Users.Find(new BsonDocument()).SingleAsync()).Id);
        Assert.Equal(employee.Id,(await Col("employees").Find(new BsonDocument()).SingleAsync())["_id"].AsGuid);
        Assert.Equal(1,await Col("api_sessions").CountDocumentsAsync(new BsonDocument()));
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await otherClient.GetAsync("/api/v1/auth/me")).StatusCode);
        foreach(var name in new[]{"customers","products","categories","brands","sales","stock_movements","idempotency_receipts"})Assert.Equal(0,await Col(name).CountDocumentsAsync(new BsonDocument()));
        Assert.Null((await c.GetFromJsonAsync<EstadoCargaResponse>(Path))!.UltimaExecucao);
        Assert.Equal(1,await Col("audit_events").CountDocumentsAsync(new BsonDocument("acao","DemoLimpa")));
        using(var generated=await c.PostAsync(Path+"/gerar",null))Assert.Equal(HttpStatusCode.OK,generated.StatusCode);
        using(var retry=await c.PostAsJsonAsync(Path+"/limpar",request))Assert.Equal(HttpStatusCode.NoContent,retry.StatusCode);
        // A lost response retried after new data must never clear that new generation.
        Assert.Equal(250,await Col("sales").CountDocumentsAsync(new BsonDocument()));
        Assert.Equal(100,await Col("products").CountDocumentsAsync(new BsonDocument()));
        Assert.NotNull((await c.GetFromJsonAsync<EstadoCargaResponse>(Path))!.UltimaExecucao);
        using(var second=await c.PostAsJsonAsync(Path+"/limpar",request with {OperacaoId=Guid.NewGuid()}))Assert.Equal(HttpStatusCode.NoContent,second.StatusCode);
        Assert.Equal(0,await Col("sales").CountDocumentsAsync(new BsonDocument()));
        Assert.Equal(2,await Col("audit_events").CountDocumentsAsync(new BsonDocument("acao","DemoLimpa")));
        using(var old=await c.PostAsJsonAsync(Path+"/limpar",request))Assert.Equal(HttpStatusCode.NoContent,old.StatusCode);
    });
    [Theory][InlineData("Vendedor")][InlineData("Estoquista")]
    public Task OnlyAdministratorMayClear(string role)=>fixture.Auth.Mongo.SafeAsync(async()=>
    {
        var f=fixture.Auth;using var c=f.Client((await f.LoginAsync(await f.SeedAsync(role))).AccessToken);using var anonymous=f.Client();
        var request=new LimparDadosRequest(Guid.NewGuid(),"APAGAR DADOS");
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Path+"/limpar",request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync(Path+"/limpar",request)).StatusCode);
    });
}
