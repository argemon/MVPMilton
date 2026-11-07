using System.Security.Claims;
using GestaoComercial.Contracts;
using GestaoComercial.Web.ApiClients;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
namespace GestaoComercial.Web.Security;

public sealed class EstadoAutenticacao(ILoggerFactory logger, MongoTickets tickets, AcessoApiClient api) : RevalidatingServerAuthenticationStateProvider(logger)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(60);
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state, CancellationToken ct)
    {
        try
        {
            var key = state.User.FindFirstValue(MongoTickets.Claim); if (key is null) return false;
            var token = await tickets.TokenAsync(key, ct); if (token is null) return false;
            var me = await api.MeAsync(token, ct);
            return me.Usuario.Id.ToString() == state.User.FindFirstValue(ClaimTypes.NameIdentifier) && me.Usuario.Perfil == state.User.FindFirstValue(ClaimTypes.Role) && (me.Usuario.DeveAlterarSenha ? "sim" : "nao") == state.User.FindFirstValue("troca");
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }
}
public sealed class SessaoCircuito(AuthenticationStateProvider state, MongoTickets tickets, AcessoApiClient api)
{
    public async Task<T> UsarAsync<T>(Func<AcessoApiClient, string, CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        var principal = (await state.GetAuthenticationStateAsync()).User;
        var key = principal.FindFirstValue(MongoTickets.Claim); if (key is null) throw new ApiFailure(401);
        var token = await tickets.TokenAsync(key, ct); if (token is null) throw new ApiFailure(401);
        return await action(api, token, ct);
    }
    public Task<SessaoResponse> MeAsync(CancellationToken ct = default) => UsarAsync((a, t, c) => a.MeAsync(t, c), ct);
}
