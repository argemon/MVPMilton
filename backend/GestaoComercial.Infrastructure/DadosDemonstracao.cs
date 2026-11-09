using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Demonstracao;
using GestaoComercial.Application.Estoque;
using GestaoComercial.Application.Funcionarios;
using GestaoComercial.Application.Produtos;
using GestaoComercial.Application.Vendas;
using GestaoComercial.Domain.Cadastros;
using GestaoComercial.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
namespace GestaoComercial.Infrastructure;

// One application replica (ADR-13): serialize the long, resumable seed, not individual business transactions.
public sealed class DadosDemonstracao(OperacoesCadastro operations,IHttpContextAccessor request,
    IFuncionarios employees,IClientes customers,IProdutos products,IEstoque stock,IVendas sales,GestaoComercial.Infrastructure.Vendas.VendasSchema schema) : IDadosDemonstracao
{
    private static readonly SemaphoreSlim Gate=new(1,1);
    private IMongoCollection<BsonDocument> Collection(string name)=>operations.Db.GetCollection<BsonDocument>(name);
    private static Guid Key(string text)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(text)).AsSpan(0,16));
    private const string Prefix="tcc-demo-v1/";
    private static readonly string[] Admin=["Administrador"];
    public async Task<EstadoCarga> ConsultarAsync(SessaoAtual ator,CancellationToken ct)
    {
        await operations.ExigirAsync(ator,Admin,ct);
        var last=await Collection("audit_events").Find(new BsonDocument("acao","DemoConcluida")).Sort(new BsonDocument("ocorridoEm",-1)).FirstOrDefaultAsync(ct);
        return new(operations.Db.DatabaseNamespace.DatabaseName,last is null?null:new DateTimeOffset(last["ocorridoEm"].ToUniversalTime()));
    }
    public async Task<ResultadoCarga> ExecutarAsync(SessaoAtual ator,bool restaurar,bool confirmar,CancellationToken ct)
    {
        await operations.ExigirAsync(ator,Admin,ct);
        if(!await schema.IsReadyAsync(ct))throw new CadastroException("PERSISTENCIA_NAO_INICIALIZADA",503);
        if(restaurar&&!confirmar)throw new CadastroException("CONFIRMACAO_OBRIGATORIA");
        var context=request.HttpContext??throw new InvalidOperationException("Contexto HTTP obrigatório.");
        if(!await Gate.WaitAsync(0,ct))throw new CadastroException("CARGA_EM_ANDAMENTO",409);
        var original=context.TraceIdentifier;
        try
        {
            // Restore receipts remain in the immutable audit trail, including after an uncertain response.
            var restores=await Collection("audit_events").Find(new BsonDocument("acao","DemoRestaurada")).CountDocumentsAsync(ct);
            var run=Prefix+restores+"/";
            if(restaurar){await Restaurar(ator,run,ct);run=Prefix+(restores+1)+"/";}
            async Task<Guid> Step(string step,Func<Task<Guid>> create)
            {
                context.TraceIdentifier=run+step;
                var existing=await Collection("audit_events").Find(new BsonDocument("traceId",context.TraceIdentifier)).FirstOrDefaultAsync(ct);
                return existing is not null?existing["recursoId"].AsGuid:await create();
            }
            var categories=new Guid[5];var brands=new Guid[5];
            for(var i=0;i<5;i++)
            {
                var n=i;
                categories[i]=await Step("categoria/"+i,async()=>(await products.SalvarReferenciaAsync(ator,false,null,CenarioMercado.Categorias[n],true,0,ct)).Id);
                brands[i]=await Step("marca/"+i,async()=>(await products.SalvarReferenciaAsync(ator,true,null,CenarioMercado.Marcas[n],true,0,ct)).Id);
            }
            var workers=new Guid[12];
            for(var i=0;i<12;i++)
            {
                var n=i;var email=$"tcc.demo.{n+1}@example.invalid";
                var employee=await Step("funcionario/"+i,async()=>(await employees.CriarFuncionarioAsync(ator,new(CenarioMercado.Pessoa(n),email,null),ct)).Id);
                workers[i]=employee;
                await Step("usuario/"+i,async()=>(await employees.CriarContaAsync(ator,employee,email,Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))+"aA1!",new[]{"Administrador","Vendedor","Estoquista"}[n%3],ct)).Id);
            }
            var clients=new Guid[40];
            for(var i=0;i<40;i++){var n=i;clients[i]=await Step("cliente/"+i,async()=>(await customers.SalvarAsync(ator,null,new(CenarioMercado.Pessoa(n+12),"PF",null,$"cliente.demo.{n+1}@example.invalid",null),true,0,ct)).Id);}
            operations.DemonstrationInstant=new DateTime(2026,8,1,12,0,0,DateTimeKind.Utc);
            var items=new Guid[100];var random=new Random(902026);
            for(var i=0;i<100;i++)
            {
                var n=i;var price=CenarioMercado.Preco(n);random.Next(500,50001);var quantity=100+random.Next(0,301);if(n>=90)quantity=7;
                items[i]=await Step("produto/"+i,async()=>(await products.SalvarAsync(ator,null,DadosProduto.Criar($"TCC-DEMO-{n+1:D3}",CenarioMercado.Produto(n),"Produto ilustrativo de supermercado — demonstração acadêmica",categories[n/20],brands[n/20],price,20),true,0,ct)).Id);
                foreach(var (kind,amount) in new[]{("Entrada",quantity),("SaidaManual",5),("Ajuste",-2)})
                {var step=$"estoque/{i}/{kind}";await Step(step,async()=>(await stock.MovimentarAsync(ator,Key(run+step),items[n],kind,amount,"Cenário fictício de demonstração",ct)).Id);}
            }
            for(var i=0;i<250;i++)
            {
                var n=i;var product=random.Next(90);var quantity=random.Next(1,4);
                // The scoped operation clock changes only timestamps of new synthetic business operations.
                operations.DemonstrationInstant=new DateTime(2026,11,7,12,0,0,DateTimeKind.Utc).AddDays(-(249-i)%90);
                var id=await Step("venda/"+i,async()=>(await sales.SalvarAsync(ator,null,Key(run+"venda/"+n),clients[n%35],[new(items[product],quantity),new(items[(product+1)%90],1)],0,ct)).Id);
                var current=await sales.ConsultarAsync(ator,id,ct);
                if(current.Status=="Rascunho")await Step("confirmacao/"+i,async()=>(await sales.TransicionarAsync(ator,id,Key(run+"confirmacao/"+n),"confirmacao",current.Versao,null,ct)).Id);
                if(i%10==0){current=await sales.ConsultarAsync(ator,id,ct);if(current.Status=="Confirmada")await Step("cancelamento/"+i,async()=>(await sales.TransicionarAsync(ator,id,Key(run+"cancelamento/"+n),"cancelamento",current.Versao,"Cancelamento fictício de demonstração",ct)).Id);}
            }
            operations.DemonstrationInstant=null;
            // Upgrade only untouched generic labels owned by this audited generation.
            for(var i=0;i<5;i++)
            {
                var n=i;
                foreach(var marca in new[]{false,true})
                {
                    var id=marca?brands[n]:categories[n];
                    var p=await products.ReferenciaAsync(ator,marca,id,ct);
                    if(p.Nome=="Demonstração "+(marca?"marca ":"categoria ")+(n+1))
                        await Step("mercado/"+(marca?"marca/":"categoria/")+n,async()=>(await products.SalvarReferenciaAsync(ator,marca,id,marca?CenarioMercado.Marcas[n]:CenarioMercado.Categorias[n],p.Ativo,p.Versao,ct)).Id);
                }
            }
            for(var i=0;i<12;i++)
            {
                var n=i;var p=await employees.ConsultarFuncionarioAsync(ator,workers[n],ct);
                if(p.Nome=="Funcionário fictício "+(n+1))await Step("mercado/funcionario/"+n,async()=>(await employees.AtualizarFuncionarioAsync(ator,p.Id,new(CenarioMercado.Pessoa(n),p.Email,p.Telefone),p.Ativo,p.Versao,ct)).Id);
            }
            for(var i=0;i<40;i++)
            {
                var n=i;var p=await customers.ConsultarAsync(ator,clients[n],ct);
                if(p.Nome=="Cliente fictício "+(n+1))await Step("mercado/cliente/"+n,async()=>(await customers.SalvarAsync(ator,p.Id,new(CenarioMercado.Pessoa(n+12),p.Tipo,p.Documento,p.Email,p.Telefone),p.Ativo,p.Versao,ct)).Id);
            }
            var legacyPrices=new Random(902026);
            for(var i=0;i<100;i++)
            {
                var originalPrice=legacyPrices.Next(500,50001);legacyPrices.Next(0,301);
                var n=i;var p=await products.ConsultarAsync(ator,items[n],ct);
                if(p.Nome=="Produto fictício "+(n+1))await Step("mercado/produto/"+n,async()=>(await products.SalvarAsync(ator,p.Id,DadosProduto.Criar(p.Sku,CenarioMercado.Produto(n),"Produto ilustrativo de supermercado — demonstração acadêmica",categories[n/20],brands[n/20],p.PrecoCentavos,p.EstoqueMinimo),p.Ativo,p.Versao,ct)).Id);
                // Only the original generated price is eligible; manual price changes survive.
                p=await products.ConsultarAsync(ator,items[n],ct);
                if(p.Nome==CenarioMercado.Produto(n)&&(p.PrecoCentavos==originalPrice||p.PrecoCentavos==199+originalPrice%4000))
                    await Step("mercado/preco-embalagem/"+n,async()=>(await products.SalvarAsync(ator,p.Id,DadosProduto.Criar(p.Sku,p.Nome,p.Descricao,p.CategoriaId,p.MarcaId,CenarioMercado.Preco(n),p.EstoqueMinimo),p.Ativo,p.Versao,ct)).Id);
            }
            for(var i=95;i<100;i++){var n=i;await Step("inativar-produto/"+i,async()=>{var p=await products.ConsultarAsync(ator,items[n],ct);return(await products.SalvarAsync(ator,p.Id,DadosProduto.Criar(p.Sku,p.Nome,p.Descricao,p.CategoriaId,p.MarcaId,p.PrecoCentavos,p.EstoqueMinimo),false,p.Versao,ct)).Id;});}
            for(var i=9;i<12;i++){var n=i;await Step("inativar-funcionario/"+i,async()=>{var e=await employees.ConsultarFuncionarioAsync(ator,workers[n],ct);return(await employees.AtualizarFuncionarioAsync(ator,e.Id,new(e.Nome,e.Email,e.Telefone),false,e.Versao,ct)).Id;});}
            await Step("concluida",()=>operations.ExecutarAsync(ator,Admin,async(s,token)=>{var id=Guid.NewGuid();await operations.AuditarAsync(s,ator,"DemoConcluida","CargaDemo",id,token);return id;},ct));
            return new(12,12,40,100,250,25);
        }
        finally{operations.DemonstrationInstant=null;context.TraceIdentifier=original;Gate.Release();}
    }
    private async Task Restaurar(SessaoAtual ator,string run,CancellationToken ct)
    {
        await operations.ExecutarAsync(ator,Admin,async(s,token)=>
        {
            // Account commands take this same guard; business commands write their referenced documents.
            var guard=await Collection("administration_control").UpdateOneAsync(s,new BsonDocument("_id","administration"),new BsonDocument("$inc",new BsonDocument("guardVersion",1L)),cancellationToken:token);
            if(guard.ModifiedCount!=1)throw new CadastroException("PERSISTENCIA_INDISPONIVEL",503);
            var audit=await Collection("audit_events").Find(s,new BsonDocument("traceId",new BsonRegularExpression("^"+System.Text.RegularExpressions.Regex.Escape(run)))).ToListAsync(token);
            BsonArray Ids(string type)=>new(audit.Where(x=>x["recursoTipo"]==type).Select(x=>x["recursoId"]).Where(x=>!x.IsBsonNull).Distinct());
            var ids=new Dictionary<string,BsonArray>{{"sales",Ids("Venda")},{"products",Ids("Produto")},{"customers",Ids("Cliente")},{"employees",Ids("Funcionario")},{"users",Ids("Usuario")},{"categories",Ids("Categoria")},{"brands",Ids("Marca")},{"stock_movements",Ids("MovimentoEstoque")}};
            BsonDocument In(string field,BsonArray values)=>new(field,new BsonDocument("$in",values));
            BsonDocument NotOwned(string name)=>new("_id",new BsonDocument("$nin",ids[name]));
            async Task Reject(string name,BsonDocument f){if(await Collection(name).Find(s,f).AnyAsync(token))throw new CadastroException("CARGA_DEPENDENCIA_MANUAL",409);}
            await Reject("sales",new BsonDocument("$and",new BsonArray{NotOwned("sales"),new BsonDocument("$or",new BsonArray{In("clienteId",ids["customers"]),In("itens.produtoId",ids["products"]),In("criadorId",ids["users"]),In("confirmacao.atorId",ids["users"]),In("cancelamento.atorId",ids["users"])})}));
            await Reject("products",new BsonDocument("$and",new BsonArray{NotOwned("products"),new BsonDocument("$or",new BsonArray{In("categoriaId",ids["categories"]),In("marcaId",ids["brands"])})}));
            await Reject("users",new BsonDocument("$and",new BsonArray{NotOwned("users"),In("employeeId",ids["employees"])}));
            await Reject("api_sessions",In("usuarioId",ids["users"]));
            var saleMovements=await Collection("stock_movements").Find(s,In("vendaId",ids["sales"])).ToListAsync(token);
            foreach(var m in saleMovements)ids["stock_movements"].Add(m["_id"]);
            await Reject("stock_movements",new BsonDocument("$and",new BsonArray{NotOwned("stock_movements"),new BsonDocument("$or",new BsonArray{In("produtoId",ids["products"]),In("atorId",ids["users"])})}));
            var all=new BsonArray(ids.Values.SelectMany(x=>x).Distinct());
            await Reject("audit_events",new BsonDocument("$and",new BsonArray{new BsonDocument("traceId",new BsonDocument("$not",new BsonRegularExpression("^"+System.Text.RegularExpressions.Regex.Escape(run)))),new BsonDocument("$or",new BsonArray{In("recursoId",all),In("atorId",ids["users"])})}));
            foreach(var pair in ids)if(pair.Value.Count>0)await Collection(pair.Key).DeleteManyAsync(s,In("_id",pair.Value),cancellationToken:token);
            await Collection("idempotency_receipts").DeleteManyAsync(s,In("recursoId",all),cancellationToken:token);
            await operations.AuditarAsync(s,ator,"DemoRestaurada","CargaDemo",Guid.NewGuid(),token);
            return true;
        },ct);
    }
}
