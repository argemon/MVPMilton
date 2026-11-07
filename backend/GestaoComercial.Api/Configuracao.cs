using GestaoComercial.Infrastructure.Persistence;
namespace GestaoComercial.Api;

public static class Configuracao
{
    public static MongoConfiguration Mongo(IConfiguration config, string environment) => new(
        config["Mongo:ConnectionString"] ?? (environment == "Development" ? config["MONGODB_URI"] : null),
        config["Mongo:Database"] ?? (environment == "Development" ? "gestao_comercial_dev" : null), environment);
}
