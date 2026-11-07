namespace GestaoComercial.Contracts;

public record ReferenciaResponse(Guid Id,string Nome,bool Ativo,long Versao);
public record ReferenciasResponse(IReadOnlyList<ReferenciaResponse> Items,int Page,int PageSize,long TotalItems);
public record CriarReferenciaRequest(string? Nome);
public record AtualizarReferenciaRequest(string? Nome,bool Ativo,long Versao);
public record ProdutoResponse(Guid Id,string Sku,string Nome,string? Descricao,Guid CategoriaId,Guid? MarcaId,decimal Preco,int EstoqueMinimo,int Saldo,string? ImagemUrl,bool Ativo,long Versao,DateTimeOffset CreatedAt,DateTimeOffset UpdatedAt);
public record ProdutosResponse(IReadOnlyList<ProdutoResponse> Items,int Page,int PageSize,long TotalItems);
public record CriarProdutoRequest(string? Sku,string? Nome,string? Descricao,Guid CategoriaId,Guid? MarcaId,decimal Preco,int EstoqueMinimo);
public record AtualizarProdutoRequest(string? Sku,string? Nome,string? Descricao,Guid CategoriaId,Guid? MarcaId,decimal Preco,int EstoqueMinimo,bool Ativo,long Versao);
