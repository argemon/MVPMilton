using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace GestaoComercial.Infrastructure.Clientes;

public sealed class ClienteDocumento
{
    [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid Id { get; set; } = Guid.NewGuid();
    [BsonElement("schemaVersion")] public int SchemaVersion { get; set; } = 1;
    [BsonElement("nome")] public string Nome { get; set; } = "";
    [BsonElement("nomeNormalizado")] public string NomeNormalizado { get; set; } = "";
    [BsonElement("tipo")] public string Tipo { get; set; } = "PF";
    [BsonElement("documento")] public string? Documento { get; set; }
    [BsonElement("email")] public string? Email { get; set; }
    [BsonElement("telefone")] public string? Telefone { get; set; }
    [BsonElement("ativo")] public bool Ativo { get; set; } = true;
    [BsonElement("versao")] public long Versao { get; set; } = 1;
    [BsonElement("guardVersion")] public long GuardVersion { get; set; }
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; }
}
