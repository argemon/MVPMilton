namespace GestaoComercial.Contracts;

public sealed record ItemVendaRequest(Guid ProdutoId,int Quantidade);
public sealed record CriarVendaRequest(Guid ClienteId,IReadOnlyList<ItemVendaRequest>? Itens);
public sealed record AtualizarVendaRequest(Guid ClienteId,IReadOnlyList<ItemVendaRequest>? Itens,long Versao);
public sealed record ConfirmarVendaRequest(long VersaoEsperada);
public sealed record EncerrarVendaRequest(long VersaoEsperada,string? Motivo);
public sealed record OperadorVendaResponse(Guid Id,string Nome);
public sealed record ClienteVendaResponse(string Nome,string? Documento);
public sealed record EventoVendaResponse(OperadorVendaResponse Ator,DateTimeOffset Data,string? Motivo);
public sealed record ItemVendaResponse(Guid Id,Guid ProdutoId,string SkuSnapshot,string NomeSnapshot,int Quantidade,decimal PrecoUnitario,decimal Total);
public sealed record VendaResponse(Guid Id,Guid ClienteId,ClienteVendaResponse? ClienteSnapshot,OperadorVendaResponse Criador,string Status,IReadOnlyList<ItemVendaResponse> Itens,decimal Total,long Versao,DateTimeOffset CriadoEm,EventoVendaResponse? Confirmacao,EventoVendaResponse? Cancelamento,EventoVendaResponse? Descarte);
public sealed record VendaResumoResponse(Guid Id,Guid ClienteId,string? ClienteNome,OperadorVendaResponse Criador,string Status,decimal Total,long Versao,DateTimeOffset CriadoEm);
public sealed record VendasResponse(IReadOnlyList<VendaResumoResponse> Items,int Page,int PageSize,long TotalItems);
