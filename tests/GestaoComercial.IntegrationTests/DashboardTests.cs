using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Infrastructure.Persistence;
using GestaoComercial.Infrastructure.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class DashboardTests(SalesFixture fixture) : IClassFixture<SalesFixture>
{
    private AuthFixture F=>fixture.Auth;
    [Fact]public Task SnapshotSurvivesCancellationBetweenSummaryAndListsAndSumsBeyondDoublePrecision()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync("Administrador"))).AccessToken);
        using var cr=await c.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente Snapshot","PF",null,null,null));var customer=(await cr.Content.ReadFromJsonAsync<ClienteResponse>())!;
        var p=await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));using var entry=await StockTests.Move(c,p.Id,"entradas",1);
        using var draft=await SalesTests.Command(c,"/api/v1/vendas",new CriarVendaRequest(customer.Id,[new(p.Id,1)]));var sale=(await draft.Content.ReadFromJsonAsync<VendaResponse>())!;
        using var confirm=await SalesTests.Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));Assert.Equal(HttpStatusCode.OK,confirm.StatusCode);
        var sales=F.Mongo.Client.GetDatabase(F.Mongo.Name).GetCollection<BsonDocument>("sales");var id=new BsonDocument("_id",new BsonBinaryData(sale.Id,GuidRepresentation.Standard));var date=new DateTime(2030,1,1,4,0,0,DateTimeKind.Utc);
        await sales.UpdateOneAsync(id,new BsonDocument("$set",new BsonDocument("confirmadoEm",date)));
        using var scope=F.Api.Services.CreateScope();var actor=(await scope.ServiceProvider.GetRequiredService<IAutenticacao>().ValidarAsync(c.DefaultRequestHeaders.Authorization!.Parameter!,false,default))!;
        var settings=MongoClientSettings.FromConnectionString(F.Mongo.Connection);var intercepted=0;
        settings.ClusterConfigurator=cb=>cb.Subscribe<MongoDB.Driver.Core.Events.CommandSucceededEvent>(e=>
        {
            if(e.CommandName!="aggregate"||Interlocked.CompareExchange(ref intercepted,1,0)!=0)return;
            // Independent client commits after summary read, before the next read in the snapshot.
            sales.UpdateOne(id,new BsonDocument("$set",new BsonDocument{{"status","Cancelada"},{"canceladoEm",date.AddDays(1)},{"cancelamento",new BsonDocument{{"atorId",new BsonBinaryData(actor.Usuario.Id,GuidRepresentation.Standard)},{"nomeSnapshot",actor.Usuario.Nome},{"motivo","Cancelamento sintético"}}}}));
        });
        using var instrumented=new MongoClient(settings);var config=new ConfigurationBuilder().Build();
        var service=new MongoDashboard(new OperacoesCadastro(instrumented,new MongoConfiguration(F.Mongo.Connection,F.Mongo.Name,"Testing"),F.Clock,new HttpContextAccessor()),config);
        var old=await service.ConsultarAsync(actor,new(2030,1,1),new(2030,1,2),default);
        Assert.Equal(1,intercepted);Assert.Equal(1,old.Resumo.QuantidadeConfirmada);Assert.Equal("Confirmada",Assert.Single(old.Recentes).Status);Assert.Equal(1250,Assert.Single(old.MaisVendidos).ReceitaCentavos);
        var current=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?inicio=2030-01-01&fim=2030-01-02"))!;Assert.Equal(1,current.Resumo.QuantidadeCancelada);Assert.Equal(0,current.Resumo.ReceitaLiquida);Assert.Empty(current.MaisVendidos);
        var template=await sales.Find(id).SingleAsync();var copies=new List<BsonDocument>();
        for(var i=0;i<100;i++){var copy=template.DeepClone().AsBsonDocument;copy["_id"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard);copy["status"]="Confirmada";copy["cancelamento"]=BsonNull.Value;copy["canceladoEm"]=BsonNull.Value;copy["confirmadoEm"]=date.AddMonths(1);copy["totalCentavos"]=99999999900000L;var item=copy["itens"][0].AsBsonDocument;item["quantidade"]=100000;item["precoUnitarioCentavos"]=999999999L;item["totalCentavos"]=99999999900000L;copies.Add(copy);}
        await sales.InsertManyAsync(copies);var precise=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?inicio=2030-02-01&fim=2030-02-02"))!;Assert.Equal(99999999900000m,precise.Resumo.ReceitaLiquida);Assert.Equal(999999999000m,precise.Resumo.TicketMedio);
    });
    [Fact]public Task ExactMetricsBoundariesRankingSnapshotsRecentAndLowStock()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync("Administrador"))).AccessToken);
        var baseline=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?inicio=2025-01-01&fim=2025-01-02"))!;
        using var cr=await c.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente Painel","PF",null,null,null));var customer=(await cr.Content.ReadFromJsonAsync<ClienteResponse>())!;
        var category=await ProductTests.Reference(c);var p=await ProductTests.Create(c,ProductTests.Data(category.Id)with{Preco=1m});
        using(var entry=await StockTests.Move(c,p.Id,"entradas",10))Assert.Equal(HttpStatusCode.Created,entry.StatusCode);
        var ids=new List<Guid>();
        foreach(var price in new[]{1m,1.01m,15m,20m})
        {
            using var update=await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,true)with{Preco=price});Assert.Equal(HttpStatusCode.OK,update.StatusCode);p=(await update.Content.ReadFromJsonAsync<ProdutoResponse>())!;
            using var draft=await SalesTests.Command(c,"/api/v1/vendas",new CriarVendaRequest(customer.Id,[new(p.Id,1)]));var sale=(await draft.Content.ReadFromJsonAsync<VendaResponse>())!;
            using var confirm=await SalesTests.Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));Assert.Equal(HttpStatusCode.OK,confirm.StatusCode);ids.Add(sale.Id);
        }
        var sale3=(await c.GetFromJsonAsync<VendaResponse>($"/api/v1/vendas/{ids[2]}"))!;using var cancel=await SalesTests.Command(c,$"/api/v1/vendas/{ids[2]}/cancelamento",new EncerrarVendaRequest(sale3.Versao,"Cancelamento posterior"));Assert.Equal(HttpStatusCode.OK,cancel.StatusCode);
        var db=F.Mongo.Client.GetDatabase(F.Mongo.Name);var sales=db.GetCollection<BsonDocument>("sales");
        for(var i=0;i<ids.Count;i++)await sales.UpdateOneAsync(new BsonDocument("_id",new BsonBinaryData(ids[i],GuidRepresentation.Standard)),new BsonDocument("$set",new BsonDocument("confirmadoEm",new DateTime(2026,1,i==3?3:1,4,0,0,DateTimeKind.Utc))));
        using var unused=await SalesTests.Command(c,"/api/v1/vendas",new CriarVendaRequest(customer.Id,[]));
        for(var i=0;i<22;i++)await ProductTests.Create(c,ProductTests.Data(category.Id));
        var inactive=await ProductTests.Create(c,ProductTests.Data(category.Id));using var deactivate=await c.PutAsJsonAsync($"/api/v1/produtos/{inactive.Id}",ProductTests.Update(inactive,false));Assert.Equal(HttpStatusCode.OK,deactivate.StatusCode);
        var d=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?inicio=2026-01-01&fim=2026-01-03"))!;
        Assert.Equal(2,d.Resumo.QuantidadeConfirmada);Assert.Equal(1,d.Resumo.QuantidadeCancelada);Assert.Equal(2.01m,d.Resumo.ReceitaLiquida);Assert.Equal(1.01m,d.Resumo.TicketMedio);
        Assert.Equal(baseline.ProdutosCadastrados+24,d.ProdutosCadastrados);Assert.Equal(baseline.ProdutosAtivos+23,d.ProdutosAtivos);Assert.Equal(baseline.EstoqueBaixo.TotalItems+22,d.EstoqueBaixo.TotalItems);Assert.Equal(20,d.EstoqueBaixo.Items.Count);Assert.DoesNotContain(d.EstoqueBaixo.Items,x=>x.ProdutoId==inactive.Id);
        Assert.Equal(3,d.Recentes.Count);Assert.All(d.Recentes,x=>Assert.Contains(x.VendaId,ids.Take(3)));Assert.Equal(0,d.Recentes.Single(x=>x.Status=="Cancelada").ContribuicaoLiquida);
        var ranked=Assert.Single(d.MaisVendidos);Assert.Equal(2,ranked.Quantidade);Assert.Equal(2.01m,ranked.ReceitaLiquida);Assert.Equal(p.Nome,ranked.Nome);Assert.Equal(2,d.Evolucao.Count);Assert.Equal(2.01m,d.Evolucao[0].ReceitaLiquida);Assert.Equal(0,d.Evolucao[1].ReceitaLiquida);
        var empty=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?inicio=2025-01-01&fim=2025-01-02"))!;Assert.Equal(0,empty.Resumo.ReceitaLiquida);Assert.Equal(0,empty.Resumo.TicketMedio);Assert.Empty(empty.Recentes);Assert.Empty(empty.MaisVendidos);Assert.Equal(baseline.EstoqueBaixo.TotalItems+22,empty.EstoqueBaixo.TotalItems);
        // Real explain of the existing confirmation index; no schema change for07.
        var explain=await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"explain",new BsonDocument{{"aggregate","sales"},{"pipeline",new BsonArray{new BsonDocument("$match",new BsonDocument("confirmadoEm",new BsonDocument{{"$gte",new DateTime(2026,1,1,4,0,0,DateTimeKind.Utc)},{"$lt",new DateTime(2026,1,3,4,0,0,DateTimeKind.Utc)}}))}},{"hint","ix_sales_confirmado_status_id"},{"cursor",new BsonDocument()}}},{"verbosity","executionStats"}});
        Assert.Contains("ix_sales_confirmado_status_id",explain.ToJson());
    });
    [Theory][InlineData("Administrador",200)][InlineData("Vendedor",403)][InlineData("Estoquista",403)]
    public Task BackendPolicyAndValidation(string role,int expected)=>F.Mongo.SafeAsync(async()=>
    {
        using var anon=F.Client();Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync("/api/v1/dashboard")).StatusCode);
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync(role))).AccessToken);Assert.Equal(expected,(int)(await c.GetAsync("/api/v1/dashboard")).StatusCode);
        if(role=="Administrador")foreach(var query in new[]{"inicio=2026-01-01","inicio=2026-01-01&fim=2028-01-01","inicio=2026-01-01&fim=2026-01-01"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/dashboard?"+query)).StatusCode);
    });
}
