using System.Globalization;
using System.Numerics;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Dashboard;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Dashboard;

public sealed class MongoDashboard(OperacoesCadastro operations,IConfiguration config) : IDashboard
{
    private string Fuso=>config["APP_TIME_ZONE"]??"America/Cuiaba";
    internal static BsonDocument D(string json)=>BsonDocument.Parse(json);
    internal static BsonDocument Match(DateTime inicio,DateTime fim)=>new("$match",new BsonDocument{{"confirmadoEm",new BsonDocument{{"$gte",inicio},{"$lt",fim}}},{"status",new BsonDocument("$in",new BsonArray{"Confirmada","Cancelada"})}});
    internal static long Exact(BsonValue value)
    {
        var text=value.IsDecimal128?value.AsDecimal128.ToString():value.ToString()!;
        if(!BigInteger.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)||n<long.MinValue||n>long.MaxValue)throw new CadastroException("LIMITE_MONETARIO_EXCEDIDO");
        return (long)n;
    }
    internal static BsonDocument ResumoStage=>D("""
    {$group:{_id:null,confirmadas:{$sum:{$cond:[{$eq:['$status','Confirmada']},NumberLong(1),NumberLong(0)]}},canceladas:{$sum:{$cond:[{$eq:['$status','Cancelada']},NumberLong(1),NumberLong(0)]}},receita:{$sum:{$cond:[{$eq:['$status','Confirmada']},{$toDecimal:'$totalCentavos'},{$toDecimal:0}]}}}}
    """);
    internal static ResumoComercial Summary(BsonDocument? d)
    {var a=d is null?0:Exact(d["confirmadas"]);var b=d is null?0:Exact(d["canceladas"]);var total=d is null?0:Exact(d["receita"]);return new(a,b,total,Indicadores.Ticket(total,a));}
    internal static BsonDocument VendaProjection=>D("{$project:{_id:1,confirmadoEm:1,'clienteSnapshot.nome':1,criadorNomeSnapshot:1,status:1,totalCentavos:1}}");
    internal static VendaRecente Recent(BsonDocument d)=>new(d["_id"].AsGuid,d["confirmadoEm"].ToUniversalTime(),d["clienteSnapshot"]["nome"].AsString,d["criadorNomeSnapshot"].AsString,d["status"].AsString,d["totalCentavos"].AsInt64,d["status"]=="Confirmada"?d["totalCentavos"].AsInt64:0);
    internal static BsonDocument[] RankingStages=>[
        D("{$match:{status:'Confirmada'}}"),D("{$sort:{confirmadoEm:-1,_id:-1}}"),D("{$unwind:'$itens'}"),
        D("{$group:{_id:'$itens.produtoId',sku:{$first:'$itens.skuSnapshot'},nome:{$first:'$itens.nomeSnapshot'},quantidade:{$sum:{$toDecimal:'$itens.quantidade'}},receita:{$sum:{$toDecimal:'$itens.totalCentavos'}}}}"),
        D("{$sort:{quantidade:-1,receita:-1,_id:1}}")];
    internal static ProdutoVendido Product(BsonDocument d)=>new(d["_id"].AsGuid,d["sku"].AsString,d["nome"].AsString,Exact(d["quantidade"]),Exact(d["receita"]));
    internal static async Task<List<BsonDocument>> Aggregate(IMongoDatabase db,IClientSessionHandle s,string collection,IEnumerable<BsonDocument> pipeline,CancellationToken ct)
        =>await db.GetCollection<BsonDocument>(collection).Aggregate<BsonDocument>(s,pipeline.ToArray(),new AggregateOptions{MaxTime=TimeSpan.FromSeconds(8),AllowDiskUse=true}).ToListAsync(ct);
    public async Task<Painel> ConsultarAsync(SessaoAtual ator,DateOnly? inicio,DateOnly? fim,CancellationToken ct)
    {
        var period=Indicadores.Periodo(inicio,fim,Fuso,operations.Now);
        return await operations.ExecutarAsync(ator,["Administrador"],async(s,token)=>
        {
            var db=operations.Db;var match=Match(period.InicioUtc,period.FimUtc);
            var summary=Summary((await Aggregate(db,s,"sales",[match,ResumoStage],token)).SingleOrDefault());
            var recent=await Aggregate(db,s,"sales",[match,D("{$sort:{confirmadoEm:-1,_id:-1}}"),D("{$limit:10}"),VendaProjection],token);
            var ranked=await Aggregate(db,s,"sales",new[]{match}.Concat(RankingStages).Append(D("{$limit:10}")),token);
            var daily=await Aggregate(db,s,"sales",[match,D("{$match:{status:'Confirmada'}}"),new BsonDocument("$group",new BsonDocument{{"_id",new BsonDocument("$dateToString",new BsonDocument{{"format","%Y-%m-%d"},{"date","$confirmadoEm"},{"timezone",Fuso}})},{"quantidade",new BsonDocument("$sum",1L)},{"receita",new BsonDocument("$sum",new BsonDocument("$toDecimal","$totalCentavos"))}})],token);
            var byDay=daily.ToDictionary(x=>DateOnly.ParseExact(x["_id"].AsString,"yyyy-MM-dd",CultureInfo.InvariantCulture));var evolution=new List<EvolucaoVenda>();
            for(var day=period.Inicio;day<period.Fim;day=day.AddDays(1))evolution.Add(byDay.TryGetValue(day,out var d)?new(day,Exact(d["quantidade"]),Exact(d["receita"])):new(day,0,0));
            var products=await Aggregate(db,s,"products",[D("{$group:{_id:null,total:{$sum:NumberLong(1)},ativos:{$sum:{$cond:['$ativo',NumberLong(1),NumberLong(0)]}}}}")],token);var count=products.SingleOrDefault();
            var lowMatch=D("{$match:{ativo:true,$expr:{$lte:['$saldo.quantidade','$estoqueMinimo']}}}");
            var lowTotal=await Aggregate(db,s,"products",[lowMatch,D("{$count:'total'}")],token);
            var low=await Aggregate(db,s,"products",[lowMatch,D("{$sort:{'saldo.quantidade':1,nomeNormalizado:1,_id:1}}"),D("{$limit:20}"),D("{$project:{sku:1,nome:1,saldo:1,estoqueMinimo:1}}")],token);
            return new Painel(period.Inicio,period.Fim,Fuso,operations.Now,summary,count is null?0:Exact(count["total"]),count is null?0:Exact(count["ativos"]),lowTotal.Count==0?0:Exact(lowTotal[0]["total"]),low.Select(x=>new ProdutoBaixo(x["_id"].AsGuid,x["sku"].AsString,x["nome"].AsString,x["saldo"]["quantidade"].AsInt32,x["estoqueMinimo"].AsInt32)).ToArray(),recent.Select(Recent).ToArray(),evolution,ranked.Select(Product).ToArray());
        },ct);
    }
}
