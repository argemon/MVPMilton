using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Estoque;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class StockFixture : IAsyncLifetime
{
    public AuthFixture Auth { get; }=new();
    public async Task InitializeAsync() { await Auth.InitializeAsync();await Auth.Api.Services.GetRequiredService<EstoqueSchema>().InitializeAsync(default); }
    public Task DisposeAsync()=>Auth.DisposeAsync();
}
public sealed class StockTests(StockFixture stock) : IClassFixture<StockFixture>
{
    private AuthFixture F=>stock.Auth;
    private IMongoDatabase Db=>F.Mongo.Client.GetDatabase(F.Mongo.Name);
    private async Task<HttpClient> User(string role="Administrador")=>F.Client((await F.LoginAsync(await F.SeedAsync(role))).AccessToken);
    private static async Task<ProdutoResponse> Product(HttpClient c)=>await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));
    internal static async Task<HttpResponseMessage> Move(HttpClient c,Guid product,string path,int quantity,Guid? key=null,string reason="Movimentação sintética")
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/v1/estoque/"+path);
        request.Headers.Add("Idempotency-Key",(key??Guid.NewGuid()).ToString());
        request.Content=path=="ajustes"?JsonContent.Create(new AjusteRequest(product,quantity,reason)):JsonContent.Create(new MovimentoRequest(product,quantity,reason));
        return await c.SendAsync(request);
    }
    private static async Task<MovimentoResponse> Success(HttpResponseMessage r) { using(r){Assert.Equal(HttpStatusCode.Created,r.StatusCode);Assert.NotNull(r.Headers.Location);return(await r.Content.ReadFromJsonAsync<MovimentoResponse>())!;} }
    private static async Task<int> Balance(HttpClient c,Guid id)=>(await c.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{id}"))!.Saldo;
    [Fact] public Task EntriesExitsAdjustmentsReconcileAndHistoryIsImmutable()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var p=await Product(c);
        var a=await Success(await Move(c,p.Id,"entradas",10));var b=await Success(await Move(c,p.Id,"saidas",2));var d=await Success(await Move(c,p.Id,"ajustes",-1));
        Assert.Equal(7,await Balance(c,p.Id));Assert.Equal((0,10),(a.SaldoAntes,a.SaldoDepois));Assert.Equal((10,8),(b.SaldoAntes,b.SaldoDepois));Assert.Equal(-1,d.Delta);
        var history=(await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}"))!;
        Assert.Equal(3,history.TotalItems);Assert.Equal(7,history.Items.Sum(x=>x.Delta));
        Assert.Equal(a,await c.GetFromJsonAsync<MovimentoResponse>($"/api/v1/estoque/movimentos/{a.Id}"));
        Assert.Equal(HttpStatusCode.MethodNotAllowed,(await c.DeleteAsync($"/api/v1/estoque/movimentos/{a.Id}")).StatusCode);
        var zero=await Success(await Move(c,p.Id,"ajustes",-7));Assert.Equal(0,zero.SaldoDepois);
        Assert.Equal(4,await Db.GetCollection<BsonDocument>("audit_events").CountDocumentsAsync(new BsonDocument{{"recursoTipo","MovimentoEstoque"},{"recursoId",new BsonDocument("$in",new BsonArray(new[]{a.Id,b.Id,d.Id,zero.Id}.Select(x=>new BsonBinaryData(x,GuidRepresentation.Standard))))}}));
    });
    [Fact] public Task InvalidInsufficientInactiveAndOverflowHaveNoEffects()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var p=await Product(c);
        foreach(var (path,qty,reason) in new[]{("entradas",0,"Motivo completo"),("saidas",-1,"Motivo completo"),("ajustes",0,"Motivo completo"),("entradas",1000001,"Motivo completo"),("entradas",1,"curto")})
            Assert.Equal(HttpStatusCode.BadRequest,(await Move(c,p.Id,path,qty,reason:reason)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/v1/estoque/entradas",new MovimentoRequest(p.Id,1,"Motivo completo"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Move(c,p.Id,"saidas",1)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Move(c,p.Id,"ajustes",-1)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Move(c,Guid.NewGuid(),"entradas",1)).StatusCode);
        await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,false));
        Assert.Equal(HttpStatusCode.Conflict,(await Move(c,p.Id,"entradas",1)).StatusCode);Assert.Equal(0,await Balance(c,p.Id));
        Assert.Empty((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}"))!.Items);
        // Privileged corruption/limit fixtures are confined to this disposable test database.
        var raw=Db.GetCollection<BsonDocument>("products");var filter=new BsonDocument("_id",new BsonBinaryData(p.Id,GuidRepresentation.Standard));
        await raw.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument{{"ativo",true},{"saldo.quantidade",int.MaxValue}}));
        using var overflow=await Move(c,p.Id,"entradas",1);Assert.Equal(HttpStatusCode.Conflict,overflow.StatusCode);Assert.Contains("SALDO_EXCEDE_LIMITE",await overflow.Content.ReadAsStringAsync());Assert.Equal(int.MaxValue,await Balance(c,p.Id));
        await raw.UpdateOneAsync(filter,new BsonDocument("$unset",new BsonDocument("saldo",1)),new UpdateOptions{BypassDocumentValidation=true});
        using var absent=await Move(c,p.Id,"saidas",1);Assert.Equal(HttpStatusCode.Conflict,absent.StatusCode);Assert.Contains("SALDO_INEXISTENTE",await absent.Content.ReadAsStringAsync());Assert.False((await raw.Find(filter).SingleAsync()).Contains("saldo"));
        // Restore the privileged corruption fixture so later list tests do not depend on execution order.
        await raw.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument{{"ativo",false},{"saldo",new BsonDocument{{"quantidade",0},{"versao",1L}}}}));
    });
    [Fact] public Task IdempotencyIsConcurrentBodyBoundAndActorIsolated()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var p=await Product(c);var key=Guid.NewGuid();var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<MovimentoResponse> Add(){await gate.Task;return await Success(await Move(c,p.Id,"entradas",3,key));}
        var a=Add();var b=Add();gate.SetResult();var results=await Task.WhenAll(a,b);Assert.Equal(results[0],results[1]);
        Assert.Equal(results[0],await Success(await Move(c,p.Id,"entradas",3,key))); // Lost response recovered by same intention.
        Assert.Equal(HttpStatusCode.Conflict,(await Move(c,p.Id,"entradas",4,key)).StatusCode);
        using var other=await User();Assert.Equal(HttpStatusCode.Conflict,(await Move(other,p.Id,"entradas",3,key)).StatusCode);
        Assert.Equal(3,await Balance(c,p.Id));Assert.Single((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}"))!.Items);
    });
    [Fact] public Task TwoIndependentExitsCompeteForLastUnit()=>F.Mongo.SafeAsync(async()=>
    {
        using var a=await User();using var b=await User();var p=await Product(a);await Success(await Move(a,p.Id,"entradas",1));
        var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Exit(HttpClient c){await gate.Task;using var r=await Move(c,p.Id,"saidas",1);return r.StatusCode;}
        var x=Exit(a);var y=Exit(b);gate.SetResult();var outcomes=await Task.WhenAll(x,y);Assert.Single(outcomes,z=>z==HttpStatusCode.Created);Assert.Single(outcomes,z=>z==HttpStatusCode.Conflict);Assert.Equal(0,await Balance(a,p.Id));
        Assert.Single((await a.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&tipo=SaidaManual"))!.Items);
    });
    [Theory][InlineData("Administrador",true,true)][InlineData("Estoquista",true,false)][InlineData("Vendedor",false,false)]
    public Task EveryRouteEnforcesPolicies(string role,bool write,bool adjust)=>F.Mongo.SafeAsync(async()=>
    {
        using var setup=await User();var p=await Product(setup);var m=await Success(await Move(setup,p.Id,"entradas",3));using var c=await User(role);using var anon=F.Client();
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/estoque/saldos")).StatusCode);
        foreach(var path in new[]{"/api/v1/estoque/movimentos",$"/api/v1/estoque/movimentos/{m.Id}"}) { Assert.Equal(write?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await c.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(path)).StatusCode); }
        foreach(var path in new[]{"entradas","saidas"}) { Assert.Equal(write?HttpStatusCode.Created:HttpStatusCode.Forbidden,(await Move(c,p.Id,path,1)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await Move(anon,p.Id,path,1)).StatusCode); }
        Assert.Equal(adjust?HttpStatusCode.Created:HttpStatusCode.Forbidden,(await Move(c,p.Id,"ajustes",1)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await Move(anon,p.Id,"ajustes",1)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync("/api/v1/estoque/saldos")).StatusCode);
    });
    [Fact] public Task ConcurrentProductInactivationAndEntrySerializeWithoutResurrectingProduct()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var p=await Product(c);var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Entry(){await gate.Task;using var r=await Move(c,p.Id,"entradas",1);return r.StatusCode;}
        async Task<HttpStatusCode> Disable(){await gate.Task;using var r=await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,false));return r.StatusCode;}
        var entry=Entry();var disable=Disable();gate.SetResult();var results=await Task.WhenAll(entry,disable);
        Assert.Equal(HttpStatusCode.OK,results[1]);Assert.Contains(results[0],new[]{HttpStatusCode.Created,HttpStatusCode.Conflict});
        var current=(await c.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!;Assert.False(current.Ativo);Assert.Equal(results[0]==HttpStatusCode.Created?1:0,current.Saldo);
        Assert.Equal(HttpStatusCode.Conflict,(await Move(c,p.Id,"entradas",1)).StatusCode);
    });
    [Fact] public Task AuditFailureRollsBackBalanceMovementAndReceiptThenSameKeyWorks()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var p=await Product(c);var key=Guid.NewGuid();using var collections=await Db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")});var validator=(await collections.SingleAsync())["options"]["validator"].AsBsonDocument;
        try {
            await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("recursoTipo",new BsonDocument("$ne","MovimentoEstoque"))})}});
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await Move(c,p.Id,"entradas",5,key)).StatusCode);Assert.Equal(0,await Balance(c,p.Id));
            Assert.Equal(0,await Db.GetCollection<BsonDocument>("stock_movements").CountDocumentsAsync(new BsonDocument("produtoId",new BsonBinaryData(p.Id,GuidRepresentation.Standard))));Assert.Equal(0,await Db.GetCollection<BsonDocument>("idempotency_receipts").CountDocumentsAsync(new BsonDocument("chave",new BsonBinaryData(key,GuidRepresentation.Standard))));
        } finally { await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}}); }
        await Success(await Move(c,p.Id,"entradas",5,key));Assert.Equal(5,await Balance(c,p.Id));
    });
    [Fact] public Task FiltersPaginationAndServerValidatorsAreReal()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var p=await Product(c);var first=await Success(await Move(c,p.Id,"entradas",3));await Success(await Move(c,p.Id,"saidas",1));
        Assert.Equal(2,(await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&pageSize=1"))!.TotalItems);
        Assert.Empty((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&page=3&pageSize=1"))!.Items);
        Assert.Single((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&tipo=Entrada&inicio=2026-01-01&fim=2027-01-01"))!.Items);
        Assert.Equal(p.Id,(await c.GetFromJsonAsync<SaldosResponse>("/api/v1/estoque/saldos?search="+p.Sku))!.Items.Single().ProdutoId);
        Assert.Empty((await c.GetFromJsonAsync<SaldosResponse>("/api/v1/estoque/saldos?abaixoMinimo=true&search="+p.Sku))!.Items);
        foreach(var query in new[]{"page=0","pageSize=101","tipo=Invalido","inicio=2026-01-01","inicio=2026-01-01&fim=2028-01-01"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/estoque/movimentos?"+query)).StatusCode);
        var movements=Db.GetCollection<BsonDocument>("stock_movements");var raw=await movements.Find(new BsonDocument("_id",new BsonBinaryData(first.Id,GuidRepresentation.Standard))).SingleAsync();
        raw["_id"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard);raw["operacaoId"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard);raw["saldoDepois"]=99;
        var error=await Assert.ThrowsAsync<MongoWriteException>(()=>movements.InsertOneAsync(raw));Assert.Equal(121,error.WriteError.Code);
        using var names=await Db.ListCollectionNamesAsync();Assert.DoesNotContain("sales",await names.ToListAsync());
    });
}
