using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<DashboardResponse> DashboardAsync(string token,DateTime inicio,DateTime fim,CancellationToken ct)=>GetAsync<DashboardResponse>($"api/v1/dashboard?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}",token,ct);
}
