using GestaoComercial.Web.Persistence;
namespace GestaoComercial.Web;

public static class Configuracao
{
    public static MongoConfiguration Mongo(IConfiguration config, string environment)
    {
        if (!Uri.TryCreate(config["Api:BaseUrl"], UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) throw new InvalidOperationException("Configuração inválida: Api:BaseUrl (HTTPS).");
        if (config["Api:HealthBaseUrl"] is { } health && (!Uri.TryCreate(health, UriKind.Absolute, out var healthUri) || healthUri.Scheme != "https" || !string.IsNullOrEmpty(healthUri.UserInfo) || !string.IsNullOrEmpty(healthUri.Query) || !string.IsNullOrEmpty(healthUri.Fragment))) throw new InvalidOperationException("Configuração inválida: Api:HealthBaseUrl (HTTPS).");
        return new(config["Mongo:ConnectionString"] ?? (environment == "Development" ? config["MONGODB_URI"] : null), config["Mongo:Database"] ?? (environment == "Development" ? "gestao_web_dev" : null), environment);
    }
}
