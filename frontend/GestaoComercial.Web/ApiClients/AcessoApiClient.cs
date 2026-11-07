using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed class ApiFailure(int status, string? code = null) : Exception("Não foi possível concluir a solicitação.") { public int Status { get; } = status; public string? Code { get; } = code; }
// Host-private credential transports. Do not serialize into Blazor component state.
internal record LoginResponse(string AccessToken, DateTimeOffset ExpiraEm, UsuarioAutenticado Usuario);
public sealed partial class AcessoApiClient(HttpClient http)
{
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        try
        {
            var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) {
                var status = (int)response.StatusCode; string? code=null;
                try { using var doc=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); if(doc.RootElement.TryGetProperty("code",out var value)) code=value.GetString(); } catch(System.Text.Json.JsonException) { }
                response.Dispose(); throw new ApiFailure(status,code);
            }
            return response;
        }
        catch (HttpRequestException) { throw new ApiFailure(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ApiFailure(503); }
    }
    private async Task<T> GetAsync<T>(string path, string token, CancellationToken ct)
    { using var r = await SendAsync(HttpMethod.Get, path, token, null, ct); return await r.Content.ReadFromJsonAsync<T>(ct) ?? throw new ApiFailure(503); }
    internal async Task<LoginResponse> EntrarAsync(string email, string senha, CancellationToken ct)
    { using var r = await SendAsync(HttpMethod.Post, "api/v1/auth/login", null, new { email, senha }, ct); return await r.Content.ReadFromJsonAsync<LoginResponse>(ct) ?? throw new ApiFailure(503); }
    public Task<SessaoResponse> MeAsync(string token, CancellationToken ct) => GetAsync<SessaoResponse>("api/v1/auth/me", token, ct);
    public async Task SairAsync(string token, CancellationToken ct) { using var r = await SendAsync(HttpMethod.Post, "api/v1/auth/logout", token, null, ct); }
    public async Task SenhaAsync(string token, string atual, string nova, CancellationToken ct) { using var r = await SendAsync(HttpMethod.Post, "api/v1/auth/alterar-senha", token, new { senhaAtual = atual, novaSenha = nova }, ct); }
    public Task<UsuariosResponse> ListarAsync(string token, int page, string? search, bool? ativo, CancellationToken ct) => GetAsync<UsuariosResponse>($"api/v1/usuarios?page={page}&pageSize=20&search={WebUtility.UrlEncode(search ?? "")}" + (ativo is null ? "" : "&ativo=" + ativo.Value.ToString().ToLowerInvariant()), token, ct);
    public Task<UsuarioResponse> ConsultarAsync(string token, Guid id, CancellationToken ct) => GetAsync<UsuarioResponse>($"api/v1/usuarios/{id}", token, ct);
    public async Task<UsuarioResponse> AtualizarAsync(string token, Guid id, AtualizarUsuarioRequest body, CancellationToken ct)
    { using var r = await SendAsync(HttpMethod.Put, $"api/v1/usuarios/{id}", token, body, ct); return await r.Content.ReadFromJsonAsync<UsuarioResponse>(ct) ?? throw new ApiFailure(503); }
    public async Task RedefinirAsync(string token, Guid id, string senha, long versao, CancellationToken ct)
    { using var r = await SendAsync(HttpMethod.Post, $"api/v1/usuarios/{id}/redefinir-senha", token, new { senhaTemporaria = senha, versaoEsperada = versao }, ct); }
}
