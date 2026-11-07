using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Persistence;

public sealed class MongoConfiguration
{
    private readonly MongoClientSettings _settings;
    public string DatabaseName { get; }
    public bool IsAtlas { get; }
    public MongoConfiguration(string? connection, string? database, string environment)
    {
        if (environment is not ("Development" or "Testing" or "Staging" or "Production")) throw new InvalidOperationException("Configuração inválida: ambiente.");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configuração obrigatória: Mongo:ConnectionString ou MONGODB_URI em Development.");
        if (string.IsNullOrWhiteSpace(database) || !System.Text.RegularExpressions.Regex.IsMatch(database, "^[a-z][a-z0-9_]{2,37}$")) throw new InvalidOperationException("Configuração inválida: Mongo:Database.");
        try
        {
            _settings = MongoClientSettings.FromConnectionString(connection);
            if (System.Text.RegularExpressions.Regex.IsMatch(connection, "(?:tlsInsecure|tlsAllowInvalidCertificates|tlsAllowInvalidHostnames)=true", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) throw new ArgumentException();
            IsAtlas = connection.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase) && _settings.Servers.All(s => s.Host.EndsWith(".mongodb.net", StringComparison.OrdinalIgnoreCase));
            var loopback = _settings.Servers.All(s => s.Host is "localhost" or "127.0.0.1" or "::1");
            if (!_settings.UseTls && !loopback) throw new ArgumentException();
            if (environment is "Staging" or "Production" && (loopback || !_settings.UseTls || _settings.Credential is null)) throw new ArgumentException();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidOperationException("Configuração inválida: Mongo:ConnectionString (formato/TLS/credencial).");
        }
        _settings.ServerSelectionTimeout = TimeSpan.FromSeconds(6);
        _settings.ConnectTimeout = TimeSpan.FromSeconds(5);
        _settings.SocketTimeout = TimeSpan.FromSeconds(8);
        _settings.ReadPreference = ReadPreference.Primary;
        _settings.WriteConcern = WriteConcern.WMajority;
        _settings.ApplicationName = "GestaoComercial.Infrastructure";
        DatabaseName = database;
    }
    public IMongoClient CreateClient() => new MongoClient(_settings);
}
