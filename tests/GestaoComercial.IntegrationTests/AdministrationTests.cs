extern alias ApiHost;
using GestaoComercial.Application.Acesso;
using GestaoComercial.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

public sealed class BootstrapTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    [Fact]
    public Task BootstrapIsOneTimeAndNeverResetsPassword() => f.Mongo.SafeAsync(async () =>
    {
        using var scope = f.Api.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<IAutenticacao>();
        Assert.True(await service.ProvisionarAsync("Administrador Teste", "bootstrap@example.test", AuthFixture.Password, default));
        Assert.False(await service.ProvisionarAsync("Outro Admin", "outro@example.test", "Outra-Sintetica!83", default));
        var l = await service.EntrarAsync("bootstrap@example.test", AuthFixture.Password, default); Assert.NotNull(l); Assert.True(l.Usuario.DeveAlterarSenha);
        Assert.Equal(1, await f.Users.CountDocumentsAsync(x => x.Perfil == "Administrador"));
    });
}
public sealed class AdministrationTests(AuthFixture f) : IClassFixture<AuthFixture>
{
    [Fact]
    public Task ConcurrentRemovalProtectsLastActiveAdministrator() => f.Mongo.SafeAsync(async () =>
    {
        // Remove synthetic admins left by another test in this dedicated class only through fixtures.
        await f.Users.UpdateManyAsync(x => x.Perfil == "Administrador", Builders<UsuarioIdentity>.Update.Set(x => x.Ativo, false));
        var a = await f.SeedAsync("Administrador"); var b = await f.SeedAsync("Administrador");
        var la = await f.LoginAsync(a); var lb = await f.LoginAsync(b);
        using var sa = f.Api.Services.CreateScope(); using var sb = f.Api.Services.CreateScope();
        var aa = sa.ServiceProvider.GetRequiredService<IAutenticacao>(); var ab = sb.ServiceProvider.GetRequiredService<IAutenticacao>();
        var ca = (await aa.ValidarAsync(la.AccessToken, false, default))!; var cb = (await ab.ValidarAsync(lb.AccessToken, false, default))!;
        var va = (await aa.ConsultarAsync(ca, a.Id, default)).Versao; var vb = (await ab.ConsultarAsync(cb, b.Id, default)).Versao;
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<FalhaAcesso?> Remove(IAutenticacao auth, SessaoAtual actor, long version)
        { await barrier.Task; try { await auth.AtualizarAsync(actor, actor.Usuario.Id, "Vendedor", false, version, default); return null; } catch (AcessoException e) { return e.Falha; } }
        var ta = Remove(aa, ca, va); var tb = Remove(ab, cb, vb); barrier.SetResult(); var outcomes = await Task.WhenAll(ta, tb);
        Assert.Single(outcomes, x => x is null); Assert.Single(outcomes, x => x == FalhaAcesso.UltimoAdministrador);
        Assert.Equal(1, await f.Users.CountDocumentsAsync(x => x.Perfil == "Administrador" && x.Ativo));
    });
}
