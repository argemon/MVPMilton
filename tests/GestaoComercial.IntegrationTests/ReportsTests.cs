using System.Net;
using System.Net.Http.Json;
using System.Text;
using GestaoComercial.Contracts;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Relatorios;
using GestaoComercial.Infrastructure.Persistence;
using GestaoComercial.Infrastructure.Relatorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class ReportsTests(SalesFixture fixture) : IClassFixture<SalesFixture>
{
    private AuthFixture F=>fixture.Auth;
    private const string Period="inicio=2032-01-01&fim=2032-02-01";
    [Fact]public Task AllFiveReportsUseSnapshotsFiltersTotalsDeterministicPagesAndSafeCsv()=>F.Mongo.SafeAsync(async()=>
    {
        var admin=await F.SeedAsync("Administrador");using var c=F.Client((await F.LoginAsync(admin)).AccessToken);var other=await F.SeedAsync("Vendedor");
        using var cr=await c.PostAsJsonAsync("/api/v1/clientes",new CriarClienteRequest("Cliente relatório","PF",null,null,null));var customer=(await cr.Content.ReadFromJsonAsync<ClienteResponse>())!;
        var p=await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));using var entry=await StockTests.Move(c,p.Id,"entradas",10);Assert.Equal(HttpStatusCode.Created,entry.StatusCode);
        using var draft=await SalesTests.Command(c,"/api/v1/vendas",new CriarVendaRequest(customer.Id,[new(p.Id,2)]));var sale=(await draft.Content.ReadFromJsonAsync<VendaResponse>())!;using var confirm=await SalesTests.Command(c,$"/api/v1/vendas/{sale.Id}/confirmacao",new ConfirmarVendaRequest(sale.Versao));Assert.Equal(HttpStatusCode.OK,confirm.StatusCode);
        var db=F.Mongo.Client.GetDatabase(F.Mongo.Name);var sales=db.GetCollection<BsonDocument>("sales");var template=await sales.Find(new BsonDocument("_id",new BsonBinaryData(sale.Id,GuidRepresentation.Standard))).SingleAsync();var start=new DateTime(2032,1,1,4,0,0,DateTimeKind.Utc);var copies=new List<BsonDocument>();
        for(var i=0;i<22;i++)
        {
            var d=template.DeepClone().AsBsonDocument;d["_id"]=new BsonBinaryData(Guid.NewGuid(),GuidRepresentation.Standard);d["confirmadoEm"]=i==21?start.AddMonths(1):start.AddMinutes(i);d["clienteSnapshot"]["nome"]="=1+1;\"Cliente Ação\"";d["itens"][0]["nomeSnapshot"]="=1+1;\"Produto Ação\"";
            if(i==1){d["criadorId"]=new BsonBinaryData(other.Id,GuidRepresentation.Standard);d["criadorNomeSnapshot"]="Outro operador histórico";d["status"]="Cancelada";d["canceladoEm"]=start.AddMonths(2);d["cancelamento"]=new BsonDocument{{"atorId",new BsonBinaryData(admin.Id,GuidRepresentation.Standard)},{"nomeSnapshot",admin.Nome},{"motivo","Cancelamento posterior sintético"}};d["totalCentavos"]=1500L;d["itens"][0]["precoUnitarioCentavos"]=750L;d["itens"][0]["totalCentavos"]=1500L;}
            copies.Add(d);
        }
        await sales.InsertManyAsync(copies);
        // Current product name and active flag cannot rewrite commercial snapshots.
        using var rename=await c.PutAsJsonAsync($"/api/v1/produtos/{p.Id}",ProductTests.Update(p,false)with{Nome="Produto atual renomeado"});Assert.Equal(HttpStatusCode.OK,rename.StatusCode);
        var report=(await c.GetFromJsonAsync<RelatorioVendasResponse>("/api/v1/relatorios/vendas?"+Period+"&pageSize=20&ordenacao=dataAsc"))!;
        Assert.Equal(21,report.TotalItems);Assert.Equal(20,report.Items.Count);Assert.Equal(20,report.Totais.QuantidadeConfirmada);Assert.Equal(1,report.Totais.QuantidadeCancelada);Assert.Equal(500m,report.Totais.ReceitaLiquida);Assert.Equal(25m,report.Totais.TicketMedio);Assert.Equal(new DateTimeOffset(start),report.Items[0].ConfirmadoEm);
        Assert.Equal(15m,report.Items.Single(x=>x.Status=="Cancelada").TotalOriginal);Assert.Equal(0,report.Items.Single(x=>x.Status=="Cancelada").ContribuicaoLiquida);
        var second=(await c.GetFromJsonAsync<RelatorioVendasResponse>("/api/v1/relatorios/vendas?"+Period+"&pageSize=20&page=2&ordenacao=dataAsc"))!;Assert.Single(second.Items);Assert.Equal(report.Totais,second.Totais);Assert.DoesNotContain(second.Items[0].VendaId,report.Items.Select(x=>x.VendaId));
        var repeated=(await c.GetFromJsonAsync<RelatorioVendasResponse>("/api/v1/relatorios/vendas?"+Period+"&pageSize=20&ordenacao=dataAsc"))!;Assert.Equal(report.Items.Select(x=>x.VendaId),repeated.Items.Select(x=>x.VendaId));
        var desc=(await c.GetFromJsonAsync<RelatorioVendasResponse>("/api/v1/relatorios/vendas?"+Period+"&ordenacao=dataDesc"))!;Assert.Equal(second.Items[0].VendaId,desc.Items[0].VendaId);
        var panel=(await c.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?"+Period))!;Assert.Equal(panel.Resumo,report.Totais);
        var operators=(await c.GetFromJsonAsync<RelatorioOperadoresResponse>("/api/v1/relatorios/operadores?"+Period))!;Assert.Equal(2,operators.TotalItems);Assert.Equal(500m,operators.Items[0].ReceitaLiquida);Assert.Equal("Outro operador histórico",operators.Items[1].Nome);Assert.Equal(0,operators.Items[1].ReceitaLiquida);
        var filtered=(await c.GetFromJsonAsync<RelatorioVendasResponse>($"/api/v1/relatorios/vendas?{Period}&operadorId={other.Id}"))!;Assert.Single(filtered.Items);Assert.Equal(0,filtered.Totais.ReceitaLiquida);Assert.Equal(other.Id,filtered.Filtros.OperadorId);
        var ranked=(await c.GetFromJsonAsync<RelatorioProdutosResponse>("/api/v1/relatorios/produtos?"+Period))!;var product=Assert.Single(ranked.Items);Assert.Equal(40,product.Quantidade);Assert.Equal(500m,product.ReceitaLiquida);Assert.StartsWith("=1+1",product.Nome);Assert.Equal(panel.MaisVendidos[0],product);
        var stock=(await c.GetFromJsonAsync<RelatorioEstoqueResponse>("/api/v1/relatorios/estoque?search="+Uri.EscapeDataString(p.Sku)+"&ativo=todos&ordenacao=saldoDesc"))!;var current=Assert.Single(stock.Items);Assert.False(current.Ativo);Assert.Equal(8,current.Saldo);Assert.Equal(8,stock.Totais.Saldo);Assert.Null(stock.Filtros.Inicio);
        Assert.Empty((await c.GetFromJsonAsync<RelatorioEstoqueResponse>("/api/v1/relatorios/estoque?search="+p.Sku))!.Items);
        var movements=db.GetCollection<BsonDocument>("stock_movements");await movements.UpdateManyAsync(new BsonDocument("produtoId",new BsonBinaryData(p.Id,GuidRepresentation.Standard)),new BsonDocument("$set",new BsonDocument("ocorridoEm",start)));
        var history=(await c.GetFromJsonAsync<RelatorioMovimentosResponse>($"/api/v1/relatorios/movimentos?{Period}&produtoId={p.Id}&pageSize=1"))!;Assert.Equal(2,history.TotalItems);Assert.Single(history.Items);Assert.Equal(new TotaisMovimentosResponse(10,2,8),history.Totais);
        var onlySale=(await c.GetFromJsonAsync<RelatorioMovimentosResponse>($"/api/v1/relatorios/movimentos?{Period}&produtoId={p.Id}&tipo=Venda"))!;Assert.Single(onlySale.Items);Assert.Equal(-2,onlySale.Totais.Delta);Assert.Equal(sale.Id,onlySale.Items[0].VendaId);
        foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})
        {
            var query=kind=="estoque"?"ativo=todos":Period;using var csv=await c.GetAsync($"/api/v1/relatorios/{kind}/exportacao?{query}");Assert.Equal(HttpStatusCode.OK,csv.StatusCode);Assert.Equal("text/csv",csv.Content.Headers.ContentType?.MediaType);Assert.Equal("attachment",csv.Content.Headers.ContentDisposition?.DispositionType);var text=await csv.Content.ReadAsStringAsync();
            Assert.DoesNotContain("accessToken",text,StringComparison.OrdinalIgnoreCase);Assert.Contains("America/Cuiaba",text);if(kind=="vendas"){Assert.Equal(23,text.Split("\r\n").Length);Assert.Contains("\"'=1+1;\"\"Cliente Ação\"\"\"",text);Assert.Contains(";25.00;25.00;",text);}if(kind=="produtos")Assert.Contains("\"'=1+1;\"\"Produto Ação\"\"\"",text);
        }
        var empty=(await c.GetFromJsonAsync<RelatorioVendasResponse>("/api/v1/relatorios/vendas?inicio=2020-01-01&fim=2020-01-02"))!;Assert.Empty(empty.Items);Assert.Equal(0,empty.Totais.ReceitaLiquida);using var emptyCsv=await c.GetAsync("/api/v1/relatorios/vendas/exportacao?inicio=2020-01-01&fim=2020-01-02");Assert.Equal(2,(await emptyCsv.Content.ReadAsStringAsync()).Split("\r\n").Length);
        Assert.Empty((await c.GetFromJsonAsync<RelatorioMovimentosResponse>($"/api/v1/relatorios/movimentos?{Period}&produtoId={Guid.NewGuid()}"))!.Items);
        foreach(var bad in new[]{"inicio=2032-01-01","inicio=2032-01-01&fim=2034-01-01",Period+"&page=0",Period+"&pageSize=101",Period+"&operadorId=invalid",Period+"&operadorId="+Guid.Empty,Period+"&ordenacao=invalid",Period+"&page=1&page=2",Period+"&unknown=true"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/relatorios/vendas?"+bad)).StatusCode);
        foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync($"/api/v1/relatorios/{kind}/exportacao?"+(kind=="estoque"?"ativo=todos":Period)+"&page=" )).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/relatorios/estoque?inicio=2032-01-01&fim=2032-02-01")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/relatorios/estoque?ativo=invalid")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/relatorios/movimentos?"+Period+"&tipo=invalid")).StatusCode);
    });
    [Theory][InlineData("Administrador")][InlineData("Vendedor")][InlineData("Estoquista")]
    public Task EveryQueryAndExportEnforcesSameBackendPolicy(string role)=>F.Mongo.SafeAsync(async()=>
    {
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync(role))).AccessToken);using var anon=F.Client();
        foreach(var kind in new[]{"vendas","operadores","produtos","estoque","movimentos"})foreach(var suffix in new[]{"","/exportacao"})
        {
            var path="/api/v1/relatorios/"+kind+suffix+(kind=="estoque"?"":"?"+Period);Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(path)).StatusCode);var allowed=role=="Administrador"||role=="Estoquista"&&kind is "estoque" or "movimentos";Assert.Equal(allowed?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await c.GetAsync(path)).StatusCode);
        }
    });
    [Fact]public Task StockSnapshotKeepsTotalsAndRowsTogetherDuringCommittedMovement()=>F.Mongo.SafeAsync(async()=>
    {
        using var c=F.Client((await F.LoginAsync(await F.SeedAsync("Administrador"))).AccessToken);var p=await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));using var scope=F.Api.Services.CreateScope();var actor=(await scope.ServiceProvider.GetRequiredService<IAutenticacao>().ValidarAsync(c.DefaultRequestHeaders.Authorization!.Parameter!,false,default))!;
        var settings=MongoClientSettings.FromConnectionString(F.Mongo.Connection);var intercepted=0;settings.ClusterConfigurator=cb=>cb.Subscribe<MongoDB.Driver.Core.Events.CommandSucceededEvent>(e=>{if(e.CommandName!="aggregate"||Interlocked.CompareExchange(ref intercepted,1,0)!=0)return;using var move=StockTests.Move(c,p.Id,"entradas",7).GetAwaiter().GetResult();Assert.Equal(HttpStatusCode.Created,move.StatusCode);});
        using var client=new MongoClient(settings);var service=new MongoRelatorios(new OperacoesCadastro(client,new MongoConfiguration(F.Mongo.Connection,F.Mongo.Name,"Testing"),F.Clock,new HttpContextAccessor()),new ConfigurationBuilder().Build());
        var before=await service.EstoqueAsync(actor,new ConsultaRelatorio(Search:p.Sku),default);Assert.Equal(1,intercepted);Assert.Equal(0,before.Totais.Saldo);Assert.Equal(0,Assert.Single(before.Dados.Items).Saldo);
        var after=(await c.GetFromJsonAsync<RelatorioEstoqueResponse>("/api/v1/relatorios/estoque?search="+p.Sku))!;Assert.Equal(7,after.Totais.Saldo);Assert.Equal(7,Assert.Single(after.Items).Saldo);
    });
}
