using System.Net;
using System.Net.Http.Json;
using GestaoComercial.Contracts;
using GestaoComercial.Infrastructure.Funcionarios;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

public sealed class EmployeeTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    private async Task<HttpClient> Admin()
    {
        await f.Api.Services.GetRequiredService<FuncionariosSchema>().InitializeAsync(default);
        return f.Client((await f.LoginAsync(await f.SeedAsync("Administrador"))).AccessToken);
    }
    private static async Task<FuncionarioResponse> Create(HttpClient c, string nome = "Pessoa Funcionária")
    {
        using var r = await c.PostAsJsonAsync("/api/v1/funcionarios", new CriarFuncionarioRequest(nome, "contato@example.test", "65999990000"));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode); Assert.NotNull(r.Headers.Location);
        return (await r.Content.ReadFromJsonAsync<FuncionarioResponse>())!;
    }
    [Fact] public Task CrudPaginationVersionAndNoAutomaticAccount() => f.Mongo.SafeAsync(async () =>
    {
        using var c = await Admin(); var count = await f.Users.CountDocumentsAsync(FilterDefinition<GestaoComercial.Infrastructure.Identity.UsuarioIdentity>.Empty);
        var prefix = "Nome" + Guid.NewGuid().ToString("N"); var e = await Create(c, "  " + prefix + "   Teste ");
        Assert.Equal(prefix + " Teste", e.Nome); Assert.Equal(1, e.Versao); Assert.True(e.Ativo);
        Assert.Equal(count, await f.Users.CountDocumentsAsync(FilterDefinition<GestaoComercial.Infrastructure.Identity.UsuarioIdentity>.Empty));
        var page = (await c.GetFromJsonAsync<FuncionariosResponse>($"/api/v1/funcionarios?search={prefix}&pageSize=1"))!; Assert.Single(page.Items); Assert.Equal(1, page.TotalItems);
        Assert.Empty((await c.GetFromJsonAsync<FuncionariosResponse>($"/api/v1/funcionarios?search={prefix}&page=2&pageSize=1"))!.Items);
        using var changed = await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}", new AtualizarFuncionarioRequest(e.Nome, null, null, false, e.Versao)); Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}", new AtualizarFuncionarioRequest(e.Nome, null, null, true, e.Versao))).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<FuncionariosResponse>($"/api/v1/funcionarios?search={prefix}"))!.Items);
        Assert.Single((await c.GetFromJsonAsync<FuncionariosResponse>($"/api/v1/funcionarios?search={prefix}&ativo=todos"))!.Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/funcionarios", new { nome = "x", email = "bad" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/funcionarios", new { nome = "Pessoa", atorId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/funcionarios?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/funcionarios/{Guid.NewGuid()}")).StatusCode);
    });
    [Theory] [InlineData("Vendedor")] [InlineData("Estoquista")]
    public Task AllEmployeeRoutesDenyOtherRoles(string role) => f.Mongo.SafeAsync(async () =>
    {
        using var c = f.Client((await f.LoginAsync(await f.SeedAsync(role))).AccessToken); using var anon = f.Client(); var id = Guid.NewGuid();
        foreach (var client in new[] { c, anon }) {
            var expected = client == c ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
            Assert.Equal(expected, (await client.GetAsync("/api/v1/funcionarios")).StatusCode);
            Assert.Equal(expected, (await client.GetAsync($"/api/v1/funcionarios/{id}")).StatusCode);
            Assert.Equal(expected, (await client.PostAsJsonAsync("/api/v1/funcionarios", new { nome="Teste" })).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync($"/api/v1/funcionarios/{id}", new { nome="Teste", ativo=false, versao=1 })).StatusCode);
            Assert.Equal(expected, (await client.PostAsJsonAsync("/api/v1/usuarios", new { funcionarioId=id, email="x@example.test", senhaTemporaria=AuthFixture.Password, perfil="Vendedor" })).StatusCode);
        }
    });
    [Fact] public Task LinkedAccountMustChangePasswordAndDeactivationRevokesExistingSessions() => f.Mongo.SafeAsync(async () =>
    {
        using var c = await Admin(); var e = await Create(c); var email = Guid.NewGuid() + "@example.test";
        using var created = await c.PostAsJsonAsync("/api/v1/usuarios", new { funcionarioId=e.Id, email, senhaTemporaria=AuthFixture.Password, perfil="Vendedor" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode); var u = (await created.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        Assert.Equal(e.Id, u.FuncionarioId); Assert.True(u.DeveAlterarSenha); Assert.DoesNotContain(AuthFixture.Password, await created.Content.ReadAsStringAsync());
        var identity = await f.Users.Find(x => x.Id == u.Id).SingleAsync(); var l = await f.LoginAsync(identity); using var session = f.Client(l.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await session.GetAsync("/api/v1/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await session.PostAsJsonAsync("/api/v1/auth/alterar-senha", new { senhaAtual=AuthFixture.Password, novaSenha="Nova-Sintetica!735" })).StatusCode);
        var active = await f.LoginAsync(identity, "Nova-Sintetica!735"); using var current = f.Client(active.AccessToken);
        using var disabled = await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}", new AtualizarFuncionarioRequest(e.Nome, e.Email, e.Telefone, false, e.Versao)); Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await current.GetAsync("/api/v1/auth/me")).StatusCode);
        var updated = (await disabled.Content.ReadFromJsonAsync<FuncionarioResponse>())!;
        var account = (await c.GetFromJsonAsync<UsuarioResponse>($"/api/v1/usuarios/{u.Id}"))!; Assert.False(account.Ativo);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v1/usuarios/{u.Id}", new AtualizarUsuarioRequest("Vendedor", true, account.Versao))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}", new AtualizarFuncionarioRequest(e.Nome, e.Email, e.Telefone, true, updated.Versao))).StatusCode);
        Assert.False((await c.GetFromJsonAsync<UsuarioResponse>($"/api/v1/usuarios/{u.Id}"))!.Ativo);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/v1/usuarios/{u.Id}", new AtualizarUsuarioRequest("Vendedor", true, account.Versao))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/usuarios", new { funcionarioId=e.Id, email=Guid.NewGuid()+"@example.test", senhaTemporaria=AuthFixture.Password, perfil="Vendedor" })).StatusCode);
    });
    [Fact] public Task ConcurrentAccountsHaveExactlyOneWinnerAndUniqueIndexIncludesInactive() => f.Mongo.SafeAsync(async () =>
    {
        using var c = await Admin(); var e = await Create(c); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> CreateAccount() { await gate.Task; using var r = await c.PostAsJsonAsync("/api/v1/usuarios", new { funcionarioId=e.Id, email=Guid.NewGuid()+"@example.test", senhaTemporaria=AuthFixture.Password, perfil="Vendedor" }); return r.StatusCode; }
        var a=CreateAccount(); var b=CreateAccount(); gate.SetResult(); var results=await Task.WhenAll(a,b);
        Assert.Single(results, x=>x==HttpStatusCode.Created); Assert.Single(results, x=>x==HttpStatusCode.Conflict);
        var u=await f.Users.Find(x=>x.EmployeeId==e.Id).SingleAsync(); u.Id=Guid.NewGuid(); u.EmailNormalizado=Guid.NewGuid().ToString(); u.Ativo=false;
        var error=await Assert.ThrowsAsync<MongoWriteException>(()=>f.Users.InsertOneAsync(u)); Assert.Equal(ServerErrorCategory.DuplicateKey,error.WriteError.Category);
    });
    [Fact] public Task CreateAccountVersusDeactivateNeverLeavesActiveLinkedAccount() => f.Mongo.SafeAsync(async () =>
    {
        using var c=await Admin(); var e=await Create(c); var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpStatusCode> Account() { await gate.Task; using var r=await c.PostAsJsonAsync("/api/v1/usuarios",new {funcionarioId=e.Id,email=Guid.NewGuid()+"@example.test",senhaTemporaria=AuthFixture.Password,perfil="Vendedor"}); return r.StatusCode; }
        async Task<HttpStatusCode> Disable() { await gate.Task; using var r=await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}",new AtualizarFuncionarioRequest(e.Nome,null,null,false,e.Versao)); return r.StatusCode; }
        var a=Account(); var b=Disable(); gate.SetResult(); await Task.WhenAll(a,b);
        var disabled=await b;
        // A bounded administrative transaction may return503 under contention. Resolve
        // its result before another PUT; never count503 itself as successful deactivation.
        for(var attempt=0;disabled==HttpStatusCode.ServiceUnavailable&&attempt<2;attempt++) {
            var current=(await c.GetFromJsonAsync<FuncionarioResponse>($"/api/v1/funcionarios/{e.Id}"))!;
            if(!current.Ativo){disabled=HttpStatusCode.OK;break;}
            using var retry=await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}",new AtualizarFuncionarioRequest(current.Nome,current.Email,current.Telefone,false,current.Versao));
            disabled=retry.StatusCode;
        }
        Assert.Equal(HttpStatusCode.OK,disabled); Assert.Contains(await a,new[]{HttpStatusCode.Created,HttpStatusCode.Conflict});
        Assert.False((await c.GetFromJsonAsync<FuncionarioResponse>($"/api/v1/funcionarios/{e.Id}"))!.Ativo);
        Assert.Equal(0,await f.Users.CountDocumentsAsync(x=>x.EmployeeId==e.Id && x.Ativo));
    });
    [Fact] public Task SchemaIdempotentAndAuditFailureRollsBackEmployeeAccountAndSession() => f.Mongo.SafeAsync(async () =>
    {
        using var c=await Admin(); var e=await Create(c);
        var schema=f.Api.Services.GetRequiredService<FuncionariosSchema>(); await schema.InitializeAsync(default); await schema.InitializeAsync(default); Assert.True(await schema.IsReadyAsync(default));
        using var r=await c.PostAsJsonAsync("/api/v1/usuarios",new {funcionarioId=e.Id,email=Guid.NewGuid()+"@example.test",senhaTemporaria=AuthFixture.Password,perfil="Vendedor"}); var u=(await r.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        var identity=await f.Users.Find(x=>x.Id==u.Id).SingleAsync(); using var current=f.Client((await f.LoginAsync(identity)).AccessToken);
        var db=f.Mongo.Client.GetDatabase(f.Mongo.Name); using var collections=await db.ListCollectionsAsync(new ListCollectionsOptions{Filter=new BsonDocument("name","audit_events")}); var validator=(await collections.SingleAsync())["options"]["validator"].AsBsonDocument;
        try {
            await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",new BsonDocument("$and",new BsonArray{validator,new BsonDocument("acao",new BsonDocument("$ne","FuncionarioAlterado"))})}});
            Assert.Equal(HttpStatusCode.ServiceUnavailable,(await c.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}",new AtualizarFuncionarioRequest(e.Nome,null,null,false,e.Versao))).StatusCode);
            Assert.True((await c.GetFromJsonAsync<FuncionarioResponse>($"/api/v1/funcionarios/{e.Id}"))!.Ativo); Assert.True((await f.Users.Find(x=>x.Id==u.Id).SingleAsync()).Ativo);
            Assert.Equal(HttpStatusCode.OK,(await current.GetAsync("/api/v1/auth/me")).StatusCode);
        } finally { await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","audit_events"},{"validator",validator}}); }
    });
    [Fact] public Task LastAdministratorCannotBeDisabledThroughEmployee() => f.Mongo.SafeAsync(async () =>
    {
        using var c=await Admin(); var e=await Create(c);
        using var r=await c.PostAsJsonAsync("/api/v1/usuarios",new {funcionarioId=e.Id,email=Guid.NewGuid()+"@example.test",senhaTemporaria=AuthFixture.Password,perfil="Administrador"}); var u=(await r.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        var identity=await f.Users.Find(x=>x.Id==u.Id).SingleAsync(); using var temp=f.Client((await f.LoginAsync(identity)).AccessToken);
        await temp.PostAsJsonAsync("/api/v1/auth/alterar-senha",new{senhaAtual=AuthFixture.Password,novaSenha="Admin-Nova!735"});
        using var sole=f.Client((await f.LoginAsync(identity,"Admin-Nova!735")).AccessToken);
        await f.Users.UpdateManyAsync(x=>x.Perfil=="Administrador" && x.Id!=u.Id,Builders<GestaoComercial.Infrastructure.Identity.UsuarioIdentity>.Update.Set(x=>x.Ativo,false));
        Assert.Equal(HttpStatusCode.Conflict,(await sole.PutAsJsonAsync($"/api/v1/funcionarios/{e.Id}",new AtualizarFuncionarioRequest(e.Nome,null,null,false,e.Versao))).StatusCode);
        Assert.True((await sole.GetFromJsonAsync<FuncionarioResponse>($"/api/v1/funcionarios/{e.Id}"))!.Ativo);
    });
}
