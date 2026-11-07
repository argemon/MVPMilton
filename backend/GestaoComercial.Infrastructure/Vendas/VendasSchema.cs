using System.Security.Cryptography;
using System.Text;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Funcionarios;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Vendas;

public sealed class VendasSchema(IMongoClient client,MongoConfiguration configuration,EstoqueSchema previous)
{
    private IMongoDatabase Db=>client.GetDatabase(configuration.DatabaseName);
    public const string Id="06-sales";
    private static BsonDocument T(string type)=>new("bsonType",type);
    private static BsonDocument N(string type)=>new("bsonType",new BsonArray{type,"null"});
    private static BsonDocument Text(int min,int max)=>new(){{"bsonType","string"},{"minLength",min},{"maxLength",max}};
    private static BsonDocument Number(string type,long min,long max)=>new(){{"bsonType",type},{"minimum",min},{"maximum",max}};
    private static BsonDocument Object(BsonDocument fields,bool nullable=false)=>new(){{"bsonType",nullable?new BsonArray{"object","null"}:(BsonValue)"object"},{"additionalProperties",false},{"required",new BsonArray(fields.Names)},{"properties",fields}};
    private static BsonDocument Event=>Object(new BsonDocument{{"atorId",T("binData")},{"nomeSnapshot",Text(2,150)},{"motivo",new BsonDocument{{"bsonType",new BsonArray{"string","null"}},{"minLength",10},{"maxLength",500}}}},true);
    private static BsonDocument SalesValidator
    {
        get
        {
            var item=Object(new BsonDocument{{"id",T("binData")},{"produtoId",T("binData")},{"skuSnapshot",Text(1,40)},{"nomeSnapshot",Text(2,150)},{"quantidade",Number("int",1,1000000)},{"precoUnitarioCentavos",Number("long",1,999999999)},{"totalCentavos",Number("long",1,99999999999999)}});
            var schema=Object(new BsonDocument{
                {"_id",T("binData")},{"schemaVersion",Number("int",1,1)},{"clienteId",T("binData")},{"criadorId",T("binData")},{"criadorNomeSnapshot",Text(2,150)},
                {"status",new BsonDocument("enum",new BsonArray{"Rascunho","Confirmada","Cancelada","Descartada"})},{"itens",new BsonDocument{{"bsonType","array"},{"maxItems",100},{"items",item}}},
                {"totalCentavos",Number("long",0,99999999999999)},{"moeda",new BsonDocument("enum",new BsonArray{"BRL"})},{"versao",Number("long",1,long.MaxValue)},{"guardVersion",Number("long",0,long.MaxValue)},
                {"createdAt",T("date")},{"updatedAt",T("date")},{"criadoEm",T("date")},{"confirmadoEm",N("date")},{"canceladoEm",N("date")},{"descartadoEm",N("date")},
                {"clienteSnapshot",Object(new BsonDocument{{"nome",Text(2,150)},{"documento",new BsonDocument{{"bsonType",new BsonArray{"string","null"}},{"maxLength",14}}}},true)},
                {"confirmacao",Event},{"cancelamento",Event},{"descarte",Event}
            });
            // With100 bounded items the per-document Int64 sum cannot overflow.
            return new("$and",new BsonArray{new BsonDocument("$jsonSchema",schema),BsonDocument.Parse("""
            {$expr:{$and:[
              {$eq:['$createdAt','$criadoEm']},{$eq:[{$size:'$itens'},{$size:{$setUnion:['$itens.produtoId',[]]}}]},{$eq:[{$size:'$itens'},{$size:{$setUnion:['$itens.id',[]]}}]},
              {$eq:['$totalCentavos',{$sum:'$itens.totalCentavos'}]},{$allElementsTrue:[{$map:{input:'$itens',as:'i',in:{$eq:['$$i.totalCentavos',{$multiply:['$$i.precoUnitarioCentavos','$$i.quantidade']}]}}}]},
              {$or:[
                {$and:[{$eq:['$status','Rascunho']},{$eq:['$clienteSnapshot',null]},{$eq:['$confirmacao',null]},{$eq:['$cancelamento',null]},{$eq:['$descarte',null]},{$eq:['$confirmadoEm',null]},{$eq:['$canceladoEm',null]},{$eq:['$descartadoEm',null]}]},
                {$and:[{$eq:['$status','Descartada']},{$eq:['$clienteSnapshot',null]},{$eq:['$confirmacao',null]},{$eq:['$cancelamento',null]},{$eq:['$confirmadoEm',null]},{$eq:['$canceladoEm',null]},{$eq:[{$type:'$descartadoEm'},'date']},{$eq:[{$type:'$descarte'},'object']},{$eq:[{$type:'$descarte.motivo'},'string']}]},
                {$and:[{$in:['$status',['Confirmada','Cancelada']]},{$gt:[{$size:'$itens'},0]},{$eq:[{$type:'$clienteSnapshot'},'object']},{$eq:[{$type:'$confirmacao'},'object']},{$eq:[{$type:'$confirmadoEm'},'date']},{$eq:['$confirmacao.motivo',null]},{$eq:['$descarte',null]},{$eq:['$descartadoEm',null]},{$or:[
                  {$and:[{$eq:['$status','Confirmada']},{$eq:['$cancelamento',null]},{$eq:['$canceladoEm',null]}]},
                  {$and:[{$eq:['$status','Cancelada']},{$eq:[{$type:'$canceladoEm'},'date']},{$eq:[{$type:'$cancelamento'},'object']},{$eq:[{$type:'$cancelamento.motivo'},'string']}]}
                ]}]}
              ]}
            ]}}
            """)});
        }
    }
    internal static BsonDocument MovementValidator
    {
        get
        {
            var v=EstoqueSchema.MovementValidator.DeepClone().AsBsonDocument;var s=v["$and"][0]["$jsonSchema"].AsBsonDocument;s["required"].AsBsonArray.Remove("operacaoId");var p=s["properties"].AsBsonDocument;
            p["operacaoId"]=N("binData");p["vendaId"]=N("binData");p["itemId"]=N("binData");p["tipo"]=new BsonDocument("enum",new BsonArray{"Entrada","SaidaManual","Ajuste","Venda","EstornoVenda"});
            v["$and"][1]=BsonDocument.Parse("""
            {"$expr":{"$and":[{"$ne":["$delta",0]},{"$eq":["$saldoDepois",{"$add":["$saldoAntes","$delta"]}]},{"$or":[{"$and":[{"$in":["$tipo",["Entrada","SaidaManual","Ajuste"]]},{"$eq":[{"$type":"$operacaoId"},"binData"]},{"$in":[{"$type":"$vendaId"},["missing","null"]]},{"$in":[{"$type":"$itemId"},["missing","null"]]},{"$or":[{"$eq":["$tipo","Ajuste"]},{"$and":[{"$eq":["$tipo","Entrada"]},{"$gt":["$delta",0]}]},{"$and":[{"$eq":["$tipo","SaidaManual"]},{"$lt":["$delta",0]}]}]}]},{"$and":[{"$in":["$tipo",["Venda","EstornoVenda"]]},{"$eq":[{"$type":"$vendaId"},"binData"]},{"$eq":[{"$type":"$itemId"},"binData"]},{"$in":[{"$type":"$operacaoId"},["missing","null"]]},{"$or":[{"$and":[{"$eq":["$tipo","Venda"]},{"$lt":["$delta",0]}]},{"$and":[{"$eq":["$tipo","EstornoVenda"]},{"$gt":["$delta",0]}]}]}]}]}]}}
            """);return v;
        }
    }
    internal static BsonDocument ReceiptValidator
    {
        get
        {
            var v=EstoqueSchema.ReceiptValidator.DeepClone().AsBsonDocument;v["$and"][0]["$jsonSchema"]["properties"]["statusHttp"]=new BsonDocument("enum",new BsonArray{200,201});
            v["$and"][1]=BsonDocument.Parse("{$expr:{$and:[{$eq:[{$binarySize:'$hashRequest'},32]},{$or:[{$eq:['$estado','Preparando']},{$and:[{$eq:['$estado','Concluido']},{$eq:['$recursoId','$resposta._id']}]}]}]}}");return v;
        }
    }
    private static string Checksum=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SalesValidator.ToJson()+MovementValidator.ToJson()+ReceiptValidator.ToJson()+":salesDateId:customerDateId:creatorDateId:confirmationStatusId:commercialUnique:v1")));
    public async Task<bool> IsReadyAsync(CancellationToken ct){var r=await Db.GetCollection<BsonDocument>("persistence_versions").Find(new BsonDocument("_id",Id)).FirstOrDefaultAsync(ct);return r is not null&&r.GetValue("checksum","")==Checksum&&r.GetValue("version",0)==7;}
    internal static async Task EnsureCompatibleAsync(IMongoDatabase db,string name,BsonDocument baseline,BsonDocument expanded,CancellationToken ct)
    {
        using var rows=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name",name)},ct);var existing=await rows.FirstOrDefaultAsync(ct);
        if(existing is null){await FuncionariosSchema.EnsureCollectionAsync(db,name,baseline,ct);return;}
        var options=existing["options"].AsBsonDocument;var validator=options.GetValue("validator",new BsonDocument());
        if((validator!=baseline&&validator!=expanded)||options.GetValue("validationLevel","")!="strict"||options.GetValue("validationAction","")!="error")throw new InvalidOperationException("Evolução divergente; revisão necessária.");
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if(!await previous.IsReadyAsync(ct))throw new InvalidOperationException("Evolução05 ausente.");
        await Expand("stock_movements",EstoqueSchema.MovementValidator,MovementValidator,ct);await Expand("idempotency_receipts",EstoqueSchema.ReceiptValidator,ReceiptValidator,ct);
        await FuncionariosSchema.EnsureCollectionAsync(Db,"sales",SalesValidator,ct);
        await Index("sales","ix_sales_criado_id",new BsonDocument{{"criadoEm",-1},{"_id",-1}},false,null,ct);
        await Index("sales","ix_sales_cliente_criado_id",new BsonDocument{{"clienteId",1},{"criadoEm",-1},{"_id",-1}},false,null,ct);
        await Index("sales","ix_sales_criador_criado_id",new BsonDocument{{"criadorId",1},{"criadoEm",-1},{"_id",-1}},false,null,ct);
        await Index("sales","ix_sales_confirmado_status_id",new BsonDocument{{"confirmadoEm",1},{"status",1},{"_id",1}},false,null,ct);
        await Index("stock_movements","ux_stock_venda_item_tipo",new BsonDocument{{"vendaId",1},{"itemId",1},{"tipo",1}},true,new BsonDocument{{"vendaId",new BsonDocument("$type","binData")},{"itemId",new BsonDocument("$type","binData")}},ct);
        await FuncionariosSchema.RecordAsync(Db,Id,7,Checksum,ct);
    }
    private async Task Expand(string name,BsonDocument baseline,BsonDocument expanded,CancellationToken ct){await EnsureCompatibleAsync(Db,name,baseline,expanded,ct);await Db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod",name},{"validator",expanded},{"validationLevel","strict"},{"validationAction","error"}},cancellationToken:ct);}
    private Task<string> Index(string collection,string name,BsonDocument keys,bool unique,BsonDocument? partial,CancellationToken ct)=>Db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys,new CreateIndexOptions<BsonDocument>{Name=name,Unique=unique,PartialFilterExpression=partial}),cancellationToken:ct);
}
