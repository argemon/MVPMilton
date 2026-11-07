using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Vendas;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class SalesFixture : IAsyncLifetime
{
    public AuthFixture Auth { get; }=new();
    public async Task InitializeAsync(){await Auth.InitializeAsync();await Auth.Api.Services.GetRequiredService<EstoqueSchema>().InitializeAsync(default);await Auth.Api.Services.GetRequiredService<VendasSchema>().InitializeAsync(default);}
    public Task DisposeAsync()=>Auth.DisposeAsync();
}
public sealed class SalesTests(SalesFixture fixture) : IClassFixture<SalesFixture>
{
    private AuthFixture F=>fixture.Auth;
    private IMongoDatabase Db=>F.Mongo.Client.GetDatabase(F.Mongo.Name);
    private async Task<HttpClient> User(string role="Administrador")=>F.Client((await F.LoginAsync(await F.SeedAsync(role))).AccessToken);
    private static async Task<ProdutoResponse> Product(HttpClient c)=>await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));
    private static async Task<Guid> Customer(HttpClient c){using var r=await c.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente Venda","PF",null,null,null));Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;}
    internal static async Task<HttpResponseMessage> Command(HttpClient c,string path,object body,Guid? key=null)
    {using var request=new HttpRequestMessage(HttpMethod.Post,path);request.Headers.Add("Idempotency-Key",(key??Guid.NewGuid()).ToString());request.Content=JsonContent.Create(body);return await c.SendAsync(request);}
    private static async Task<VendaResponse> Sale(HttpResponseMessage r,HttpStatusCode expected=HttpStatusCode.OK){using(r){Assert.Equal(expected,r.StatusCode);return(await r.Content.ReadFromJsonAsync<VendaResponse>())!;}}
    private static Task<VendaResponse> Draft(HttpClient c,Guid customer,params ItemVendaRequest[] items)=>Create(c,customer,Guid.NewGuid(),items);
    private static async Task<VendaResponse> Create(HttpClient c,Guid customer,Guid key,params ItemVendaRequest[] items)=>await Sale(await Command(c,"/api/v1/vendas",new CriarVendaRequest(customer,items),key),HttpStatusCode.Created);
    private static async Task<int> Balance(HttpClient c,Guid id)=>(await c.GetFromJsonAsync<ProdutoResponse>($"/api/v1/produtos/{id}"))!.Saldo;
    private static async Task Enter(HttpClient c,Guid product,int quantity=10){using var r=await StockTests.Move(c,product,"entradas",quantity);Assert.Equal(HttpStatusCode.Created,r.StatusCode);}
    [Fact]public Task FullLifecycleUsesExactServerPricesSnapshotsAndOneRestock()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);await Enter(c,p.Id);
        var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,2));Assert.Equal(25m,sale.Total);Assert.Equal(12.50m,sale.Itens.Single().PrecoUnitario);Assert.Equal(10,await Balance(c,p.Id));
        var key=Guid.NewGuid();var confirmed=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key));Assert.Equal("Confirmada",confirmed.Status);Assert.Equal(8,await Balance(c,p.Id));Assert.NotNull(confirmed.ClienteSnapshot);Assert.NotNull(confirmed.Confirmacao);
        var replay=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key));Assert.Equal(confirmed.Id,replay.Id);Assert.Equal(confirmed.Versao,replay.Versao);Assert.Equal(8,await Balance(c,p.Id));
        Assert.Equal(HttpStatusCode.MethodNotAllowed,(await c.DeleteAsync($"/api/v1/vendas/{sale.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/vendas/{sale.Id}",new AtualizarVendaRequest(customer,[],confirmed.Versao))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(confirmed.Versao))).StatusCode);
        using var rename=await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,false)with{Nome="Nome atual",Preco=22m});Assert.Equal(HttpStatusCode.OK,rename.StatusCode);
        var cancelKey=Guid.NewGuid();var cancelBody=new EncerrarVendaRequest(confirmed.Versao,"Cancelamento sintético");var cancelled=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/cancelamento",cancelBody,cancelKey));Assert.Equal("Cancelada",cancelled.Status);Assert.Equal(10,await Balance(c,p.Id));Assert.Equal(p.Nome,cancelled.Itens.Single().NomeSnapshot);Assert.Equal(25m,cancelled.Total);
        await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/cancelamento",cancelBody,cancelKey));Assert.Equal(10,await Balance(c,p.Id));Assert.Equal(HttpStatusCode.Conflict,(await Command(c,$"/api/v1/vendas/{sale.Id}/cancelamento",cancelBody with{VersaoEsperada=cancelled.Versao})).StatusCode);
        var oldReceipt=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key));Assert.Equal("Confirmada",oldReceipt.Status);Assert.Equal("Cancelada",(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{sale.Id}"))!.Status);
        var history=(await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}"))!;Assert.Equal(10,history.Items.Sum(x=>x.Delta));Assert.Single(history.Items,x=>x.Tipo=="Venda");Assert.Single(history.Items,x=>x.Tipo=="EstornoVenda");Assert.All(history.Items.Where(x=>x.VendaId is not null),x=>Assert.Equal(sale.Id,x.VendaId));
        var clientHistory=(await c.GetFromJsonAsync<HistoricoClienteResponse>($"/api/v1/clientes/{customer}/vendas"))!;Assert.Contains(clientHistory.Items,x=>x.Id==sale.Id);
    });
    [Fact]public Task DraftEditingRefreshesPricesPreservesItemIdsAndDiscardNeverMovesStock()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,2));var itemId=sale.Itens.Single().Id;
        using var changed=await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p)with{Preco=15m});Assert.Equal(HttpStatusCode.OK,changed.StatusCode);
        using var conflict=await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));Assert.Equal(HttpStatusCode.Conflict,conflict.StatusCode);Assert.Contains("PRECO_ALTERADO",await conflict.Content.ReadAsStringAsync());
        var edited=await Sale(await c.PutAsJsonAsync($"/api/v1/vendas/{sale.Id}",new AtualizarVendaRequest(customer,[new(p.Id,3)],sale.Versao)));Assert.Equal(itemId,edited.Itens.Single().Id);Assert.Equal(45m,edited.Total);Assert.Equal(0,await Balance(c,p.Id));
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/vendas/{sale.Id}",new AtualizarVendaRequest(customer,[],sale.Versao))).StatusCode);
        var discarded=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/descarte",new EncerrarVendaRequest(edited.Versao,"Descarte sintético")));Assert.Equal("Descartada",discarded.Status);Assert.NotNull(discarded.Descarte);Assert.Equal(0,await Balance(c,p.Id));
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/vendas/{sale.Id}",new AtualizarVendaRequest(customer,[],discarded.Versao))).StatusCode);
        var empty=await Draft(c,customer);Assert.Empty(empty.Itens);Assert.Equal(0m,empty.Total);Assert.Equal(HttpStatusCode.Conflict,(await Command(c,$"/api/v1/vendas/{empty.Id}/confirmacao",new ConfirmarVendaRequest(empty.Versao))).StatusCode);
    });
    [Fact]public Task MultipleItemsInsufficientOrAuditFailureRollBackAllDocuments()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var a=await Product(c);var b=await Product(c);await Enter(c,a.Id,5);
        var sale=await Draft(c,customer,new ItemVendaRequest(a.Id,2),new ItemVendaRequest(b.Id,1));Assert.Equal(37.50m,sale.Total);var key=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Conflict,(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key)).StatusCode);Assert.Equal(5,await Balance(c,a.Id));Assert.Equal(0,await Balance(c,b.Id));Assert.Equal("Rascunho",(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{sale.Id}"))!.Status);
        Assert.Equal(0,await Db.GetCollection<BsonDocument>("stock_movements").CountDocumentsAsync(new BsonDocument("vendaId",new BsonBinaryData(sale.Id,GuidRepresentation.Standard))));
        await Enter(c,b.Id,5);using var cols=await Db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")});var validator=(await cols.SingleAsync())["options"]["validator"].AsBsonDocument;
        try{await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("acao",new BsonDocument("$ne","Confirmada"))})}});
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key)).StatusCode);Assert.Equal(5,await Balance(c,a.Id));Assert.Equal(5,await Balance(c,b.Id));
            Assert.Equal(0,await Db.GetCollection<BsonDocument>("stock_movements").CountDocumentsAsync(new BsonDocument("vendaId",new BsonBinaryData(sale.Id,GuidRepresentation.Standard))));Assert.Equal(0,await Db.GetCollection<BsonDocument>("idempotency_receipts").CountDocumentsAsync(new BsonDocument("chave",new BsonBinaryData(key,GuidRepresentation.Standard))));
        }finally{await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}});}
        var done=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key));Assert.Equal("Confirmada",done.Status);Assert.Equal(3,await Balance(c,a.Id));Assert.Equal(4,await Balance(c,b.Id));
    });
    [Fact]public Task TwoSalesCompeteForLastUnitWithIndependentSessions()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);await Enter(c,p.Id,1);var a=await Draft(c,customer,new ItemVendaRequest(p.Id,1));var b=await Draft(c,customer,new ItemVendaRequest(p.Id,1));
        var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Confirm(VendaResponse sale){await gate.Task;using var r=await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));return r.StatusCode;}
        var x=Confirm(a);var y=Confirm(b);gate.SetResult();var outcomes=await Task.WhenAll(x,y);Assert.Single(outcomes,z=>z==HttpStatusCode.OK);Assert.Single(outcomes,z=>z==HttpStatusCode.Conflict);Assert.Equal(0,await Balance(c,p.Id));
        Assert.Single((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&tipo=Venda"))!.Items);
    });
    [Fact]public Task CreateAndConfirmIdempotencyAreConcurrentAndBoundToRequest()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);await Enter(c,p.Id);var key=Guid.NewGuid();var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<VendaResponse> Add(){await gate.Task;return await Create(c,customer,key,new ItemVendaRequest(p.Id,2));}var a=Add();var b=Add();gate.SetResult();var drafts=await Task.WhenAll(a,b);Assert.Equal(drafts[0].Id,drafts[1].Id);
        Assert.Equal(HttpStatusCode.Conflict,(await Command(c,"/api/v1/vendas",new CriarVendaRequest(customer,[new(p.Id,3)]),key)).StatusCode);
        var sale=drafts[0];key=Guid.NewGuid();var confirmGate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<VendaResponse> Confirm(){await confirmGate.Task;return await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao),key));}var x=Confirm();var y=Confirm();confirmGate.SetResult();var confirmed=await Task.WhenAll(x,y);Assert.Equal(confirmed[0].Versao,confirmed[1].Versao);Assert.Equal(8,await Balance(c,p.Id));
        Assert.Equal(HttpStatusCode.Conflict,(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao+1),key)).StatusCode);
    });
    [Fact]public Task InvalidInactiveAndMissingReferencesNeverProduceBusinessEffects()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);
        foreach(var body in new object[]{new CriarVendaRequest(Guid.Empty,[]),new CriarVendaRequest(customer,null),new CriarVendaRequest(customer,[new(p.Id,0)]),new CriarVendaRequest(customer,[new(p.Id,1),new(p.Id,2)]),new{clienteId=customer,itens=new[]{new{produtoId=p.Id,quantidade=1,precoUnitario=0.01m}}}})Assert.Equal(HttpStatusCode.BadRequest,(await Command(c,"/api/v1/vendas",body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Command(c,"/api/v1/vendas",new CriarVendaRequest(Guid.NewGuid(),[]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Command(c,"/api/v1/vendas",new CriarVendaRequest(customer,[new(Guid.NewGuid(),1)]))).StatusCode);
        var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,1));await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,false));
        Assert.Equal(HttpStatusCode.Conflict,(await Command(c,"/api/v1/vendas",new CriarVendaRequest(customer,[new(p.Id,1)]))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/vendas/{sale.Id}",new AtualizarVendaRequest(customer,[new(p.Id,1)],sale.Versao))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao))).StatusCode);
        var removed=await Sale(await c.PutAsJsonAsync($"/api/v1/vendas/{sale.Id}",new AtualizarVendaRequest(customer,[],sale.Versao)));Assert.Empty(removed.Itens);
        var client=(await c.GetFromJsonAsync<ClienteResponse>($"/api/v1/clientes/{customer}"))!;await c.PutAsJsonAsync($"/api/v1/clientes/{customer}",new AtualizarClienteRequest(client.Nome,client.Tipo,client.Documento,client.Email,client.Telefone,false,client.Versao));Assert.Equal(HttpStatusCode.Conflict,(await Command(c,"/api/v1/vendas",new CriarVendaRequest(customer,[]))).StatusCode);
    });
    [Fact]public Task SellerOwnershipAppliesBeforePaginationCountsAndClientHistory()=>F.Mongo.SafeAsync(async()=>
    {
        using var admin=await User();var customer=await Customer(admin);var p=await Product(admin);using var a=await User("Vendedor");using var b=await User("Vendedor");var own=await Draft(a,customer,new ItemVendaRequest(p.Id,1));var other=await Draft(b,customer,new ItemVendaRequest(p.Id,1));
        var list=(await a.GetFromJsonAsync<VendasResponse>($"/api/v1/vendas?clienteId={customer}&pageSize=1"))!;Assert.Equal(1,list.TotalItems);Assert.Equal(own.Id,list.Items.Single().Id);var history=(await a.GetFromJsonAsync<HistoricoClienteResponse>($"/api/v1/clientes/{customer}/vendas"))!;Assert.Equal(1,history.TotalItems);Assert.Equal(own.Id,history.Items.Single().Id);
        Assert.Equal(HttpStatusCode.NotFound,(await a.GetAsync($"/api/v1/vendas/{other.Id}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await a.PutAsJsonAsync($"/api/v1/vendas/{other.Id}",new AtualizarVendaRequest(customer,[],other.Versao))).StatusCode);
        foreach(var action in new[]{"confirmacao","descarte"})Assert.Equal(HttpStatusCode.NotFound,(await Command(a,$"/api/v1/vendas/{other.Id}/{action}",action=="confirmacao"?(object)new ConfirmarVendaRequest(other.Versao):new EncerrarVendaRequest(other.Versao,"Descarte sintético"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await Command(a,$"/api/v1/vendas/{own.Id}/cancelamento",new EncerrarVendaRequest(own.Versao,"Cancelamento sintético"))).StatusCode);
        Assert.Equal(2,(await admin.GetFromJsonAsync<VendasResponse>($"/api/v1/vendas?clienteId={customer}"))!.TotalItems);
        using var stock=await User("Estoquista");using var anon=F.Client();foreach(var c in new[]{stock,anon})
        {
            var denied=c==stock?HttpStatusCode.Forbidden:HttpStatusCode.Unauthorized;
            foreach(var path in new[]{"/api/v1/vendas",$"/api/v1/vendas/{own.Id}"})Assert.Equal(denied,(await c.GetAsync(path)).StatusCode);
            Assert.Equal(denied,(await Command(c,"/api/v1/vendas",new CriarVendaRequest(customer,[]))).StatusCode);Assert.Equal(denied,(await c.PutAsJsonAsync($"/api/v1/vendas/{own.Id}",new AtualizarVendaRequest(customer,[],own.Versao))).StatusCode);
            foreach(var action in new[]{"confirmacao","cancelamento","descarte"})Assert.Equal(denied,(await Command(c,$"/api/v1/vendas/{own.Id}/{action}",action=="confirmacao"?(object)new ConfirmarVendaRequest(own.Versao):new EncerrarVendaRequest(own.Versao,"Motivo sintético"))).StatusCode);
        }
    });
    [Fact]public Task CancellationOverflowAndAuditFailurePreserveConfirmedSaleUntilSuccessfulRetry()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);await Enter(c,p.Id);var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,2));sale=await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao)));
        var raw=Db.GetCollection<BsonDocument>("products");var filter=new BsonDocument("_id",new BsonBinaryData(p.Id,GuidRepresentation.Standard));var original=(await raw.Find(filter).SingleAsync())["saldo"].DeepClone();var key=Guid.NewGuid();var body=new EncerrarVendaRequest(sale.Versao,"Cancelamento sintético");
        try{await raw.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("saldo.quantidade",int.MaxValue)));using var r=await Command(c,$"/api/v1/vendas/{sale.Id}/cancelamento",body,key);Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);Assert.Contains("SALDO_EXCEDE_LIMITE",await r.Content.ReadAsStringAsync());Assert.Equal("Confirmada",(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{sale.Id}"))!.Status);}
        finally{await raw.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("saldo",original)));}
        var client=(await c.GetFromJsonAsync<ClienteResponse>($"/api/v1/clientes/{customer}"))!;await c.PutAsJsonAsync($"/api/v1/clientes/{customer}",new AtualizarClienteRequest(client.Nome,client.Tipo,client.Documento,client.Email,client.Telefone,false,client.Versao));
        using var cols=await Db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")});var validator=(await cols.SingleAsync())["options"]["validator"].AsBsonDocument;
        try{await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("acao",new BsonDocument("$ne","Cancelada"))})}});Assert.Equal(HttpStatusCode.ServiceUnavailable,(await Command(c,$"/api/v1/vendas/{sale.Id}/cancelamento",body,key)).StatusCode);Assert.Equal(8,await Balance(c,p.Id));Assert.Equal("Confirmada",(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{sale.Id}"))!.Status);}
        finally{await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}});}
        await Sale(await Command(c,$"/api/v1/vendas/{sale.Id}/cancelamento",body,key));Assert.Equal(10,await Balance(c,p.Id));Assert.Single((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&tipo=EstornoVenda"))!.Items);
    });
    [Fact]public Task MissingBalanceCannotBeRecreatedBySaleConfirmation()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,1));var products=Db.GetCollection<BsonDocument>("products");var filter=new BsonDocument("_id",new BsonBinaryData(p.Id,GuidRepresentation.Standard));var original=(await products.Find(filter).SingleAsync())["saldo"].DeepClone();
        try{await products.UpdateOneAsync(filter,new BsonDocument("$unset",new BsonDocument("saldo",1)),new UpdateOptions{BypassDocumentValidation=true});using var r=await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);Assert.Contains("SALDO_INEXISTENTE",await r.Content.ReadAsStringAsync());Assert.False((await products.Find(filter).SingleAsync()).Contains("saldo"));Assert.Equal("Rascunho",(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{sale.Id}"))!.Status);}
        finally{await products.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("saldo",original)));}
    });
    [Theory][InlineData("preco")][InlineData("produto")][InlineData("cliente")]
    public Task ReferenceChangesAndConfirmationSerializeWithRealGuards(string change)=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);await Enter(c,p.Id,1);var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,1));var client=(await c.GetFromJsonAsync<ClienteResponse>($"/api/v1/clientes/{customer}"))!;var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Confirm(){await gate.Task;using var r=await Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));return r.StatusCode;}
        async Task<HttpStatusCode> Change(){await gate.Task;using var r=change=="cliente"?await c.PutAsJsonAsync($"/api/v1/clientes/{customer}",new AtualizarClienteRequest(client.Nome,client.Tipo,client.Documento,client.Email,client.Telefone,false,client.Versao)):await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,change!="produto")with{Preco=change=="preco"?15m:p.Preco});return r.StatusCode;}
        var a=Confirm();var b=Change();gate.SetResult();var outcomes=await Task.WhenAll(a,b);Assert.Equal(HttpStatusCode.OK,outcomes[1]);Assert.Contains(outcomes[0],new[]{HttpStatusCode.OK,HttpStatusCode.Conflict});var current=(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{sale.Id}"))!;Assert.Equal(outcomes[0]==HttpStatusCode.OK?"Confirmada":"Rascunho",current.Status);Assert.Equal(outcomes[0]==HttpStatusCode.OK?0:1,await Balance(c,p.Id));Assert.Equal(12.50m,current.Itens.Single().PrecoUnitario);
    });
    [Theory][InlineData("unknown")][InlineData("transient")]
    public Task ConfirmationControlFaultsUseSameStockTransactionAndReceipt(string mode)=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);await Enter(c,p.Id);var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,2));using var scope=F.Api.Services.CreateScope();var services=scope.ServiceProvider;var actor=(await services.GetRequiredService<GestaoComercial.Application.Acesso.IAutenticacao>().ValidarAsync(c.DefaultRequestHeaders.Authorization!.Parameter!,false,default))!;
        var fault=new CommitFault{Mode=mode};var client=System.Reflection.DispatchProxy.Create<IMongoClient,ClientFaultProxy>();var proxy=(ClientFaultProxy)(object)client;proxy.Target=F.Mongo.Client;proxy.Fault=fault;
        var service=new MongoVendas(services.GetRequiredService<GestaoComercial.Infrastructure.Persistence.OperacoesCadastro>(),client,services.GetRequiredService<MongoEstoque>(),services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),services.GetRequiredService<VendasSchema>());var key=Guid.NewGuid();var done=await service.TransicionarAsync(actor,sale.Id,key,"confirmacao",sale.Versao,null,default);Assert.Equal("Confirmada",done.Status);Assert.Equal(mode=="transient"?2:1,fault.Starts);Assert.Equal(2,fault.Commits);Assert.Equal(mode=="transient"?1:0,fault.Aborts);
        await service.TransicionarAsync(actor,sale.Id,key,"confirmacao",sale.Versao,null,default);Assert.Equal(8,await Balance(c,p.Id));Assert.Single((await c.GetFromJsonAsync<MovimentosResponse>($"/api/v1/estoque/movimentos?produtoId={p.Id}&tipo=Venda"))!.Items);
    });
    [Fact]public Task FiltersDatesPaginationAndServerValidatorsRejectInvalidHistory()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=await User();var customer=await Customer(c);var p=await Product(c);var sale=await Draft(c,customer,new ItemVendaRequest(p.Id,2));await Draft(c,customer);
        var page=(await c.GetFromJsonAsync<VendasResponse>($"/api/v1/vendas?clienteId={customer}&status=Rascunho&pageSize=1&inicio=2026-01-01&fim=2027-01-01"))!;Assert.Equal(2,page.TotalItems);Assert.Single(page.Items);Assert.Empty((await c.GetFromJsonAsync<VendasResponse>($"/api/v1/vendas?clienteId={customer}&page=3&pageSize=1"))!.Items);
        foreach(var filter in new[]{"page=0","pageSize=101","status=invalido","inicio=2026-01-01","inicio=2026-01-01&fim=2028-01-01"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/vendas?"+filter)).StatusCode);
        var sales=Db.GetCollection<BsonDocument>("sales");var raw=await sales.Find(new BsonDocument("_id",new BsonBinaryData(sale.Id,GuidRepresentation.Standard))).SingleAsync();Assert.Equal(BsonType.Int64,raw["totalCentavos"].BsonType);Assert.Equal(2500,raw["totalCentavos"].AsInt64);
        async Task Invalid(BsonDocument d){d["_id"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard);Assert.Equal(121,(await Assert.ThrowsAsync<MongoWriteException>(()=>sales.InsertOneAsync(d))).WriteError.Code);}
        var dup=raw.DeepClone().AsBsonDocument;dup["itens"].AsBsonArray.Add(dup["itens"][0].DeepClone());dup["totalCentavos"]=5000L;await Invalid(dup);
        var total=raw.DeepClone().AsBsonDocument;total["totalCentavos"]=1L;await Invalid(total);var money=raw.DeepClone().AsBsonDocument;money["totalCentavos"]=25.0;await Invalid(money);var status=raw.DeepClone().AsBsonDocument;status["status"]="Confirmada";await Invalid(status);
    });
}
