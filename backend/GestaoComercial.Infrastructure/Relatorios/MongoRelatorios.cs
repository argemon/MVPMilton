using System.Text.RegularExpressions;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Dashboard;
using GestaoComercial.Application.Estoque;
using GestaoComercial.Application.Relatorios;
using GestaoComercial.Infrastructure.Dashboard;
using GestaoComercial.Infrastructure.Estoque;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using static GestaoComercial.Infrastructure.Dashboard.MongoDashboard;
namespace GestaoComercial.Infrastructure.Relatorios;

public sealed class MongoRelatorios(OperacoesCadastro operations,IConfiguration config) : IRelatorios
{
    private string Fuso=>config["APP_TIME_ZONE"]??"America/Cuiaba";
    private static readonly string[] Commercial=["Administrador"],Stock=["Administrador","Estoquista"];
    private MetadadosRelatorio Validate(ConsultaRelatorio f,string tipo)
    {
        OperacoesCadastro.Pagina(f.Page,f.PageSize,f.Search);
        if(f.OperadorId==Guid.Empty||f.ProdutoId==Guid.Empty)throw new CadastroException("VALIDACAO");
        var sort=f.Ordenacao??(tipo=="estoque"?"nomeAsc":tipo=="operadores"?"receitaDesc":tipo=="produtos"?"quantidadeDesc":"dataDesc");
        string[] allowed=tipo switch{"estoque"=>["nomeAsc","nomeDesc","saldoAsc","saldoDesc"],"operadores"=>["receitaDesc","nomeAsc"],"produtos"=>["quantidadeDesc","receitaDesc","nomeAsc"],_=>["dataDesc","dataAsc"]};
        if(!allowed.Contains(sort))throw new CadastroException("VALIDACAO");
        if(tipo=="estoque")
        {
            if(f.Inicio is not null||f.Fim is not null||f.OperadorId is not null||f.ProdutoId is not null||f.Tipo is not null)throw new CadastroException("VALIDACAO");
            return new(null,null,Fuso,operations.Now,sort,null,null,null,f.Search?.Trim(),f.Ativo);
        }
        var p=Indicadores.Periodo(f.Inicio,f.Fim,Fuso,operations.Now,false);
        if(tipo=="movimentos")
        {if(f.OperadorId is not null||f.Search is not null||f.Tipo is not(null or "Entrada" or "SaidaManual" or "Ajuste" or "Venda" or "EstornoVenda"))throw new CadastroException("VALIDACAO");}
        else if(f.ProdutoId is not null||f.Tipo is not null||f.Search is not null)throw new CadastroException("VALIDACAO");
        return new(p.Inicio,p.Fim,Fuso,operations.Now,sort,f.OperadorId,f.ProdutoId,f.Tipo,null,null);
    }
    private BsonDocument SaleMatch(ConsultaRelatorio f)
    {
        var p=Indicadores.Periodo(f.Inicio,f.Fim,Fuso,operations.Now,false);var match=Match(p.InicioUtc,p.FimUtc);
        if(f.OperadorId is {} id)match["$match"]["criadorId"]=MongoEstoque.Uuid(id);return match;
    }
    private async Task<PaginaRelatorio<T>> Page<T>(IClientSessionHandle s,string collection,IEnumerable<BsonDocument> pipeline,ConsultaRelatorio f,MetadadosRelatorio meta,Func<BsonDocument,T> map,CancellationToken ct)
    {
        var stages=pipeline.ToArray();var count=await Aggregate(operations.Db,s,collection,stages.Append(D("{$count:'total'}")),ct);var total=count.Count==0?0:Exact(count[0]["total"]);
        if(f.Exportar&&total>10000)throw new CadastroException("EXPORTACAO_EXCEDE_LIMITE");
        // Separate cursors avoid putting up to10k projected rows inside a16MiB facet document.
        var rows=await Aggregate(operations.Db,s,collection,stages.Concat(new[]{new BsonDocument("$skip",f.Exportar?0:(f.Page-1)*f.PageSize),new BsonDocument("$limit",f.Exportar?10000:f.PageSize)}),ct);
        return new(rows.Select(map).ToArray(),f.Exportar?1:f.Page,f.Exportar?10000:f.PageSize,total,meta with{GeradoEm=operations.Now});
    }
    private async Task<ResumoComercial> Totals(IClientSessionHandle s,ConsultaRelatorio f,CancellationToken ct)=>Summary((await Aggregate(operations.Db,s,"sales",[SaleMatch(f),ResumoStage],ct)).SingleOrDefault());
    public Task<RelatorioVendas> VendasAsync(SessaoAtual ator,ConsultaRelatorio f,CancellationToken ct)
    {
        var meta=Validate(f,"vendas");return operations.ExecutarAsync(ator,Commercial,async(s,t)=>
        {
            var total=await Totals(s,f,t);var direction=meta.Ordenacao=="dataAsc"?1:-1;
            var rows=await Page(s,"sales",[SaleMatch(f),new BsonDocument("$sort",new BsonDocument{{"confirmadoEm",direction},{"_id",direction}}),VendaProjection],f,meta,Recent,t);
            return new RelatorioVendas(rows,total);
        },ct);
    }
    public Task<RelatorioOperadores> OperadoresAsync(SessaoAtual ator,ConsultaRelatorio f,CancellationToken ct)
    {
        var meta=Validate(f,"operadores");return operations.ExecutarAsync(ator,Commercial,async(s,t)=>
        {
            var total=await Totals(s,f,t);var group=ResumoStage.DeepClone().AsBsonDocument;group["$group"]["_id"]="$criadorId";group["$group"]["nome"]=new BsonDocument("$first","$criadorNomeSnapshot");
            var sort=meta.Ordenacao=="nomeAsc"?D("{$sort:{nome:1,_id:1}}"):D("{$sort:{receita:-1,_id:1}}");
            var rows=await Page(s,"sales",[SaleMatch(f),D("{$sort:{confirmadoEm:-1,_id:-1}}"),group,sort],f,meta,d=>new OperadorRelatorio(d["_id"].AsGuid,d["nome"].AsString,Summary(d)),t);return new RelatorioOperadores(rows,total);
        },ct);
    }
    public Task<RelatorioProdutos> ProdutosAsync(SessaoAtual ator,ConsultaRelatorio f,CancellationToken ct)
    {
        var meta=Validate(f,"produtos");return operations.ExecutarAsync(ator,Commercial,async(s,t)=>
        {
            var total=await Totals(s,f,t);var sort=meta.Ordenacao switch{"nomeAsc"=>D("{$sort:{nome:1,_id:1}}"),"receitaDesc"=>D("{$sort:{receita:-1,_id:1}}"),_=>D("{$sort:{quantidade:-1,receita:-1,_id:1}}")};
            var rows=await Page(s,"sales",new[]{SaleMatch(f)}.Concat(RankingStages.SkipLast(1)).Append(sort),f,meta,Product,t);return new RelatorioProdutos(rows,total);
        },ct);
    }
    public Task<RelatorioEstoque> EstoqueAsync(SessaoAtual ator,ConsultaRelatorio f,CancellationToken ct)
    {
        var meta=Validate(f,"estoque");return operations.ExecutarAsync(ator,Stock,async(s,t)=>
        {
            var filter=new BsonDocument();if(f.Ativo is {} active)filter["ativo"]=active;
            if(!string.IsNullOrWhiteSpace(f.Search)){var search=f.Search.Trim().ToUpperInvariant();filter["$or"]=new BsonArray{new BsonDocument("nomeNormalizado",new BsonRegularExpression("^"+Regex.Escape(search))),new BsonDocument("skuNormalizado",search)};}
            var match=new BsonDocument("$match",filter);
            var totals=(await Aggregate(operations.Db,s,"products",[match,D("{$group:{_id:null,saldo:{$sum:{$toDecimal:'$saldo.quantidade'}},baixos:{$sum:{$cond:[{$and:['$ativo',{$lte:['$saldo.quantidade','$estoqueMinimo']}]},NumberLong(1),NumberLong(0)]}}}}")],t)).SingleOrDefault();
            var sort=meta.Ordenacao switch{"nomeDesc"=>D("{$sort:{nomeNormalizado:-1,_id:1}}"),"saldoAsc"=>D("{$sort:{'saldo.quantidade':1,nomeNormalizado:1,_id:1}}"),"saldoDesc"=>D("{$sort:{'saldo.quantidade':-1,nomeNormalizado:1,_id:1}}"),_=>D("{$sort:{nomeNormalizado:1,_id:1}}")};
            var rows=await Page(s,"products",[match,sort,D("{$project:{sku:1,nome:1,saldo:1,estoqueMinimo:1,ativo:1}}")],f,meta,d=>new SaldoProduto(d["_id"].AsGuid,d["sku"].AsString,d["nome"].AsString,d["saldo"]["quantidade"].AsInt32,d["estoqueMinimo"].AsInt32,d["ativo"].AsBoolean),t);
            return new RelatorioEstoque(rows,new(totals is null?0:Exact(totals["saldo"]),totals is null?0:Exact(totals["baixos"])));
        },ct);
    }
    public Task<RelatorioMovimentos> MovimentosAsync(SessaoAtual ator,ConsultaRelatorio f,CancellationToken ct)
    {
        var meta=Validate(f,"movimentos");return operations.ExecutarAsync(ator,Stock,async(s,t)=>
        {
            var period=Indicadores.Periodo(f.Inicio,f.Fim,Fuso,operations.Now,false);var filter=new BsonDocument("ocorridoEm",new BsonDocument{{"$gte",period.InicioUtc},{"$lt",period.FimUtc}});
            if(f.ProdutoId is {} id)filter["produtoId"]=MongoEstoque.Uuid(id);if(f.Tipo is {} type)filter["tipo"]=type;var match=new BsonDocument("$match",filter);
            var totals=(await Aggregate(operations.Db,s,"stock_movements",[match,D("{$group:{_id:null,entradas:{$sum:{$cond:[{$gt:['$delta',0]},{$toDecimal:'$delta'},{$toDecimal:0}]}},saidas:{$sum:{$cond:[{$lt:['$delta',0]},{$toDecimal:{$multiply:['$delta',-1]}},{$toDecimal:0}]}}}}")],t)).SingleOrDefault();
            var entries=totals is null?0:Exact(totals["entradas"]);var exits=totals is null?0:Exact(totals["saidas"]);var direction=meta.Ordenacao=="dataAsc"?1:-1;
            var rows=await Page(s,"stock_movements",[match,new BsonDocument("$sort",new BsonDocument{{"ocorridoEm",direction},{"_id",direction}}),D("{$project:{produtoId:1,tipo:1,delta:1,saldoAntes:1,saldoDepois:1,motivo:1,atorId:1,atorNomeSnapshot:1,ocorridoEm:1,vendaId:1,itemId:1}}")],f,meta,MongoEstoque.Map,t);
            return new RelatorioMovimentos(rows,new(entries,exits,checked(entries-exits)));
        },ct);
    }
}
