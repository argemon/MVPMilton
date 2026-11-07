using System.Reflection;
using System.Net.Http.Json;
using System.Runtime.ExceptionServices;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Estoque;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

// Decorate only session control. Every read/write and actual commit/abort uses
// the real authenticated MongoDB and the original driver session.
public sealed class CommitFault
{
    public string Mode { get; set; }="";
    public int Starts { get; set; }
    public int Commits { get; set; }
    public int Aborts { get; set; }
    internal static MongoException Error(string label) { var e=new MongoException("Synthetic transaction control fault.");e.AddErrorLabel(label);return e; }
}
public class SessionFaultProxy : DispatchProxy
{
    public IClientSessionHandle Target { get; set; }=null!;
    public CommitFault Fault { get; set; }=null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        if(method!.Name==nameof(IClientSessionHandle.StartTransaction))Fault.Starts++;
        if(method.Name==nameof(IClientSessionHandle.AbortTransactionAsync))Fault.Aborts++;
        if(method.Name==nameof(IClientSessionHandle.CommitTransactionAsync))return Commit((CancellationToken)args![0]!);
        try{return method.Invoke(Target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException!).Throw();throw;}
    }
    private async Task Commit(CancellationToken ct)
    {
        Fault.Commits++;
        if(Fault.Mode=="transient"&&Fault.Commits==1||Fault.Mode=="exhausted")throw CommitFault.Error("TransientTransactionError");
        await Target.CommitTransactionAsync(ct);
        if(Fault.Mode=="unknown"&&Fault.Commits==1||Fault.Mode=="ambiguous")throw CommitFault.Error("UnknownTransactionCommitResult");
    }
}
public class ClientFaultProxy : DispatchProxy
{
    public IMongoClient Target { get; set; }=null!;
    public CommitFault Fault { get; set; }=null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        if(method!.Name==nameof(IMongoClient.StartSessionAsync))return Session((Task<IClientSessionHandle>)method.Invoke(Target,args)!);
        try{return method.Invoke(Target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException!).Throw();throw;}
    }
    private async Task<IClientSessionHandle> Session(Task<IClientSessionHandle> pending)
    {
        var session=DispatchProxy.Create<IClientSessionHandle,SessionFaultProxy>();var proxy=(SessionFaultProxy)(object)session;proxy.Target=await pending;proxy.Fault=Fault;return session;
    }
}
public sealed class StockCommitTests(StockFixture f) : IClassFixture<StockFixture>
{
    [Theory][InlineData("unknown")][InlineData("transient")][InlineData("ambiguous")][InlineData("exhausted")]
    public Task RealTransactionsResolveControlFaultsWithoutDuplicatingMovements(string mode)=>f.Auth.Mongo.SafeAsync(async()=>
    {
        var auth=f.Auth;var login=await auth.LoginAsync(await auth.SeedAsync("Administrador"));using var c=auth.Client(login.AccessToken);var p=await ProductTests.Create(c,ProductTests.Data((await ProductTests.Reference(c)).Id));
        using var scope=auth.Api.Services.CreateScope();var services=scope.ServiceProvider;var actor=(await services.GetRequiredService<IAutenticacao>().ValidarAsync(login.AccessToken,false,default))!;
        var fault=new CommitFault{Mode=mode};var client=DispatchProxy.Create<IMongoClient,ClientFaultProxy>();var proxy=(ClientFaultProxy)(object)client;proxy.Target=auth.Mongo.Client;proxy.Fault=fault;
        var service=new MongoEstoque(services.GetRequiredService<OperacoesCadastro>(),client,services.GetRequiredService<ILogger<MongoEstoque>>(),services.GetRequiredService<IConfiguration>(),services.GetRequiredService<EstoqueSchema>());var key=Guid.NewGuid();
        Movimento? original=null;
        if(mode=="ambiguous") { var e=await Assert.ThrowsAsync<CadastroException>(()=>service.MovimentarAsync(actor,key,p.Id,"Entrada",3,"Falha sintética de commit",default));Assert.Equal("RESULTADO_DESCONHECIDO",e.Code);Assert.Equal(503,e.Status); }
        else if(mode=="exhausted") { var e=await Assert.ThrowsAsync<MongoException>(()=>service.MovimentarAsync(actor,key,p.Id,"Entrada",3,"Falha sintética de commit",default));Assert.True(e.HasErrorLabel("TransientTransactionError"));Assert.Equal(0,(await c.GetFromJsonAsync<GestaoComercial.Contracts.ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!.Saldo); }
        else original=await service.MovimentarAsync(actor,key,p.Id,"Entrada",3,"Falha sintética de commit",default);
        Assert.Equal(mode=="exhausted"?3:mode=="transient"?2:1,fault.Starts);Assert.Equal(mode=="exhausted"?3:mode=="transient"?1:0,fault.Aborts);Assert.True(fault.Commits>=2);
        var resolved=await services.GetRequiredService<IEstoque>().MovimentarAsync(actor,key,p.Id,"Entrada",3,"Falha sintética de commit",default);
        if(original is not null)Assert.Equal(original,resolved);
        Assert.Equal(3,resolved.SaldoDepois);var db=auth.Mongo.Client.GetDatabase(auth.Mongo.Name);Assert.Equal(1,await db.GetCollection<BsonDocument>("stock_movements").CountDocumentsAsync(new BsonDocument("produtoId",new BsonBinaryData(p.Id,GuidRepresentation.Standard))));
        Assert.Equal(3,(await c.GetFromJsonAsync<GestaoComercial.Contracts.ProdutoResponse>($"/api/v1/produtos/{p.Id}"))!.Saldo);
    });
}
