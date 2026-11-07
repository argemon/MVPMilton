using GestaoComercial.Application.Acesso;
using GestaoComercial.Domain.Cadastros;
namespace GestaoComercial.Application.Produtos;

public record Referencia(Guid Id,string Nome,bool Ativo,long Versao);
public record PaginaReferencias(IReadOnlyList<Referencia> Items,int Page,int PageSize,long TotalItems);
public record Produto(Guid Id,string Sku,string Nome,string? Descricao,Guid CategoriaId,Guid? MarcaId,long PrecoCentavos,int EstoqueMinimo,int Saldo,bool TemImagem,bool Ativo,long Versao,DateTimeOffset CreatedAt,DateTimeOffset UpdatedAt);
public record PaginaProdutos(IReadOnlyList<Produto> Items,int Page,int PageSize,long TotalItems);
public record ConteudoImagem(byte[] Bytes,string Mime);
public interface IProdutos
{
    Task<PaginaReferencias> ReferenciasAsync(SessaoAtual ator,bool marca,string? search,bool? ativo,int page,int pageSize,CancellationToken ct);
    Task<Referencia> ReferenciaAsync(SessaoAtual ator,bool marca,Guid id,CancellationToken ct);
    Task<Referencia> SalvarReferenciaAsync(SessaoAtual ator,bool marca,Guid? id,string? nome,bool ativo,long versao,CancellationToken ct);
    Task<PaginaProdutos> ListarAsync(SessaoAtual ator,string? search,Guid? categoriaId,Guid? marcaId,bool? ativo,int page,int pageSize,CancellationToken ct);
    Task<Produto> ConsultarAsync(SessaoAtual ator,Guid id,CancellationToken ct);
    Task<Produto> SalvarAsync(SessaoAtual ator,Guid? id,DadosProduto dados,bool ativo,long versao,CancellationToken ct);
    Task<Produto> ImagemAsync(SessaoAtual ator,Guid id,byte[] conteudo,long versao,CancellationToken ct);
    Task<ConteudoImagem> LerImagemAsync(SessaoAtual ator,Guid id,CancellationToken ct);
    Task RemoverImagemAsync(SessaoAtual ator,Guid id,long versao,CancellationToken ct);
}
