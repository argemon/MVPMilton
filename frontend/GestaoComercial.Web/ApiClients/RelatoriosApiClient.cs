using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<RelatorioVendasResponse> RelatorioVendasAsync(string token,string query,CancellationToken ct)=>GetAsync<RelatorioVendasResponse>("api/v1/relatorios/vendas"+query,token,ct);
    public Task<RelatorioOperadoresResponse> RelatorioOperadoresAsync(string token,string query,CancellationToken ct)=>GetAsync<RelatorioOperadoresResponse>("api/v1/relatorios/operadores"+query,token,ct);
    public Task<RelatorioProdutosResponse> RelatorioProdutosAsync(string token,string query,CancellationToken ct)=>GetAsync<RelatorioProdutosResponse>("api/v1/relatorios/produtos"+query,token,ct);
    public Task<RelatorioEstoqueResponse> RelatorioEstoqueAsync(string token,string query,CancellationToken ct)=>GetAsync<RelatorioEstoqueResponse>("api/v1/relatorios/estoque"+query,token,ct);
    public Task<RelatorioMovimentosResponse> RelatorioMovimentosAsync(string token,string query,CancellationToken ct)=>GetAsync<RelatorioMovimentosResponse>("api/v1/relatorios/movimentos"+query,token,ct);
    public async Task<byte[]> ExportarRelatorioAsync(string token,string tipo,string query,CancellationToken ct)
    {
        if(tipo is not("vendas" or "operadores" or "produtos" or "estoque" or "movimentos"))throw new ApiFailure(404);
        using var r=await SendAsync(HttpMethod.Get,"api/v1/relatorios/"+tipo+"/exportacao"+query,token,null,ct);
        if(r.Content.Headers.ContentType?.MediaType!="text/csv")throw new ApiFailure(503);
        return await r.Content.ReadAsByteArrayAsync(ct);
    }
}
