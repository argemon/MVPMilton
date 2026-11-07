extern alias ApiHost;
extern alias WebHost;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;
using LocalEnvironment = ApiHost::GestaoComercial.Hosting.LocalEnvironment;

public sealed class ConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("not-a-uri-password-marker")]
    [InlineData("mongodb://remote.invalid/?tls=false")]
    [InlineData("mongodb://localhost/?tlsInsecure=true")]
    public void InvalidConnectionHasSafeError(string? uri)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new MongoConfiguration(uri, "gc_test", "Testing"));
        Assert.Contains("Configuração", error.Message);
        Assert.DoesNotContain("password-marker", error.ToString());
    }
    [Theory]
    [InlineData("production.database")]
    [InlineData("../db")]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void InvalidDatabaseRejected(string db) => Assert.Throws<InvalidOperationException>(() => new MongoConfiguration("mongodb://localhost:27017", db, "Testing"));

    [Fact]
    public void ProductionDoesNotUseDevelopmentAlias()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MONGODB_URI"] = "mongodb://localhost", ["Mongo:Database"] = "gc_test" }).Build();
        Assert.Throws<InvalidOperationException>(() => ApiHost::GestaoComercial.Api.Configuracao.Mongo(config, "Production"));
    }

    [Theory]
    [InlineData("http://api/")]
    [InlineData("https://user:secret@api/")]
    [InlineData("/relative")]
    public void InvalidApiUrlRejected(string url)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = url }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => WebHost::GestaoComercial.Web.Configuracao.Mongo(config, "Testing"));
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public void EnvironmentFilePreservesExistingVariablesAndDoesNotExpandValues()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gc_env_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var key = "GC_M00_" + Guid.NewGuid().ToString("N");
        var file = Path.Combine(dir, ".env");
        try
        {
            File.WriteAllText(file, key + "=file-value\nLITERAL=\"a=$VALUE\"\nEMPTY=\n");
            Environment.SetEnvironmentVariable(key, "process-value");
            var config = new ConfigurationManager();
            LocalEnvironment.AddDevelopmentFile(config, "Development", dir);
            Assert.Equal("process-value", config[key]);
            Assert.Equal("a=$VALUE", config["LITERAL"]);
            Assert.Equal("", config["EMPTY"]);
            var testing = new ConfigurationManager();
            LocalEnvironment.AddDevelopmentFile(testing, "Testing", dir);
            Assert.Null(testing["LITERAL"]);
            Assert.Equal(key + "=file-value\nLITERAL=\"a=$VALUE\"\nEMPTY=\n", File.ReadAllText(file));
        }
        finally { Environment.SetEnvironmentVariable(key, null); Directory.Delete(dir, true); }
    }
}

public sealed class DemonstrationConfigurationTests
{
    [Xunit.Fact]
    public void DevelopmentHostsShareTheExistingDatabase()
    {
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"MONGODB_URI","mongodb://localhost"},{"Api:BaseUrl","https://localhost/"}}).Build();
        Xunit.Assert.Equal(ApiHost::GestaoComercial.Api.Configuracao.Mongo(config,"Development").DatabaseName,WebHost::GestaoComercial.Web.Configuracao.Mongo(config,"Development").DatabaseName);
    }
}
