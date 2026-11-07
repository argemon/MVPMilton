using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<ClientesResponse> ClientesAsync(string token,int page,string search,string tipo,string ativo,CancellationToken ct) => GetAsync<ClientesResponse>($"api/v1/clientes?page={page}&search={WebUtility.UrlEncode(search)}&ativo={ativo}"+(tipo==""?"":"&tipo="+tipo),token,ct);
    public Task<ClienteResponse> ClienteAsync(string token,Guid id,CancellationToken ct) => GetAsync<ClienteResponse>($"api/v1/clientes/{id}",token,ct);
    public Task<HistoricoClienteResponse> HistoricoClienteAsync(string token,Guid id,CancellationToken ct,int page=1) => GetAsync<HistoricoClienteResponse>($"api/v1/clientes/{id}/vendas?page={page}",token,ct);
    public async Task<ClienteResponse> SalvarClienteAsync(string token,Guid? id,string nome,string tipo,string? documento,string? email,string? telefone,bool ativo,long versao,CancellationToken ct)
    {
        object body=id is null?new CriarClienteRequest(nome,tipo,documento,email,telefone):new AtualizarClienteRequest(nome,tipo,documento,email,telefone,ativo,versao);
        using var r=await SendAsync(id is null?HttpMethod.Post:HttpMethod.Put,"api/v1/clientes"+(id is null?"":$"/{id}"),token,body,ct);
        return await r.Content.ReadFromJsonAsync<ClienteResponse>(ct)??throw new ApiFailure(503);
    }
}
