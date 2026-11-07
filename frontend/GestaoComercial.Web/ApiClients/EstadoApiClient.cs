using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed class EstadoApiClient(HttpClient http)
{
    public async Task<bool> ConsultarAsync(CancellationToken ct)
    {
        try { return (await http.GetFromJsonAsync<HealthResponse>("health/ready", ct))?.Status == "ready"; }
        catch (HttpRequestException) { return false; }
        catch (System.Text.Json.JsonException) { return false; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return false; }
    }
}
