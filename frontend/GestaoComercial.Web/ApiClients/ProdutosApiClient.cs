using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
namespace GestaoComercial.Web.ApiClients;

public sealed partial class AcessoApiClient
{
    public Task<ReferenciasResponse> ReferenciasAsync(string token,bool marca,int page,string search,string ativo,CancellationToken ct)=>GetAsync<ReferenciasResponse>($"api/v1/{(marca?"marcas":"categorias")}?page={page}&pageSize=100&search={WebUtility.UrlEncode(search)}&ativo={ativo}",token,ct);
    public async Task<ReferenciaResponse> SalvarReferenciaAsync(string token,bool marca,Guid? id,string nome,bool ativo,long versao,CancellationToken ct) {
        object body=id is null?new CriarReferenciaRequest(nome):new AtualizarReferenciaRequest(nome,ativo,versao);
        using var r=await SendAsync(id is null?HttpMethod.Post:HttpMethod.Put,$"api/v1/{(marca?"marcas":"categorias")}"+(id is null?"":$"/{id}"),token,body,ct);return await r.Content.ReadFromJsonAsync<ReferenciaResponse>(ct)??throw new ApiFailure(503);
    }
    public Task<ProdutosResponse> ProdutosAsync(string token,int page,string search,Guid? categoriaId,Guid? marcaId,string ativo,CancellationToken ct)=>GetAsync<ProdutosResponse>($"api/v1/produtos?page={page}&search={WebUtility.UrlEncode(search)}&ativo={ativo}"+(categoriaId is null?"":"&categoriaId="+categoriaId)+(marcaId is null?"":"&marcaId="+marcaId),token,ct);
    public Task<ProdutoResponse> ProdutoAsync(string token,Guid id,CancellationToken ct)=>GetAsync<ProdutoResponse>($"api/v1/produtos/{id}",token,ct);
    public async Task<ProdutoResponse> SalvarProdutoAsync(string token,Guid? id,CriarProdutoRequest data,bool ativo,long versao,CancellationToken ct) {
        object body=id is null?data:new AtualizarProdutoRequest(data.Sku,data.Nome,data.Descricao,data.CategoriaId,data.MarcaId,data.Preco,data.EstoqueMinimo,ativo,versao);
        using var r=await SendAsync(id is null?HttpMethod.Post:HttpMethod.Put,"api/v1/produtos"+(id is null?"":$"/{id}"),token,body,ct);return await r.Content.ReadFromJsonAsync<ProdutoResponse>(ct)??throw new ApiFailure(503);
    }
    public async Task<byte[]> ImagemProdutoAsync(string token,Guid id,CancellationToken ct) {
        using var r=await SendAsync(HttpMethod.Get,$"api/v1/produtos/{id}/imagem",token,null,ct);return await r.Content.ReadAsByteArrayAsync(ct);
    }
    public async Task<ProdutoResponse> EnviarImagemAsync(string token,Guid id,byte[] bytes,long versao,CancellationToken ct) {
        using var request=new HttpRequestMessage(HttpMethod.Post,$"api/v1/produtos/{id}/imagem");request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        using var content=new MultipartFormDataContent();content.Add(new StringContent(versao.ToString(System.Globalization.CultureInfo.InvariantCulture)),"versao");content.Add(new ByteArrayContent(bytes),"arquivo","imagem");request.Content=content;
        try {
            using var r=await http.SendAsync(request,ct);
            if(!r.IsSuccessStatusCode) throw new ApiFailure((int)r.StatusCode);
            return await r.Content.ReadFromJsonAsync<ProdutoResponse>(ct)??throw new ApiFailure(503);
        }catch(HttpRequestException){throw new ApiFailure(503);}catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new ApiFailure(503);}
    }
    public async Task RemoverImagemAsync(string token,Guid id,long versao,CancellationToken ct) {using var r=await SendAsync(HttpMethod.Delete,$"api/v1/produtos/{id}/imagem?versao={versao}",token,null,ct);}
}
