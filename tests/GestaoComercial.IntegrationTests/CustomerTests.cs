using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Clientes;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class CustomerTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    private async Task<HttpClient> User(string role="Vendedor") => f.Client((await f.LoginAsync(await f.SeedAsync(role))).AccessToken);
    private static CriarClienteRequest Data(string? doc=null) => new("Cliente Sintético "+Guid.NewGuid().ToString("N"),"PF",doc,"contato@example.test","65999990000");
    private static async Task<ClienteResponse> Create(HttpClient c,CriarClienteRequest? data=null)
    {
        using var r=await c.PostAsJsonAsync("/api/v1/clientes",data??Data()); Assert.Equal(HttpStatusCode.Created,r.StatusCode); Assert.NotNull(r.Headers.Location); return (await r.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }
    [Fact] public Task CrudFiltersEmailDocumentPaginationVersionAndHistory() => f.Mongo.SafeAsync(async()=>
    {
        using var c=await User(); var e=await Create(c,Data("12345678909")); Assert.True(e.Ativo); Assert.Equal(1,e.Versao);
        foreach(var q in new[]{e.Documento!,e.Email!,e.Nome[..30]}) Assert.Contains((await c.GetFromJsonAsync<ClientesResponse>($"/api/v1/clientes?search={Uri.EscapeDataString(q)}"))!.Items,x=>x.Id==e.Id);
        Assert.Empty((await c.GetFromJsonAsync<ClientesResponse>("/api/v1/clientes?tipo=PJ"))!.Items);
        Assert.Empty((await c.GetFromJsonAsync<ClientesResponse>("/api/v1/clientes?page=100&pageSize=1"))!.Items);
        var request=new AtualizarClienteRequest(e.Nome,"PF",e.Documento,null,null,false,e.Versao);
        using var changed=await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",request); Assert.Equal(HttpStatusCode.OK,changed.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",request)).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<ClientesResponse>("/api/v1/clientes?search=12345678909"))!.Items);
        Assert.Single((await c.GetFromJsonAsync<ClientesResponse>("/api/v1/clientes?search=12345678909&ativo=todos"))!.Items);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/v1/clientes",Data(e.Documento))).StatusCode);
        var row=(await changed.Content.ReadFromJsonAsync<ClienteResponse>())!;
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",request with{Tipo="PJ",Versao=row.Versao})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",request with{Ativo=true,Versao=row.Versao})).StatusCode);
        var h=(await c.GetFromJsonAsync<HistoricoClienteResponse>($"/api/v1/clientes/{e.Id}/vendas"))!; Assert.Empty(h.Items); Assert.Equal(0,h.TotalItems);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/clientes/{Guid.NewGuid()}/vendas")).StatusCode);
        using var names=await f.Mongo.Client.GetDatabase(f.Mongo.Name).ListCollectionNamesAsync(); Assert.DoesNotContain("sales",await names.ToListAsync());
    });
    [Theory] [InlineData("Estoquista",403)] [InlineData("Administrador",200)] [InlineData("Vendedor",200)]
    public Task AuthorizationForEveryVerb(string role,int expected) => f.Mongo.SafeAsync(async()=>
    {
        using var c=await User(role); using var allowed=await User(); var e=await Create(allowed);
        Assert.Equal(expected,(int)(await c.GetAsync("/api/v1/clientes")).StatusCode);
        Assert.Equal(expected,(int)(await c.GetAsync($"/api/v1/clientes/{e.Id}")).StatusCode);
        Assert.Equal(expected,(int)(await c.GetAsync($"/api/v1/clientes/{e.Id}/vendas")).StatusCode);
        Assert.Equal(expected==200?201:403,(int)(await c.PostAsJsonAsync("/api/v1/clientes",Data())).StatusCode);
        Assert.Equal(expected,(int)(await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",new AtualizarClienteRequest(e.Nome,"PF",null,null,null,true,e.Versao))).StatusCode);
        using var anon=f.Client();
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync("/api/v1/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync($"/api/v1/clientes/{e.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync($"/api/v1/clientes/{e.Id}/vendas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PostAsJsonAsync("/api/v1/clientes",Data())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",new{})).StatusCode);
    });
    [Fact] public Task RejectsMalformedFieldsFiltersAndHistoryPeriods() => f.Mongo.SafeAsync(async()=>
    {
        using var c=await User(); var e=await Create(c);
        foreach(var bad in new[]{Data() with{Nome=null},Data() with{Tipo="OUTRO"},Data() with{Documento=""},Data() with{Documento="11111111111"},Data() with{Email="bad"},Data() with{Telefone="1"}})
            Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/v1/clientes",bad)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/v1/clientes",new{nome="Pessoa",tipo="PF",saldo=10})).StatusCode);
        foreach(var query in new[]{"page=0","pageSize=101","ativo=bad","tipo=XX"}) Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/clientes?"+query)).StatusCode);
        foreach(var query in new[]{"inicio=2026-01-01","inicio=2026-01-01&fim=2026-01-01","inicio=2025-01-01&fim=2026-11-07","page=0"}) Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync($"/api/v1/clientes/{e.Id}/vendas?"+query)).StatusCode);
    });
    [Fact] public Task ConcurrentDocumentAndConcurrentEditsHaveOneWinner() => f.Mongo.SafeAsync(async()=>
    {
        using var c=await User(); var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> CreateOne() { await gate.Task; using var r=await c.PostAsJsonAsync("/api/v1/clientes",Data("98765432100")); return r.StatusCode; }
        var a=CreateOne(); var b=CreateOne(); gate.SetResult(); var results=await Task.WhenAll(a,b); Assert.Single(results,x=>x==HttpStatusCode.Created); Assert.Single(results,x=>x==HttpStatusCode.Conflict);
        var e=await Create(c); var editGate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Edit(string name) { await editGate.Task; using var r=await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",new AtualizarClienteRequest(name,"PF",null,null,null,true,e.Versao)); return r.StatusCode; }
        var x=Edit("Primeira edição"); var y=Edit("Segunda edição"); editGate.SetResult(); var edits=await Task.WhenAll(x,y); Assert.Single(edits,v=>v==HttpStatusCode.OK); Assert.Single(edits,v=>v==HttpStatusCode.Conflict);
    });
    [Fact] public Task UpgradeIdempotentValidatorRejectsBadDataAndAuditFailureRollsBack() => f.Mongo.SafeAsync(async()=>
    {
        using var c=await User(); var e=await Create(c); var schema=f.Api.Services.GetRequiredService<ClientesSchema>(); await schema.InitializeAsync(default); await schema.InitializeAsync(default); Assert.True(await schema.IsReadyAsync(default));
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name); var customers=db.GetCollection<BsonDocument>("customers"); var original=await customers.Find(new BsonDocument("_id",new BsonBinaryData(e.Id,GuidRepresentation.Standard))).SingleAsync();
        var invalid=original.DeepClone().AsBsonDocument; invalid["_id"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard); invalid["versao"]=1.0;
        var error=await Assert.ThrowsAsync<MongoWriteException>(()=>customers.InsertOneAsync(invalid)); Assert.Equal(121,error.WriteError.Code);
        using var collection=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")}); var validator=(await collection.SingleAsync())["options"]["validator"].AsBsonDocument;
        try {
            await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("recursoTipo",new BsonDocument("$ne","Cliente"))})}});
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PutAsJsonAsync($"/api/v1/clientes/{e.Id}",new AtualizarClienteRequest("Não salvar","PF",null,null,null,false,e.Versao))).StatusCode);
            Assert.Equal(original,await customers.Find(new BsonDocument("_id",original["_id"])).SingleAsync());
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PostAsJsonAsync("/api/v1/clientes",Data())).StatusCode);
        } finally { await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}}); }
    });
}
