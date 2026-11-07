using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<FuncionariosResponse> FuncionariosAsync(string token, int page, string search, string ativo, CancellationToken ct) => GetAsync<FuncionariosResponse>($"api/v1/funcionarios?page={page}&search={WebUtility.UrlEncode(search)}&ativo={ativo}", token, ct);
    public Task<FuncionarioResponse> FuncionarioAsync(string token, Guid id, CancellationToken ct) => GetAsync<FuncionarioResponse>($"api/v1/funcionarios/{id}", token, ct);
    public async Task<FuncionarioResponse> SalvarFuncionarioAsync(string token, Guid? id, string nome, string? email, string? telefone, bool ativo, long versao, CancellationToken ct)
    {
        object body = id is null ? new CriarFuncionarioRequest(nome, email, telefone) : new AtualizarFuncionarioRequest(nome, email, telefone, ativo, versao);
        using var r = await SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, "api/v1/funcionarios" + (id is null ? "" : $"/{id}"), token, body, ct);
        return await r.Content.ReadFromJsonAsync<FuncionarioResponse>(ct) ?? throw new ApiFailure(503);
    }
    internal async Task<UsuarioResponse> CriarContaAsync(string token, Guid funcionarioId, string email, string senha, string perfil, CancellationToken ct)
    {
        using var r = await SendAsync(HttpMethod.Post, "api/v1/usuarios", token, new { funcionarioId, email, senhaTemporaria = senha, perfil }, ct);
        return await r.Content.ReadFromJsonAsync<UsuarioResponse>(ct) ?? throw new ApiFailure(503);
    }
}
