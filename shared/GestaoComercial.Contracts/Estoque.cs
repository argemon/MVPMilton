namespace GestaoComercial.Contracts;

public sealed record SaldoProdutoResponse(Guid ProdutoId, string Sku, string Nome, int Saldo, int Minimo, bool Ativo);
public sealed record SaldosResponse(IReadOnlyList<SaldoProdutoResponse> Items, int Page, int PageSize, long TotalItems);
public sealed record AtorMovimentoResponse(Guid Id, string Nome);
public sealed record MovimentoResponse(Guid Id, Guid ProdutoId, string Tipo, int Delta, int SaldoAntes, int SaldoDepois, string Motivo, AtorMovimentoResponse Ator, DateTimeOffset OcorridoEm, Guid? VendaId=null, Guid? ItemId=null);
public sealed record MovimentosResponse(IReadOnlyList<MovimentoResponse> Items, int Page, int PageSize, long TotalItems);
public sealed record MovimentoRequest(Guid ProdutoId, int Quantidade, string? Motivo);
public sealed record AjusteRequest(Guid ProdutoId, int Delta, string? Motivo);
