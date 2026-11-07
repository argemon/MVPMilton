using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Infrastructure.Clientes;
using GestaoComercial.Infrastructure.Funcionarios;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Produtos;

public sealed class ProdutosSchema(IMongoClient client,MongoConfiguration configuration,ClientesSchema previous)
{
    private IMongoDatabase Db=>client.GetDatabase(configuration.DatabaseName);
    public const string Id="04-products";
    private static BsonDocument Text(int min,int max)=>new(){{"bsonType","string"},{"minLength",min},{"maxLength",max}};
    private static BsonDocument Type(string type)=>new("bsonType",type);
    private static BsonDocument ReferenceProperties {
        get { var p=FuncionariosSchema.Properties; p.Remove("email");p.Remove("telefone"); p["nome"]=Text(2,80);p["nomeNormalizado"]=Text(2,80); return p; }
    }
    private static BsonDocument ProductValidator {
        get {
            var p=FuncionariosSchema.Properties; p.Remove("email");p.Remove("telefone");
            p.Add("sku",Text(1,40));p.Add("skuNormalizado",new BsonDocument{{"bsonType","string"},{"pattern","^[A-Z0-9._-]{1,40}$"}});
            p.Add("descricao",new BsonDocument{{"bsonType",new BsonArray{"string","null"}},{"maxLength",2000}});
            p.Add("categoriaId",Type("binData"));p.Add("marcaId",new BsonDocument("bsonType",new BsonArray{"binData","null"}));
            p.Add("precoCentavos",new BsonDocument{{"bsonType","long"},{"minimum",1L},{"maximum",999999999L}});
            p.Add("moeda",new BsonDocument("enum",new BsonArray{"BRL"}));
            p.Add("estoqueMinimo",new BsonDocument{{"bsonType","int"},{"minimum",0},{"maximum",1000000}});
            p.Add("saldo",new BsonDocument{{"bsonType","object"},{"additionalProperties",false},{"required",new BsonArray{"quantidade","versao"}},{"properties",new BsonDocument{{"quantidade",new BsonDocument{{"bsonType","int"},{"minimum",0}}},{"versao",new BsonDocument{{"bsonType","long"},{"minimum",1L}}}}}});
            p.Add("imagem",new BsonDocument{{"bsonType",new BsonArray{"object","null"}},{"additionalProperties",false},{"required",new BsonArray{"chave","mime","tamanhoBytes"}},{"properties",new BsonDocument{{"chave",new BsonDocument{{"bsonType","string"},{"pattern","^[a-f0-9]{32}\\.png$"}}},{"mime",new BsonDocument("enum",new BsonArray{"image/png"})},{"tamanhoBytes",new BsonDocument{{"bsonType","int"},{"minimum",1},{"maximum",2097152}}}}}});
            return FuncionariosSchema.Validator(p);
        }
    }
    private static string Checksum=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ProductValidator.ToJson()+FuncionariosSchema.Validator(ReferenceProperties).ToJson()+":referenceNameUnique:nameId:skuUnique:categoryActiveId:brandActiveId:v1")));
    public async Task<bool> IsReadyAsync(CancellationToken ct) {
        var r=await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id",Id)).FirstOrDefaultAsync(ct);
        return r is not null&&r.GetValue("checksum","")==Checksum&&r.GetValue("version",0)==5;
    }
    public async Task InitializeAsync(CancellationToken ct) {
        if(!await previous.IsReadyAsync(ct)) throw new InvalidOperationException("Evolução03 ausente.");
        foreach(var name in new[]{"categories","brands"}) {
            await FuncionariosSchema.EnsureCollectionAsync(Db,name,FuncionariosSchema.Validator(ReferenceProperties),ct);
            await Index(name,"ux_"+name+"_nome",new BsonDocument("nomeNormalizado",1),true,ct);
            await Index(name,"ix_"+name+"_nome_id",new BsonDocument{{"nomeNormalizado",1},{"_id",1}},false,ct);
        }
        await FuncionariosSchema.EnsureCollectionAsync(Db,"products",ProductValidator,ct);
        await Index("products","ux_products_sku",new BsonDocument("skuNormalizado",1),true,ct);
        await Index("products","ix_products_nome_id",new BsonDocument{{"nomeNormalizado",1},{"_id",1}},false,ct);
        await Index("products","ix_products_categoria_ativo_id",new BsonDocument{{"categoriaId",1},{"ativo",1},{"_id",1}},false,ct);
        await Index("products","ix_products_marca_ativo_id",new BsonDocument{{"marcaId",1},{"ativo",1},{"_id",1}},false,ct);
        await FuncionariosSchema.RecordAsync(Db,Id,5,Checksum,ct);
    }
    private Task<string> Index(string collection,string name,BsonDocument keys,bool unique,CancellationToken ct)=>Db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys,new CreateIndexOptions{Name=name,Unique=unique}),cancellationToken:ct);
}
