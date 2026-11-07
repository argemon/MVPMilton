using GestaoComercial.Application.Acesso;
using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Produtos;
using GestaoComercial.Contracts;
using GestaoComercial.Domain.Cadastros;
namespace GestaoComercial.Api.Acesso;

public static class DinheiroHttp
{
    public static long Centavos(decimal reais) {
        if(reais is <0.01m or >9999999.99m || decimal.Truncate(reais*100m)!=reais*100m) throw new CadastroException("VALIDACAO");
        return checked((long)(reais*100m));
    }
}
public static class EndpointsProdutos
{
    private static SessaoAtual Actor(HttpContext c)=>(SessaoAtual)c.Items[typeof(SessaoAtual)]!;
    private static ReferenciaResponse Public(Referencia r)=>new(r.Id,r.Nome,r.Ativo,r.Versao);
    private static ProdutoResponse Public(Produto p)=>new(p.Id,p.Sku,p.Nome,p.Descricao,p.CategoriaId,p.MarcaId,p.PrecoCentavos/100m,p.EstoqueMinimo,p.Saldo,p.TemImagem?$"/api/v1/produtos/{p.Id}/imagem":null,p.Ativo,p.Versao,p.CreatedAt,p.UpdatedAt);
    private static DadosProduto Dados(string? sku,string? nome,string? descricao,Guid categoriaId,Guid? marcaId,decimal preco,int minimo) {
        try{return DadosProduto.Criar(sku,nome,descricao,categoriaId,marcaId,DinheiroHttp.Centavos(preco),minimo);}catch(ArgumentException){throw new CadastroException("VALIDACAO");}
    }
    public static void MapProdutos(this WebApplication app) {
        foreach(var (path,brand) in new[]{("categorias",false),("marcas",true)}) {
            var marca=brand;var group=app.MapGroup("/api/v1/"+path).RequireAuthorization("Produtos.Ler");
            group.MapGet("",async(HttpContext c,IProdutos service,string? search,string? ativo,int? page,int? pageSize,CancellationToken ct)=>{
                var p=await service.ReferenciasAsync(Actor(c),marca,search,EndpointsFuncionarios.Ativo(ativo),page??1,pageSize??20,ct);return Results.Ok(new ReferenciasResponse(p.Items.Select(Public).ToArray(),p.Page,p.PageSize,p.TotalItems));
            }).Produces<ReferenciasResponse>();
            group.MapGet("/{id:guid}",async(Guid id,HttpContext c,IProdutos service,CancellationToken ct)=>Results.Ok(Public(await service.ReferenciaAsync(Actor(c),marca,id,ct)))).Produces<ReferenciaResponse>();
            group.MapPost("",async(CriarReferenciaRequest d,HttpContext c,IProdutos service,CancellationToken ct)=>{
                var r=Public(await service.SalvarReferenciaAsync(Actor(c),marca,null,d.Nome,true,0,ct));return Results.Created($"/api/v1/{(marca?"marcas":"categorias")}/{r.Id}",r);
            }).RequireAuthorization("Produtos.Gerenciar").Produces<ReferenciaResponse>(201);
            group.MapPut("/{id:guid}",async(Guid id,AtualizarReferenciaRequest d,HttpContext c,IProdutos service,CancellationToken ct)=>Results.Ok(Public(await service.SalvarReferenciaAsync(Actor(c),marca,id,d.Nome,d.Ativo,d.Versao,ct)))).RequireAuthorization("Produtos.Gerenciar").Produces<ReferenciaResponse>();
        }
        var products=app.MapGroup("/api/v1/produtos").RequireAuthorization("Produtos.Ler");
        products.MapGet("",async(HttpContext c,IProdutos service,string? search,Guid? categoriaId,Guid? marcaId,string? ativo,int? page,int? pageSize,CancellationToken ct)=>{
            var p=await service.ListarAsync(Actor(c),search,categoriaId,marcaId,EndpointsFuncionarios.Ativo(ativo),page??1,pageSize??20,ct);return Results.Ok(new ProdutosResponse(p.Items.Select(Public).ToArray(),p.Page,p.PageSize,p.TotalItems));
        }).Produces<ProdutosResponse>();
        products.MapGet("/{id:guid}",async(Guid id,HttpContext c,IProdutos service,CancellationToken ct)=>Results.Ok(Public(await service.ConsultarAsync(Actor(c),id,ct)))).Produces<ProdutoResponse>();
        products.MapPost("",async(CriarProdutoRequest d,HttpContext c,IProdutos service,CancellationToken ct)=>{
            var p=Public(await service.SalvarAsync(Actor(c),null,Dados(d.Sku,d.Nome,d.Descricao,d.CategoriaId,d.MarcaId,d.Preco,d.EstoqueMinimo),true,0,ct));return Results.Created($"/api/v1/produtos/{p.Id}",p);
        }).RequireAuthorization("Produtos.Gerenciar").Produces<ProdutoResponse>(201);
        products.MapPut("/{id:guid}",async(Guid id,AtualizarProdutoRequest d,HttpContext c,IProdutos service,CancellationToken ct)=>Results.Ok(Public(await service.SalvarAsync(Actor(c),id,Dados(d.Sku,d.Nome,d.Descricao,d.CategoriaId,d.MarcaId,d.Preco,d.EstoqueMinimo),d.Ativo,d.Versao,ct)))).RequireAuthorization("Produtos.Gerenciar").Produces<ProdutoResponse>();
        products.MapGet("/{id:guid}/imagem",async(Guid id,HttpContext c,IProdutos service,CancellationToken ct)=>{
            var file=await service.LerImagemAsync(Actor(c),id,ct);c.Response.Headers.CacheControl="no-store";return Results.Bytes(file.Bytes,file.Mime);
        });
        products.MapPost("/{id:guid}/imagem",async(Guid id,HttpContext c,IProdutos service,CancellationToken ct)=>{
            const int limit=2097152;
            if(c.Request.ContentLength>limit+8192) throw new CadastroException("IMAGEM_EXCEDE_LIMITE",413);
            if(!c.Request.HasFormContentType) throw new CadastroException("TIPO_IMAGEM_INVALIDO",415);
            var feature=c.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();if(feature is {IsReadOnly:false}) feature.MaxRequestBodySize=limit+8192;
            IFormCollection form;try{form=await c.Request.ReadFormAsync(ct);}catch(InvalidDataException){throw new CadastroException("IMAGEM_INVALIDA");}
            if(form.Files.Count!=1||form.Files[0].Name!="arquivo"||form.Keys.Any(x=>x!="versao")||!long.TryParse(form["versao"],out var version)||version<1) throw new CadastroException("VALIDACAO");
            var file=form.Files[0];if(file.Length>limit) throw new CadastroException("IMAGEM_EXCEDE_LIMITE",413);
            await using var input=file.OpenReadStream();using var memory=new MemoryStream();await input.CopyToAsync(memory,ct);
            return Results.Ok(Public(await service.ImagemAsync(Actor(c),id,memory.ToArray(),version,ct)));
        }).RequireAuthorization("Produtos.Gerenciar").Produces<ProdutoResponse>();
        products.MapDelete("/{id:guid}/imagem",async(Guid id,long versao,HttpContext c,IProdutos service,CancellationToken ct)=>{await service.RemoverImagemAsync(Actor(c),id,versao,ct);return Results.NoContent();}).RequireAuthorization("Produtos.Gerenciar");
    }
}
