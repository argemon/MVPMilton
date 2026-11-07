using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace GestaoComercial.Infrastructure.Identity;

public sealed class UsuarioIdentity
{
    [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid Id { get; set; } = Guid.NewGuid();
    [BsonElement("schemaVersion")] public int SchemaVersion { get; set; } = 1;
    [BsonElement("nome")] public string Nome { get; set; } = "";
    [BsonElement("nomeNormalizado")] public string NomeNormalizado { get; set; } = "";
    [BsonElement("email")] public string Email { get; set; } = "";
    [BsonElement("emailNormalizado")] public string EmailNormalizado { get; set; } = "";
    [BsonElement("passwordHash")] public string PasswordHash { get; set; } = "";
    [BsonElement("perfil")] public string Perfil { get; set; } = "";
    [BsonElement("ativo")] public bool Ativo { get; set; } = true;
    [BsonElement("deveAlterarSenha")] public bool DeveAlterarSenha { get; set; } = true;
    [BsonElement("securityStamp")] public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    [BsonElement("failedAccessCount")] public int FailedAccessCount { get; set; }
    [BsonElement("lockoutEnd")] public DateTime? LockoutEnd { get; set; }
    [BsonElement("employeeId"), BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid? EmployeeId { get; set; }
    [BsonElement("versao")] public long Versao { get; set; } = 1;
    [BsonElement("guardVersion")] public long GuardVersion { get; set; }
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; }
}
public sealed class SessaoDocumento
{
    [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid Id { get; set; } = Guid.NewGuid();
    [BsonElement("schemaVersion")] public int SchemaVersion { get; set; } = 1;
    [BsonElement("usuarioId"), BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid UsuarioId { get; set; }
    [BsonElement("tokenHash")] public byte[] TokenHash { get; set; } = [];
    [BsonElement("securityStamp")] public string Stamp { get; set; } = "";
    [BsonElement("criadoEm")] public DateTime CriadoEm { get; set; }
    [BsonElement("expiraEm")] public DateTime ExpiraEm { get; set; }
    [BsonElement("ultimoUsoEm")] public DateTime UltimoUsoEm { get; set; }
    [BsonElement("revogadaEm")] public DateTime? RevogadaEm { get; set; }
}
