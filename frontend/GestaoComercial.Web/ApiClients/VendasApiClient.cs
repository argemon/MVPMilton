using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<VendasResponse> VendasAsync(string token,int page,string cliente,string status,string inicio,string fim,CancellationToken ct)=>GetAsync<VendasResponse>($"api/v1/vendas?page={page}"+(cliente==""?"":"&clienteId="+WebUtility.UrlEncode(cliente))+(status==""?"":"&status="+WebUtility.UrlEncode(status))+(inicio==""?"":"&inicio="+WebUtility.UrlEncode(inicio))+(fim==""?"":"&fim="+WebUtility.UrlEncode(fim)),token,ct);
    public Task<VendaResponse> VendaAsync(string token,Guid id,CancellationToken ct)=>GetAsync<VendaResponse>($"api/v1/vendas/{id}",token,ct);
    public async Task<VendaResponse> SalvarVendaAsync(string token,Guid? id,Guid key,Guid clienteId,IReadOnlyList<ItemVendaRequest> itens,long versao,CancellationToken ct)
    {
        object body=id is null?new CriarVendaRequest(clienteId,itens):new AtualizarVendaRequest(clienteId,itens,versao);
        using var r=await SendAsync(id is null?HttpMethod.Post:HttpMethod.Put,"api/v1/vendas"+(id is null?"":$"/{id}"),token,body,ct,id is null?key:null);return await r.Content.ReadFromJsonAsync<VendaResponse>(ct)??throw new ApiFailure(503);
    }
    public async Task<VendaResponse> TransicionarVendaAsync(string token,Guid id,Guid key,string action,long versao,string? motivo,CancellationToken ct)
    {
        object body=action=="confirmacao"?new ConfirmarVendaRequest(versao):new EncerrarVendaRequest(versao,motivo);
        using var r=await SendAsync(HttpMethod.Post,$"api/v1/vendas/{id}/{action}",token,body,ct,key);return await r.Content.ReadFromJsonAsync<VendaResponse>(ct)??throw new ApiFailure(503);
    }
}
