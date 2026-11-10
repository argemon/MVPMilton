using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;
public sealed partial class AcessoApiClient
{
    public Task<EstadoCargaResponse> EstadoCargaAsync(string token,CancellationToken ct)=>GetAsync<EstadoCargaResponse>("api/v1/administracao/dados-demonstracao",token,ct);
    public async Task LimparDadosAsync(string token,Guid operacaoId,string confirmacao,CancellationToken ct)
    {
        using var response=await SendAsync(HttpMethod.Post,"api/v1/administracao/dados-demonstracao/limpar",token,new LimparDadosRequest(operacaoId,confirmacao),ct);
    }
    public async Task<CargaDemonstracaoResponse> CargaAsync(string token,bool restaurar,bool confirmar,CancellationToken ct)
    {
        using var response=await SendAsync(HttpMethod.Post,"api/v1/administracao/dados-demonstracao/"+(restaurar?"restaurar":"gerar"),token,restaurar?new RestaurarCargaRequest(confirmar):null,ct);
        return await response.Content.ReadFromJsonAsync<CargaDemonstracaoResponse>(ct)??throw new ApiFailure(503);
    }
}
