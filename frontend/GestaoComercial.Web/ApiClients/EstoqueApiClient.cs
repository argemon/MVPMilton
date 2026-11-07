using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<SaldosResponse> SaldosAsync(string token,int page,string search,string ativo,bool abaixo,CancellationToken ct) => GetAsync<SaldosResponse>($"api/v1/estoque/saldos?page={page}&search={WebUtility.UrlEncode(search)}&ativo={ativo}&abaixoMinimo={abaixo.ToString().ToLowerInvariant()}",token,ct);
    public Task<MovimentosResponse> MovimentosAsync(string token,int page,string produto,string tipo,string inicio,string fim,CancellationToken ct) => GetAsync<MovimentosResponse>($"api/v1/estoque/movimentos?page={page}"+(produto==""?"":"&produtoId="+WebUtility.UrlEncode(produto))+(tipo==""?"":"&tipo="+WebUtility.UrlEncode(tipo))+(inicio==""?"":"&inicio="+WebUtility.UrlEncode(inicio))+(fim==""?"":"&fim="+WebUtility.UrlEncode(fim)),token,ct);
    public async Task<MovimentoResponse> MovimentarAsync(string token,Guid chave,string tipo,Guid produto,int quantidade,string motivo,CancellationToken ct)
    {
        object body=tipo=="ajustes"?new AjusteRequest(produto,quantidade,motivo):new MovimentoRequest(produto,quantidade,motivo);
        using var response=await SendAsync(HttpMethod.Post,"api/v1/estoque/"+tipo,token,body,ct,chave);
        return await response.Content.ReadFromJsonAsync<MovimentoResponse>(ct)??throw new ApiFailure(503);
    }
}
