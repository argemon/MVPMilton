using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

[Collection("CargaDemonstracao")]
public sealed class DemonstrationTests(SalesFixture fixture) : IClassFixture<SalesFixture>
{
    private AuthFixture F=>fixture.Auth;
    private const string Path="/api/v1/administracao/dados-demonstracao/";
    [Fact]
    public Task LoadRepeatRestoreReconcileReportsAndPreserveManualDependencies()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync("Administrador"))).AccessToken);c.Timeout=TimeSpan.FromMinutes(15);
        var db=F.Mongo.Client.GetDatabase(F.Mongo.Name);
        IMongoCollection<BsonDocument> Col(string n)=>db.GetCollection<BsonDocument>(n);
        var originalUsers=await Col("users").CountDocumentsAsync(new BsonDocument());
        using var manual=await c.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente manual preservado","PF",null,null,null));
        Assert.Equal(HttpStatusCode.Created,manual.StatusCode);var manualId=(await manual.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;
        async Task Generate(bool restore=false){using var response=restore?await c.PostAsJsonAsync(Path+"restaurar",new RestaurarCargaRequest(true)):await c.PostAsync(Path+"gerar",null);var text=await response.Content.ReadAsStringAsync();Assert.True(response.StatusCode==HttpStatusCode.OK,$"Carga: {response.StatusCode}: {text}");Assert.Equal(new CargaDemonstracaoResponse(12,12,40,100,250,25),await response.Content.ReadFromJsonAsync<CargaDemonstracaoResponse>());}
        async Task<(string,long)> Check()
        {
            foreach(var (name,count) in new[]{("employees",12),("customers",41),("products",100),("sales",250),("categories",5),("brands",5)})Assert.Equal(count,await Col(name).CountDocumentsAsync(new BsonDocument()));
            Assert.Equal(originalUsers+12,await Col("users").CountDocumentsAsync(new BsonDocument()));
            var products=await Col("products").Find(new BsonDocument()).Sort(new BsonDocument("sku",1)).ToListAsync();var moves=await Col("stock_movements").Find(new BsonDocument()).ToListAsync();
            Assert.Equal(850,moves.Count); // 300 manual movements, 500 sale lines, 50 restocks.
            foreach(var p in products)Assert.Equal(p["saldo"]["quantidade"].AsInt32,moves.Where(m=>m["produtoId"]==p["_id"]).Sum(m=>m["delta"].AsInt32));
            Assert.Equal(5,products.Count(p=>!p["ativo"].AsBoolean));Assert.Equal(3,await Col("employees").CountDocumentsAsync(new BsonDocument("ativo",false)));
            var state=(await c.GetFromJsonAsync<EstadoCargaResponse>(Path[..^1]))!;Assert.NotNull(state.UltimaExecucao);Assert.Equal(F.Mongo.Name,state.Banco);
            var sales=await Col("sales").Find(new BsonDocument()).ToListAsync();Assert.All(sales,s=>Assert.Equal(2,s["itens"].AsBsonArray.Count));Assert.Equal(35,sales.Select(s=>s["clienteId"]).Distinct().Count());Assert.Equal(25,sales.Count(s=>s["status"]=="Cancelada"));Assert.Equal(225,sales.Count(s=>s["status"]=="Confirmada"));Assert.Equal(50,moves.Count(m=>m["tipo"]=="EstornoVenda"));
            var query="inicio=2026-08-01&fim=2026-11-08";
            var panel=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?"+query))!;var report=(await c.GetFromJsonAsync<RelatorioVendasResponse>("/api/v1/relatorios/vendas?"+query))!;
            Assert.Equal(225,panel.Resumo.QuantidadeConfirmada);Assert.Equal(25,panel.Resumo.QuantidadeCancelada);Assert.Equal(panel.Resumo,report.Totais);Assert.Equal(250,report.TotalItems);
            Assert.Equal(sales.Where(s=>s["status"]=="Confirmada").Sum(s=>s["totalCentavos"].AsInt64)/100m,panel.Resumo.ReceitaLiquida);
            foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"}){using var r=await c.GetAsync($"/api/v1/relatorios/{kind}/exportacao?"+(kind=="estoque"?"":query));Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.True((await r.Content.ReadAsByteArrayAsync()).Length>100);}
            Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/clientes/"+manualId)).StatusCode);
            return(string.Join(";",products.Select(p=>p["sku"]+":"+p["precoCentavos"]+":"+p["saldo"]["quantidade"])),sales.Sum(s=>s["totalCentavos"].AsInt64));
        }
        using(var cols=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")}))
        {
            var validator=(await cols.SingleAsync())["options"]["validator"].AsBsonDocument;
            try{await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("acao",new BsonDocument("$ne","ProdutoCriado"))})}});using var failed=await c.PostAsync(Path+"gerar",null);Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);Assert.Equal(12,await Col("employees").CountDocumentsAsync(new BsonDocument()));Assert.Equal(0,await Col("products").CountDocumentsAsync(new BsonDocument()));}
            finally{await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}});}
        }
        var loading=Generate();
        using(var barrier=new CancellationTokenSource(TimeSpan.FromSeconds(30))){while(!await Col("stock_movements").Find(new BsonDocument()).AnyAsync(barrier.Token))await Task.Delay(25,barrier.Token);}
        using(var concurrent=await c.PostAsync(Path+"gerar",null))Assert.Equal(HttpStatusCode.Conflict,concurrent.StatusCode);
        await loading;var first=await Check();
        var arroz=await Col("products").Find(new BsonDocument("sku","TCC-DEMO-001")).SingleAsync();
        Assert.Equal("Arroz branco Camil 500 g",arroz["nome"].AsString);
        var historico=await Col("sales").Find(new BsonDocument()).ToListAsync();
        // Simulate a persisted legacy label, without changing the historical snapshots.
        var precoLegado=new Random(902026).Next(500,50001);
        await Col("products").UpdateOneAsync(new BsonDocument("_id",arroz["_id"]),new BsonDocument("$set",new BsonDocument{{"nome","Produto fictício 1"},{"precoCentavos",(long)precoLegado}}));
        await Generate();
        var atualizado=await Col("products").Find(new BsonDocument("_id",arroz["_id"])).SingleAsync();
        Assert.Equal("Arroz branco Camil 500 g",atualizado["nome"].AsString);
        Assert.Equal(arroz["saldo"],atualizado["saldo"]);
        Assert.Equal(arroz["precoCentavos"],atualizado["precoCentavos"]);
        foreach(var venda in historico)Assert.Equal(venda,await Col("sales").Find(new BsonDocument("_id",venda["_id"])).SingleAsync());
        Assert.Equal(first,await Check());await Generate();Assert.Equal(first,await Check());
        using(var no=await c.PostAsJsonAsync(Path+"restaurar",new RestaurarCargaRequest(false)))Assert.Equal(HttpStatusCode.BadRequest,no.StatusCode);
        using(var cols=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")})){var validator=(await cols.SingleAsync())["options"]["validator"].AsBsonDocument;try{await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("acao",new BsonDocument("$ne","DemoRestaurada"))})}});using var failed=await c.PostAsJsonAsync(Path+"restaurar",new RestaurarCargaRequest(true));Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);}finally{await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}});}Assert.Equal(first,await Check());}
        await Generate(true);Assert.Equal(first,await Check());
        var product=(await Col("products").Find(new BsonDocument("ativo",true)).FirstAsync())["_id"].AsGuid;
        using var sale=await SalesTests.Command(c,"/api/v1/vendas",new CriarVendaRequest(manualId,[new(product,1)]));Assert.Equal(HttpStatusCode.Created,sale.StatusCode);
        using var refusal=await c.PostAsJsonAsync(Path+"restaurar",new RestaurarCargaRequest(true));Assert.Equal(HttpStatusCode.Conflict,refusal.StatusCode);Assert.Contains("CARGA_DEPENDENCIA_MANUAL",await refusal.Content.ReadAsStringAsync());
        Assert.Equal(251,await Col("sales").CountDocumentsAsync(new BsonDocument()));Assert.Equal(100,await Col("products").CountDocumentsAsync(new BsonDocument()));
    });
    [Theory][InlineData("Vendedor")][InlineData("Estoquista")]
    public Task DenyNonAdministratorsAndAnonymous(string role)=>F.Mongo.SafeAsync(async()=>
    {
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync(role))).AccessToken);using var anon=F.Client();
        foreach(var client in new[]{c,anon}){using var state=await client.GetAsync(Path[..^1]);Assert.Equal(client==c?HttpStatusCode.Forbidden:HttpStatusCode.Unauthorized,state.StatusCode);}
        foreach(var client in new[]{c,anon})foreach(var action in new[]{"gerar","restaurar"}){using var r=await client.PostAsJsonAsync(Path+action,new RestaurarCargaRequest(true));Assert.Equal(client==c?HttpStatusCode.Forbidden:HttpStatusCode.Unauthorized,r.StatusCode);}
    });
}
