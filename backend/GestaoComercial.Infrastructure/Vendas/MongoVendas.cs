using System.Security.Cryptography;
using System.Text.Json;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Vendas;
using GestaoComercial.Domain.Estoque;
using GestaoComercial.Domain.Vendas;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
namespace GestaoComercial.Infrastructure.Vendas;

public sealed class MongoVendas(OperacoesCadastro operations,IMongoClient client,MongoEstoque stock,IConfiguration configuration,VendasSchema schema) : IVendas
{
    private static readonly string[] Roles=["Administrador","Vendedor"];
    private IMongoCollection<BsonDocument> Sales=>operations.Db.GetCollection<BsonDocument>("sales");
    private static BsonBinaryData U(Guid value)=>MongoEstoque.Uuid(value);
    private static byte[] Hash(object value)=>SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value));
    private static BsonValue Optional(string? value)=>value is null?BsonNull.Value:value;
    private static OperadorVenda Actor(BsonDocument d,string id,string name)=>new(d[id].AsGuid,d[name].AsString);
    private static string? CustomerName(BsonDocument d)=>d.TryGetValue("clienteSnapshot",out var snapshot)&&snapshot is BsonDocument doc&&doc.TryGetValue("nome",out var name)&&name.IsString?name.AsString:null;
    private static EventoVenda? Event(BsonDocument d,string field,string date)
    { if(d[field].IsBsonNull)return null;var e=d[field].AsBsonDocument;return new(Actor(e,"atorId","nomeSnapshot"),d[date].ToUniversalTime(),e["motivo"].IsBsonNull?null:e["motivo"].AsString); }
    internal static Venda Map(BsonDocument d)=>new(d["_id"].AsGuid,d["clienteId"].AsGuid,d["clienteSnapshot"].IsBsonNull?null:new(d["clienteSnapshot"]["nome"].AsString,d["clienteSnapshot"]["documento"].IsBsonNull?null:d["clienteSnapshot"]["documento"].AsString),Actor(d,"criadorId","criadorNomeSnapshot"),d["status"].AsString,d["itens"].AsBsonArray.Select(v=>{var i=v.AsBsonDocument;return new ItemVenda(i["id"].AsGuid,i["produtoId"].AsGuid,i["skuSnapshot"].AsString,i["nomeSnapshot"].AsString,i["quantidade"].AsInt32,i["precoUnitarioCentavos"].AsInt64,i["totalCentavos"].AsInt64);}).ToArray(),d["totalCentavos"].AsInt64,d["versao"].AsInt64,d["criadoEm"].ToUniversalTime(),Event(d,"confirmacao","confirmadoEm"),Event(d,"cancelamento","canceladoEm"),Event(d,"descarte","descartadoEm"));
    private static void Own(SessaoAtual actor,BsonDocument sale){if(actor.Usuario.Perfil!="Administrador"&&sale["criadorId"].AsGuid!=actor.Usuario.Id)throw new CadastroException("VENDA_NAO_ENCONTRADA",404);}
    public async Task<PaginaVendas> ListarAsync(SessaoAtual ator,Guid? clienteId,string? status,DateOnly? inicio,DateOnly? fim,int page,int pageSize,CancellationToken ct)
    {
        await operations.ExigirAsync(ator,Roles,ct);OperacoesCadastro.Pagina(page,pageSize);
        if(clienteId==Guid.Empty||status is not(null or "Rascunho" or "Confirmada" or "Cancelada" or "Descartada"))throw new CadastroException("VALIDACAO");
        var period=MongoEstoque.Periodo(inicio,fim,configuration["APP_TIME_ZONE"]??"America/Cuiaba");var f=Builders<BsonDocument>.Filter.Empty;
        if(ator.Usuario.Perfil!="Administrador")f &= new BsonDocument("criadorId",U(ator.Usuario.Id));
        if(clienteId is {} customer)f &= new BsonDocument("clienteId",U(customer));if(status is not null)f &= new BsonDocument("status",status);
        if(period.Inicio is {} start)f &= Builders<BsonDocument>.Filter.Gte("criadoEm",start)&Builders<BsonDocument>.Filter.Lt("criadoEm",period.Fim!.Value);
        var total=await Sales.CountDocumentsAsync(f,cancellationToken:ct);
        var projection=new BsonDocument{{"_id",1},{"clienteId",1},{"clienteSnapshot.nome",1},{"criadorId",1},{"criadorNomeSnapshot",1},{"status",1},{"totalCentavos",1},{"versao",1},{"criadoEm",1}};
        var rows=await Sales.Find(f).Sort(new BsonDocument{{"criadoEm",-1},{"_id",-1}}).Skip((page-1)*pageSize).Limit(pageSize).Project<BsonDocument>(projection).ToListAsync(ct);
        return new(rows.Select(d=>new VendaResumo(d["_id"].AsGuid,d["clienteId"].AsGuid,CustomerName(d),Actor(d,"criadorId","criadorNomeSnapshot"),d["status"].AsString,d["totalCentavos"].AsInt64,d["versao"].AsInt64,d["criadoEm"].ToUniversalTime())).ToArray(),page,pageSize,total);
    }
    public async Task<Venda> ConsultarAsync(SessaoAtual ator,Guid id,CancellationToken ct)
    { await operations.ExigirAsync(ator,Roles,ct);var d=await Sales.Find(new BsonDocument("_id",U(id))).SingleOrDefaultAsync(ct)??throw new CadastroException("VENDA_NAO_ENCONTRADA",404);Own(ator,d);return Map(d); }
    private async Task<BsonDocument> GuardSale(IClientSessionHandle s,SessaoAtual actor,Guid id,long version,CancellationToken ct)
    {
        var filter=new BsonDocument("_id",U(id));var sale=await Sales.Find(s,filter).SingleOrDefaultAsync(ct)??throw new CadastroException("VENDA_NAO_ENCONTRADA",404);Own(actor,sale);
        if(sale["versao"].AsInt64!=version)throw new CadastroException("VERSAO_DESATUALIZADA",409);
        filter.Add("versao",version);filter.Add("status",sale["status"]);
        var result=await Sales.UpdateOneAsync(s,filter,new BsonDocument("$inc",new BsonDocument("guardVersion",1L)),cancellationToken:ct);
        if(result.ModifiedCount!=1)throw new CadastroException("VERSAO_DESATUALIZADA",409);sale["guardVersion"]=checked(sale["guardVersion"].AsInt64+1);return sale;
    }
    private async Task<BsonDocument> Guard(IClientSessionHandle s,string collection,Guid id,bool active,CancellationToken ct)
    {
        if(collection=="products"){var current=await operations.Db.GetCollection<BsonDocument>(collection).Find(s,new BsonDocument("_id",U(id))).SingleOrDefaultAsync(ct)??throw new CadastroException("PRODUTO_NAO_ENCONTRADO",404);stock.LerSaldo(current);}
        var d=await operations.Db.GetCollection<BsonDocument>(collection).FindOneAndUpdateAsync(s,new BsonDocument("_id",U(id)),new BsonDocument("$inc",new BsonDocument("guardVersion",1L)),new FindOneAndUpdateOptions<BsonDocument>{ReturnDocument=ReturnDocument.After},ct)??throw new CadastroException(collection=="customers"?"CLIENTE_NAO_ENCONTRADO":"PRODUTO_NAO_ENCONTRADO",404);
        if(active&&!d["ativo"].AsBoolean)throw new CadastroException("RECURSO_INATIVO",409);return d;
    }
    private static void Validate(Guid customer,IReadOnlyList<ItemComando>? items,long? version)
    {
        if(customer==Guid.Empty||items is null||version is <1)throw new CadastroException("VALIDACAO");
        try{RegrasVenda.Itens(items.Select(x=>(x.ProdutoId,x.Quantidade)));}catch(ArgumentException){throw new CadastroException("VALIDACAO");}
    }
    private async Task<BsonDocument> Draft(IClientSessionHandle s,SessaoAtual actor,Guid id,Guid customer,IReadOnlyList<ItemComando> items,BsonDocument? previous,CancellationToken ct)
    {
        await Guard(s,"customers",customer,true,ct);var array=new BsonArray();var old=previous?["itens"].AsBsonArray.ToDictionary(x=>x["produtoId"].AsGuid,x=>x["id"].AsGuid);
        foreach(var item in items.OrderBy(x=>x.ProdutoId))
        {
            var p=await Guard(s,"products",item.ProdutoId,true,ct);long subtotal;
            try{subtotal=RegrasVenda.Subtotal(p["precoCentavos"].AsInt64,item.Quantidade);}catch(ArgumentException e){throw new CadastroException(e.Message);}
            array.Add(new BsonDocument{{"id",U(old is not null&&old.TryGetValue(item.ProdutoId,out var stable)?stable:Guid.NewGuid())},{"produtoId",U(item.ProdutoId)},{"skuSnapshot",p["sku"]},{"nomeSnapshot",p["nome"]},{"quantidade",item.Quantidade},{"precoUnitarioCentavos",p["precoCentavos"]},{"totalCentavos",subtotal}});
        }
        long total;try{total=RegrasVenda.Total(array.Select(x=>x["totalCentavos"].AsInt64));}catch(ArgumentException e){throw new CadastroException(e.Message);}
        var sale=previous??new BsonDocument{{"_id",U(id)},{"schemaVersion",1},{"criadorId",U(actor.Usuario.Id)},{"criadorNomeSnapshot",actor.Usuario.Nome},{"status","Rascunho"},{"versao",0L},{"guardVersion",0L},{"createdAt",operations.Now},{"criadoEm",operations.Now},{"confirmadoEm",BsonNull.Value},{"canceladoEm",BsonNull.Value},{"descartadoEm",BsonNull.Value},{"clienteSnapshot",BsonNull.Value},{"confirmacao",BsonNull.Value},{"cancelamento",BsonNull.Value},{"descarte",BsonNull.Value},{"moeda","BRL"}};
        // createdAt and criadoEm must be the exact same instant, including on retry.
        sale["criadoEm"]=sale["createdAt"];sale["clienteId"]=U(customer);sale["itens"]=array;sale["totalCentavos"]=total;sale["versao"]=checked(sale["versao"].AsInt64+1);sale["updatedAt"]=operations.Now;
        if(sale.ToBson().Length>1048576)throw new CadastroException("VALIDACAO");
        if(previous is null)await Sales.InsertOneAsync(s,sale,cancellationToken:ct);else await Replace(s,sale,ct);
        await operations.AuditarAsync(s,actor,previous is null?"VendaCriada":"VendaAlterada","Venda",id,ct);return sale;
    }
    private async Task Replace(IClientSessionHandle s,BsonDocument sale,CancellationToken ct)
    {
        var result=await Sales.ReplaceOneAsync(s,new BsonDocument{{"_id",sale["_id"]},{"versao",sale["versao"].AsInt64-1}},sale,cancellationToken:ct);if(result.ModifiedCount!=1)throw new CadastroException("VERSAO_DESATUALIZADA",409);
    }
    public async Task<Venda> SalvarAsync(SessaoAtual ator,Guid? id,Guid chave,Guid clienteId,IReadOnlyList<ItemComando>? itens,long versao,CancellationToken ct)
    {
        await operations.ExigirAsync(ator,Roles,ct);if(!await schema.IsReadyAsync(ct))throw new CadastroException("PERSISTENCIA_NAO_INICIALIZADA",503);Validate(clienteId,itens,id is null?null:versao);
        if(id is {} existing)return Map(await operations.ExecutarAsync(ator,Roles,async(s,token)=>{var sale=await GuardSale(s,ator,existing,versao,token);if(sale["status"]!="Rascunho")throw new CadastroException("ESTADO_INVALIDO",409);return await Draft(s,ator,existing,clienteId,itens!,sale,token);},ct));
        var newId=Guid.NewGuid();var hash=Hash(new{clienteId,itens=itens!.OrderBy(x=>x.ProdutoId)});
        return Map(await new OperacoesComerciais(operations,client).ExecutarAsync(ator,Roles,chave,"vendas/criar/v1",hash,201,newId,(s,t)=>Draft(s,ator,newId,clienteId,itens!,null,t),ct));
    }
    public async Task<Venda> TransicionarAsync(SessaoAtual ator,Guid id,Guid chave,string acao,long versaoEsperada,string? motivo,CancellationToken ct)
    {
        var roles=acao=="cancelamento"?new[]{"Administrador"}:Roles;await operations.ExigirAsync(ator,roles,ct);if(!await schema.IsReadyAsync(ct))throw new CadastroException("PERSISTENCIA_NAO_INICIALIZADA",503);
        if(id==Guid.Empty||versaoEsperada<1||acao is not("confirmacao" or "cancelamento" or "descarte"))throw new CadastroException("VALIDACAO");
        string? reason=null;if(acao!="confirmacao")try{reason=Movimentacao.Motivo(motivo);}catch(ArgumentException){throw new CadastroException("VALIDACAO");}
        // Enforce ownership before returning even an older receipt.
        await ConsultarAsync(ator,id,ct);
        return Map(await new OperacoesComerciais(operations,client).ExecutarAsync(ator,roles,chave,"vendas/"+acao+"/v1",Hash(new{id,versaoEsperada,motivo=reason}),200,id,async(s,token)=>
        {
            var sale=await GuardSale(s,ator,id,versaoEsperada,token);string state;
            try{state=RegrasVenda.Transicao(sale["status"].AsString,acao,sale["itens"].AsBsonArray.Count);}catch(InvalidOperationException e){throw new CadastroException(e.Message,409);}
            if(acao!="descarte")
            {
                var customer=await Guard(s,"customers",sale["clienteId"].AsGuid,acao=="confirmacao",token);
                if(acao=="confirmacao")sale["clienteSnapshot"]=new BsonDocument{{"nome",customer["nome"]},{"documento",customer["documento"]}};
                foreach(var item in sale["itens"].AsBsonArray.Select(x=>x.AsBsonDocument).OrderBy(x=>x["produtoId"].AsGuid))
                {
                    var product=await Guard(s,"products",item["produtoId"].AsGuid,acao=="confirmacao",token);
                    if(acao=="confirmacao"&&product["precoCentavos"]!=item["precoUnitarioCentavos"])throw new CadastroException("PRECO_ALTERADO",409);
                    await stock.AplicarAsync(s,ator,item["produtoId"].AsGuid,acao=="confirmacao"?"Venda":"EstornoVenda",acao=="confirmacao"?-item["quantidade"].AsInt32:item["quantidade"].AsInt32,reason??"Confirmação de venda",Guid.NewGuid(),Guid.Empty,token,id,item["id"].AsGuid);
                }
            }
            sale["status"]=state;sale["versao"]=checked(sale["versao"].AsInt64+1);sale["updatedAt"]=operations.Now;
            sale[acao]=new BsonDocument{{"atorId",U(ator.Usuario.Id)},{"nomeSnapshot",ator.Usuario.Nome},{"motivo",Optional(reason)}};
            sale[acao=="confirmacao"?"confirmadoEm":acao=="cancelamento"?"canceladoEm":"descartadoEm"]=operations.Now;
            await Replace(s,sale,token);await operations.AuditarAsync(s,ator,state,"Venda",id,token);return sale;
        },ct));
    }
}
