using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace GestaoComercial.Infrastructure.Produtos;

public sealed class ReferenciaDocumento
{
    [BsonId,BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid Id { get; set; }=Guid.NewGuid();
    [BsonElement("schemaVersion")] public int SchemaVersion { get; set; }=1;
    [BsonElement("nome")] public string Nome { get; set; }="";
    [BsonElement("nomeNormalizado")] public string NomeNormalizado { get; set; }="";
    [BsonElement("ativo")] public bool Ativo { get; set; }=true;
    [BsonElement("versao")] public long Versao { get; set; }=1;
    [BsonElement("guardVersion")] public long GuardVersion { get; set; }
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; }
}
public sealed class SaldoDocumento
{
    [BsonElement("quantidade")] public int Quantidade { get; set; }
    [BsonElement("versao")] public long Versao { get; set; }=1;
}
public sealed class ImagemDocumento
{
    [BsonElement("chave")] public string Chave { get; set; }="";
    [BsonElement("mime")] public string Mime { get; set; }="image/png";
    [BsonElement("tamanhoBytes")] public int TamanhoBytes { get; set; }
}
public sealed class ProdutoDocumento
{
    [BsonId,BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid Id { get; set; }=Guid.NewGuid();
    [BsonElement("schemaVersion")] public int SchemaVersion { get; set; }=1;
    [BsonElement("sku")] public string Sku { get; set; }="";
    [BsonElement("skuNormalizado")] public string SkuNormalizado { get; set; }="";
    [BsonElement("nome")] public string Nome { get; set; }="";
    [BsonElement("nomeNormalizado")] public string NomeNormalizado { get; set; }="";
    [BsonElement("descricao")] public string? Descricao { get; set; }
    [BsonElement("categoriaId"),BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid CategoriaId { get; set; }
    [BsonElement("marcaId"),BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid? MarcaId { get; set; }
    [BsonElement("precoCentavos")] public long PrecoCentavos { get; set; }
    [BsonElement("moeda")] public string Moeda { get; set; }="BRL";
    [BsonElement("estoqueMinimo")] public int EstoqueMinimo { get; set; }
    [BsonElement("saldo")] public SaldoDocumento Saldo { get; set; }=new();
    [BsonElement("imagem")] public ImagemDocumento? Imagem { get; set; }
    [BsonElement("ativo")] public bool Ativo { get; set; }=true;
    [BsonElement("versao")] public long Versao { get; set; }=1;
    [BsonElement("guardVersion")] public long GuardVersion { get; set; }
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; }
}
