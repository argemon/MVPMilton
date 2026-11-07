using System.Text.RegularExpressions;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Produtos;
using GestaoComercial.Domain.Cadastros;
using GestaoComercial.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
namespace GestaoComercial.Infrastructure.Produtos;

public sealed class MongoProdutos(OperacoesCadastro operations,ImagensPrivadas images,ProdutosSchema schema) : IProdutos
{
    private static readonly string[] Read=["Administrador","Vendedor","Estoquista"],Write=["Administrador","Estoquista"];
    private IMongoCollection<ProdutoDocumento> Products=>operations.Db.GetCollection<ProdutoDocumento>("products");
    private IMongoCollection<ReferenciaDocumento> References(bool brand)=>operations.Db.GetCollection<ReferenciaDocumento>(brand?"brands":"categories");
    private static Referencia Public(ReferenciaDocumento r)=>new(r.Id,r.Nome,r.Ativo,r.Versao);
    private static Produto Public(ProdutoDocumento p)=>new(p.Id,p.Sku,p.Nome,p.Descricao,p.CategoriaId,p.MarcaId,p.PrecoCentavos,p.EstoqueMinimo,p.Saldo.Quantidade,p.Imagem is not null,p.Ativo,p.Versao,new(p.CreatedAt,TimeSpan.Zero),new(p.UpdatedAt,TimeSpan.Zero));
    private static CadastroException Missing()=>new("RECURSO_NAO_ENCONTRADO",404);
    public async Task<PaginaReferencias> ReferenciasAsync(SessaoAtual ator,bool marca,string? search,bool? ativo,int page,int pageSize,CancellationToken ct) {
        await operations.ExigirAsync(ator,Read,ct);OperacoesCadastro.Pagina(page,pageSize,search);
        var f=Builders<ReferenciaDocumento>.Filter.Empty;
        if(ativo is {} a) f&=Builders<ReferenciaDocumento>.Filter.Eq(x=>x.Ativo,a);
        if(!string.IsNullOrWhiteSpace(search)) f&=Builders<ReferenciaDocumento>.Filter.Regex(x=>x.NomeNormalizado,new BsonRegularExpression("^"+Regex.Escape(search.Trim().ToUpperInvariant())));
        var total=await References(marca).CountDocumentsAsync(f,cancellationToken:ct);
        var rows=await References(marca).Find(f).SortBy(x=>x.NomeNormalizado).ThenBy(x=>x.Id).Skip(checked((page-1)*pageSize)).Limit(pageSize).ToListAsync(ct);
        return new(rows.Select(Public).ToArray(),page,pageSize,total);
    }
    public async Task<Referencia> ReferenciaAsync(SessaoAtual ator,bool marca,Guid id,CancellationToken ct) {
        await operations.ExigirAsync(ator,Read,ct);return Public(await References(marca).Find(x=>x.Id==id).SingleOrDefaultAsync(ct)??throw Missing());
    }
    public async Task<Referencia> SalvarReferenciaAsync(SessaoAtual ator,bool marca,Guid? id,string? nome,bool ativo,long versao,CancellationToken ct) {
        string n;try{n=DadosProduto.NomeReferencia(nome);}catch(ArgumentException){throw new CadastroException("VALIDACAO");}
        if(id is not null&&versao<1) throw new CadastroException("VALIDACAO");
        try {
            return await operations.ExecutarAsync(ator,Write,async(s,token)=>{
                ReferenciaDocumento r;
                if(id is null) {
                    r=new(){Nome=n,NomeNormalizado=n.ToUpperInvariant(),CreatedAt=operations.Now,UpdatedAt=operations.Now};
                    await References(marca).InsertOneAsync(s,r,cancellationToken:token);
                } else {
                    var f=Builders<ReferenciaDocumento>.Filter.Where(x=>x.Id==id.Value&&x.Versao==versao);
                    var u=Builders<ReferenciaDocumento>.Update.Set(x=>x.Nome,n).Set(x=>x.NomeNormalizado,n.ToUpperInvariant()).Set(x=>x.Ativo,ativo).Set(x=>x.UpdatedAt,operations.Now).Inc(x=>x.Versao,1).Inc(x=>x.GuardVersion,1);
                    r=await References(marca).FindOneAndUpdateAsync(s,f,u,new FindOneAndUpdateOptions<ReferenciaDocumento>{ReturnDocument=ReturnDocument.After},token)
                        ??throw(await References(marca).Find(s,x=>x.Id==id.Value).AnyAsync(token)?new CadastroException("VERSAO_DESATUALIZADA",409):Missing());
                    if(!ativo) {
                        var used=marca?Builders<ProdutoDocumento>.Filter.Where(x=>x.MarcaId==id.Value&&x.Ativo):Builders<ProdutoDocumento>.Filter.Where(x=>x.CategoriaId==id.Value&&x.Ativo);
                        if(await Products.Find(s,used).AnyAsync(token)) throw new CadastroException("REFERENCIA_EM_USO",409);
                    }
                }
                await operations.AuditarAsync(s,ator,id is null?"ReferenciaCriada":"ReferenciaAlterada",marca?"Marca":"Categoria",r.Id,token);return Public(r);
            },ct);
        }catch(MongoWriteException e) when(e.WriteError.Category==ServerErrorCategory.DuplicateKey){throw new CadastroException("NOME_DUPLICADO",409);}
    }
    public async Task<PaginaProdutos> ListarAsync(SessaoAtual ator,string? search,Guid? categoriaId,Guid? marcaId,bool? ativo,int page,int pageSize,CancellationToken ct) {
        await operations.ExigirAsync(ator,Read,ct);OperacoesCadastro.Pagina(page,pageSize,search);
        var f=Builders<ProdutoDocumento>.Filter.Empty;
        if(ativo is {} a) f&=Builders<ProdutoDocumento>.Filter.Eq(x=>x.Ativo,a);
        if(categoriaId is {} category) f&=Builders<ProdutoDocumento>.Filter.Eq(x=>x.CategoriaId,category);
        if(marcaId is {} brand) f&=Builders<ProdutoDocumento>.Filter.Eq(x=>x.MarcaId,brand);
        if(!string.IsNullOrWhiteSpace(search)) {var q=search.Trim().ToUpperInvariant();f&=Builders<ProdutoDocumento>.Filter.Regex(x=>x.NomeNormalizado,new BsonRegularExpression("^"+Regex.Escape(q)))|Builders<ProdutoDocumento>.Filter.Eq(x=>x.SkuNormalizado,q);}
        var total=await Products.CountDocumentsAsync(f,cancellationToken:ct);
        var rows=await Products.Find(f).SortBy(x=>x.NomeNormalizado).ThenBy(x=>x.Id).Skip(checked((page-1)*pageSize)).Limit(pageSize).ToListAsync(ct);
        return new(rows.Select(Public).ToArray(),page,pageSize,total);
    }
    public async Task<Produto> ConsultarAsync(SessaoAtual ator,Guid id,CancellationToken ct) {
        await operations.ExigirAsync(ator,Read,ct);return Public(await Products.Find(x=>x.Id==id).SingleOrDefaultAsync(ct)??throw Missing());
    }
    private async Task GuardReferences(IClientSessionHandle s,bool marca,IEnumerable<Guid> ids,Guid? required,bool active,CancellationToken ct) {
        foreach(var id in ids.Distinct().Order()) {
            var r=await References(marca).FindOneAndUpdateAsync(s,Builders<ReferenciaDocumento>.Filter.Eq(x=>x.Id,id),Builders<ReferenciaDocumento>.Update.Inc(x=>x.GuardVersion,1),new FindOneAndUpdateOptions<ReferenciaDocumento>{ReturnDocument=ReturnDocument.After},ct)??throw Missing();
            if(active&&id==required&&!r.Ativo) throw new CadastroException("RECURSO_INATIVO",409);
        }
    }
    public async Task<Produto> SalvarAsync(SessaoAtual ator,Guid? id,DadosProduto dados,bool ativo,long versao,CancellationToken ct) {
        // Reapply domain invariants even when called without HTTP mapping.
        DadosProduto d;try{d=DadosProduto.Criar(dados.Sku,dados.Nome,dados.Descricao,dados.CategoriaId,dados.MarcaId,dados.PrecoCentavos,dados.EstoqueMinimo);}catch(ArgumentException){throw new CadastroException("VALIDACAO");}
        if(id is not null&&versao<1) throw new CadastroException("VALIDACAO");
        try {
            return await operations.ExecutarAsync(ator,Write,async(s,token)=>{
                var old=id is {} pid?await Products.Find(s,x=>x.Id==pid).SingleOrDefaultAsync(token)??throw Missing():null;
                if(old is not null&&old.Versao!=versao) throw new CadastroException("VERSAO_DESATUALIZADA",409);
                await GuardReferences(s,false,old is null?[d.CategoriaId]:[old.CategoriaId,d.CategoriaId],d.CategoriaId,ativo,token);
                await GuardReferences(s,true,new[]{old?.MarcaId,d.MarcaId}.OfType<Guid>(),d.MarcaId,ativo,token);
                ProdutoDocumento p;
                if(id is null) {
                    p=new(){Sku=d.Sku,SkuNormalizado=d.Sku,Nome=d.Nome,NomeNormalizado=d.Nome.ToUpperInvariant(),Descricao=d.Descricao,CategoriaId=d.CategoriaId,MarcaId=d.MarcaId,PrecoCentavos=d.PrecoCentavos,EstoqueMinimo=d.EstoqueMinimo,CreatedAt=operations.Now,UpdatedAt=operations.Now};
                    await Products.InsertOneAsync(s,p,cancellationToken:token);
                } else {
                    var update=Builders<ProdutoDocumento>.Update.Set(x=>x.Sku,d.Sku).Set(x=>x.SkuNormalizado,d.Sku).Set(x=>x.Nome,d.Nome).Set(x=>x.NomeNormalizado,d.Nome.ToUpperInvariant()).Set(x=>x.Descricao,d.Descricao).Set(x=>x.CategoriaId,d.CategoriaId).Set(x=>x.MarcaId,d.MarcaId).Set(x=>x.PrecoCentavos,d.PrecoCentavos).Set(x=>x.EstoqueMinimo,d.EstoqueMinimo).Set(x=>x.Ativo,ativo).Set(x=>x.UpdatedAt,operations.Now).Inc(x=>x.Versao,1).Inc(x=>x.GuardVersion,1);
                    p=await Products.FindOneAndUpdateAsync(s,Builders<ProdutoDocumento>.Filter.Where(x=>x.Id==id.Value&&x.Versao==versao),update,new FindOneAndUpdateOptions<ProdutoDocumento>{ReturnDocument=ReturnDocument.After},token)??throw new CadastroException("VERSAO_DESATUALIZADA",409);
                }
                await operations.AuditarAsync(s,ator,id is null?"ProdutoCriado":"ProdutoAlterado","Produto",p.Id,token);return Public(p);
            },ct);
        } catch(MongoWriteException e) when(e.WriteError.Category==ServerErrorCategory.DuplicateKey){throw new CadastroException("SKU_DUPLICADO",409);}
    }
    public async Task<Produto> ImagemAsync(SessaoAtual ator,Guid id,byte[] conteudo,long versao,CancellationToken ct) {
        await operations.ExigirAsync(ator,Write,ct); if(versao<1) throw new CadastroException("VALIDACAO");
        var image=await images.GravarAsync(conteudo,ct);
        // Keep the new file on ANY failure (including unknown commit): explicit cleanup owns orphans.
        var result=await UpdateImage(ator,id,image,versao,ct);images.RemoverAposCommit(result.Old);return result.Product;
    }
    private Task<(Produto Product,string? Old)> UpdateImage(SessaoAtual ator,Guid id,ImagemDocumento? image,long versao,CancellationToken ct)=>operations.ExecutarAsync(ator,Write,async(s,token)=>{
        var p=await Products.Find(s,x=>x.Id==id).SingleOrDefaultAsync(token)??throw Missing();
        if(p.Versao!=versao) throw new CadastroException("VERSAO_DESATUALIZADA",409);
        if(image is null&&p.Imagem is null) throw Missing();
        var previous=p.Imagem?.Chave;
        var update=Builders<ProdutoDocumento>.Update.Set(x=>x.Imagem,image).Set(x=>x.UpdatedAt,operations.Now).Inc(x=>x.Versao,1).Inc(x=>x.GuardVersion,1);
        var changed=await Products.FindOneAndUpdateAsync(s,Builders<ProdutoDocumento>.Filter.Where(x=>x.Id==id&&x.Versao==versao),update,new FindOneAndUpdateOptions<ProdutoDocumento>{ReturnDocument=ReturnDocument.After},token)??throw new CadastroException("VERSAO_DESATUALIZADA",409);
        await operations.AuditarAsync(s,ator,image is null?"ImagemRemovida":"ImagemAlterada","Produto",id,token);return(Public(changed),previous);
    },ct);
    public async Task<ConteudoImagem> LerImagemAsync(SessaoAtual ator,Guid id,CancellationToken ct) {
        await operations.ExigirAsync(ator,Read,ct);var p=await Products.Find(x=>x.Id==id).SingleOrDefaultAsync(ct)??throw Missing();
        if(p.Imagem is null) throw Missing();
        try{return new(await File.ReadAllBytesAsync(images.Caminho(p.Imagem.Chave),ct),p.Imagem.Mime);}catch(FileNotFoundException){throw Missing();}
    }
    public async Task RemoverImagemAsync(SessaoAtual ator,Guid id,long versao,CancellationToken ct) {
        if(versao<1) throw new CadastroException("VALIDACAO");var result=await UpdateImage(ator,id,null,versao,ct);images.RemoverAposCommit(result.Old);
    }
    public async Task<int> LimparOrfaosAsync(CancellationToken ct) {
        // CLI only, in maintenance, with writes stopped. No directory deletion and no client paths.
        if(!await schema.IsReadyAsync(ct)) throw new InvalidOperationException("Evolução04 necessária antes da limpeza.");
        if(!Directory.Exists(images.Root)) return 0;
        var rows=await Products.Find(Builders<ProdutoDocumento>.Filter.Empty).Project(x=>x.Imagem).ToListAsync(ct);
        var used=rows.Where(x=>x is not null).Select(x=>x!.Chave).ToHashSet(StringComparer.Ordinal);var removed=0;
        foreach(var file in Directory.EnumerateFiles(images.Root,"*.png",SearchOption.TopDirectoryOnly)) {
            ct.ThrowIfCancellationRequested();var key=Path.GetFileName(file);
            if(Regex.IsMatch(key,"^[a-f0-9]{32}\\.png$")&&!used.Contains(key)) { File.Delete(images.Caminho(key));removed++; }
        }return removed;
    }
}
