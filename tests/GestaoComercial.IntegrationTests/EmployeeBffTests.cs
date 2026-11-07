using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Funcionarios;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class EmployeeBffTests(BffFixture f) : IClassFixture<BffFixture>
{
    [Fact] public Task EmployeePagesAndHttpAccountCreationRequireCookiePolicyAndAntiforgery() => f.Mongo.SafeAsync(async () =>
    {
        await f.Backend.Api.Services.GetRequiredService<FuncionariosSchema>().InitializeAsync(default);
        var admin=await f.Backend.SeedAsync("Administrador"); using var api=f.Backend.Client((await f.Backend.LoginAsync(admin)).AccessToken);
        using var created=await api.PostAsJsonAsync("/api/v1/funcionarios",new CriarFuncionarioRequest("Pessoa BFF",null,null)); var employee=(await created.Content.ReadFromJsonAsync<FuncionarioResponse>())!;
        using var c=f.Client(); using var login=await f.Login(c,admin.Email,AuthFixture.Password);
        foreach (var path in new[]{"/funcionarios","/funcionarios/novo",$"/funcionarios/{employee.Id}"}) Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(path)).StatusCode);
        var page=await c.GetStringAsync($"/usuarios/novo?funcionarioId={employee.Id}"); Assert.Contains("/usuarios/criar",page);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/usuarios/criar",new FormUrlEncodedContent([]))).StatusCode);
        using var result=await BffFixture.Form(c,"/usuarios/criar",BffFixture.Csrf(page),("funcionarioId",employee.Id.ToString()),("email",Guid.NewGuid()+"@example.test"),("senha",AuthFixture.Password),("perfil","Vendedor"));
        Assert.Equal(HttpStatusCode.SeeOther,result.StatusCode); Assert.StartsWith("/usuarios/",result.Headers.Location!.ToString());
        var detail=await c.GetStringAsync(result.Headers.Location); Assert.DoesNotContain(AuthFixture.Password,detail); Assert.Contains(employee.Id.ToString(),detail);
        using var seller=f.Client(); var u=await f.Backend.SeedAsync(); using var sellerLogin=await f.Login(seller,u.Email,AuthFixture.Password);
        Assert.Contains("acesso-negado",(await seller.GetAsync("/funcionarios")).Headers.Location!.ToString());
    });
}
