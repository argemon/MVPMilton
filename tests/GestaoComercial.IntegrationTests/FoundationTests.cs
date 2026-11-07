extern alias ApiHost;
extern alias WebHost;
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using Xunit;
using Xunit.Sdk;

public sealed class MongoFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;
    private bool _ownsDatabase;
    public string Connection { get; private set; } = "";
    public string Name { get; } = "gc_m00_" + Guid.NewGuid().ToString("N")[..24] + "_test";
    public IMongoClient Client { get; private set; } = null!;
    public MongoFoundation Foundation { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        var phase = "configuration";
        try
        {
            if (Environment.GetEnvironmentVariable("TEST_MONGO_CONTAINER") == "true")
            {
                _container = new MongoDbBuilder("mongo:8.0.17").WithReplicaSet("rs0").Build();
                await _container.StartAsync();
                var url = new MongoUrlBuilder(_container.GetConnectionString()) { DirectConnection = true }; Connection = url.ToString();
            }
            else
            {
                Connection = Environment.GetEnvironmentVariable("TEST_MONGODB_URI") ?? "";
                if (string.IsNullOrWhiteSpace(Connection))
                {
                    for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
                    {
                        var file = Path.Combine(d.FullName, ".env"); if (!File.Exists(file)) continue;
                        var entries = ApiHost::GestaoComercial.Hosting.LocalEnvironment.Read(file);
                        Connection = entries.GetValueOrDefault("MONGODB_URI") ?? ""; break;
                    }
                }
            }
            var config = new MongoConfiguration(Connection, Name, "Testing"); Client = config.CreateClient(); Foundation = new MongoFoundation(Client, config);
            phase = "topology";
            if (!await Foundation.SupportsTransactionsAsync(CancellationToken.None)) throw new InvalidOperationException();
            phase = "isolated-database";
            using var names = await Client.GetDatabase(Name).ListCollectionNamesAsync();
            if ((await names.ToListAsync()).Count != 0) throw new InvalidOperationException();
            _ownsDatabase = true;
        }
        catch (Exception e)
        {
            var category = e.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ? "autenticação/permissão" : e.Message.Contains("limit", StringComparison.OrdinalIgnoreCase) ? "limite do serviço" : "conexão/comando";
            throw new XunitException("Integração MongoDB indisponível: " + phase + " (" + e.GetType().Name + ", código " + (e is MongoCommandException command ? command.Code : 0) + ", categoria " + category + "). Nenhuma mensagem sensível foi registrada.");
        }
    }
    public async Task DisposeAsync()
    {
        try
        {
            if (_ownsDatabase && System.Text.RegularExpressions.Regex.IsMatch(Name, "^gc_m00_[a-f0-9]{24}_test$")) await Client.DropDatabaseAsync(Name);
        }
        catch (Exception e) { throw new XunitException("Limpeza do banco exclusivo de teste falhou (" + e.GetType().Name + "). Nenhum outro banco foi removido."); }
        finally { if (_container is not null) await _container.DisposeAsync(); (Client as IDisposable)?.Dispose(); }
    }
    public async Task SafeAsync(Func<Task> action)
    {
        try { await action(); } catch (XunitException) { throw; } catch (Exception e) { throw new XunitException("Operação MongoDB de teste falhou (" + e.GetType().Name + ", código " + (e is MongoCommandException m ? m.Code : 0) + ", transiente " + (e is MongoException me && me.HasErrorLabel("TransientTransactionError")) + "). Detalhes sensíveis omitidos.\n" + e.StackTrace); }
    }
}

public sealed class FoundationTests(MongoFixture fixture) : IClassFixture<MongoFixture>
{
    [Fact]
    public Task ExplicitInitializationIsIdempotentAndWebIndependent() => fixture.SafeAsync(async () =>
    {
        await fixture.Foundation.InitializeAsync(CancellationToken.None);
        var db = fixture.Client.GetDatabase(fixture.Name); var catalog = db.GetCollection<BsonDocument>("persistence_versions");
        var first = await catalog.Find(new BsonDocument("_id", MongoFoundation.BaselineId)).SingleAsync();
        await fixture.Foundation.InitializeAsync(CancellationToken.None);
        var again = await catalog.Find(new BsonDocument("_id", MongoFoundation.BaselineId)).SingleAsync();
        Assert.Equal(first, again); Assert.True(await fixture.Foundation.IsReadyAsync(CancellationToken.None));
        var webConfig = new WebHost::GestaoComercial.Web.Persistence.MongoConfiguration(fixture.Connection, fixture.Name, "Testing");
        var web = new WebHost::GestaoComercial.Web.Persistence.MongoFoundation(fixture.Client, webConfig);
        await web.InitializeAsync(CancellationToken.None); Assert.True(await web.IsReadyAsync(CancellationToken.None));
        using var indexes = await catalog.Indexes.ListAsync(); Assert.Contains(await indexes.ToListAsync(), i => i["name"] == "ux_persistence_version" && i["unique"].ToBoolean());
    });

    [Fact]
    public Task TransactionCommitsAndAbortsDisposableDocuments() => fixture.SafeAsync(async () =>
    {
        var db = fixture.Client.GetDatabase(fixture.Name);
        await db.CreateCollectionAsync("transaction_probe");
        var collection = db.GetCollection<BsonDocument>("transaction_probe");
        await db.CreateCollectionAsync("transaction_probe_other");
        var other = db.GetCollection<BsonDocument>("transaction_probe_other");
        using (var session = await fixture.Client.StartSessionAsync())
        {
            session.StartTransaction(new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
            await collection.InsertOneAsync(session, new BsonDocument("_id", "committed"));
            await other.InsertOneAsync(session, new BsonDocument("_id", "committed"));
            await session.CommitTransactionAsync();
        }
        using (var session = await fixture.Client.StartSessionAsync())
        {
            session.StartTransaction(new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
            await collection.InsertOneAsync(session, new BsonDocument("_id", "aborted"));
            await other.InsertOneAsync(session, new BsonDocument("_id", "aborted"));
            await session.AbortTransactionAsync();
        }
        Assert.Equal(1, await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0, await collection.CountDocumentsAsync(new BsonDocument("_id", "aborted")));
        Assert.Equal(1, await other.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0, await other.CountDocumentsAsync(new BsonDocument("_id", "aborted")));
    });

    [Fact]
    public Task BaselineRejectsInvalidDocumentAndDoesNotOverwriteIncompatibleVersion() => fixture.SafeAsync(async () =>
    {
        await fixture.Foundation.InitializeAsync(CancellationToken.None);
        var collection = fixture.Client.GetDatabase(fixture.Name).GetCollection<BsonDocument>("persistence_versions");
        var error = await Assert.ThrowsAsync<MongoWriteException>(() => collection.InsertOneAsync(new BsonDocument("_id", "invalid")));
        Assert.Equal(121, error.WriteError.Code);
        var filter = new BsonDocument("_id", MongoFoundation.BaselineId);
        try
        {
            await collection.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("checksum", new string('0', 64)));
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Foundation.InitializeAsync(CancellationToken.None));
            Assert.Equal(new string('0', 64), (await collection.Find(filter).SingleAsync())["checksum"].AsString);
        }
        finally { await collection.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("checksum", MongoFoundation.Checksum)); }
    });

    [Fact]
    public Task ApiHostReadsRealReadyAndWebUsesTypedHttpClient() => fixture.SafeAsync(async () =>
    {
        await fixture.Foundation.InitializeAsync(CancellationToken.None);
        await using var factory = new WebApplicationFactory<ApiHost::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing"); b.UseSetting("Mongo:ConnectionString", fixture.Connection); b.UseSetting("Mongo:Database", fixture.Name);
            b.ConfigureServices(s => s.AddSingleton<IStartupFilter, InternalPortFilter>());
        });
        await factory.Services.GetRequiredService<GestaoComercial.Infrastructure.Identity.AuthSchema>().InitializeAsync(default);
        await factory.Services.GetRequiredService<GestaoComercial.Infrastructure.Funcionarios.FuncionariosSchema>().InitializeAsync(default);
        await factory.Services.GetRequiredService<GestaoComercial.Infrastructure.Clientes.ClientesSchema>().InitializeAsync(default);
        await factory.Services.GetRequiredService<GestaoComercial.Infrastructure.Produtos.ProdutosSchema>().InitializeAsync(default);
        await factory.Services.GetRequiredService<GestaoComercial.Infrastructure.Estoque.EstoqueSchema>().InitializeAsync(default);
        await factory.Services.GetRequiredService<GestaoComercial.Infrastructure.Vendas.VendasSchema>().InitializeAsync(default);
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-Internal", "1");
        Assert.Equal("{\"status\":\"ready\"}", await client.GetStringAsync("/health/ready"));
        var typed = new WebHost::GestaoComercial.Web.ApiClients.EstadoApiClient(client);
        Assert.True(await typed.ConsultarAsync(CancellationToken.None));
    });
}
public sealed class InternalPortFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app => { app.Use(async (c, n) => { if (c.Request.Headers.ContainsKey("X-Test-Internal")) c.Connection.LocalPort = 8081; await n(c); }); next(app); };
}
