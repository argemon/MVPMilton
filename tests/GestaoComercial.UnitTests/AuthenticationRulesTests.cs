extern alias WebHost;
using GestaoComercial.Domain.Acesso;
using GestaoComercial.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class AuthenticationRulesTests
{
    [Theory]
    [InlineData("https://externo.test/", "/")]
    [InlineData("//externo.test/", "/")]
    [InlineData("/\\externo.test", "/")]
    [InlineData("/\nexterno", "/")]
    [InlineData("/usuarios?page=2", "/usuarios?page=2")]
    [InlineData(null, "/")]
    public void ReturnUrlIsStrictlyLocal(string? value, string expected) => Assert.Equal(expected, WebHost::GestaoComercial.Web.Security.ContasHttp.RetornoLocal(value));
    [Fact]
    public void IdentityHashIsSaltedVerifiableAndSupportsRehash()
    {
        var u = new UsuarioIdentity(); var h = new PasswordHasher<UsuarioIdentity>(); const string password = "Sintetica-Teste!725";
        var a = h.HashPassword(u, password); var b = h.HashPassword(u, password); Assert.NotEqual(a, b);
        Assert.Equal(PasswordVerificationResult.Success, h.VerifyHashedPassword(u, a, password));
        Assert.Equal(PasswordVerificationResult.Failed, h.VerifyHashedPassword(u, a, "Errada-Teste!462"));
        var old = new PasswordHasher<UsuarioIdentity>(Options.Create(new PasswordHasherOptions { IterationCount = 10000 })).HashPassword(u, password);
        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, h.VerifyHashedPassword(u, old, password));
    }
    [Theory]
    [InlineData(29, 7, false, true)]
    [InlineData(30, 7, false, false)]
    [InlineData(1, 8, false, false)]
    [InlineData(1, 7, true, false)]
    public void ExpirationHasExactBoundaries(int idleMinutes, int absoluteHours, bool revoked, bool expected)
    {
        var now = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);
        var s = new SessaoDocumento { UltimoUsoEm = now.AddMinutes(-idleMinutes), ExpiraEm = now.AddHours(8 - absoluteHours), RevogadaEm = revoked ? now : null };
        Assert.Equal(expected, MongoAutenticacao.SessaoValida(s, now));
    }
    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Vendedor", true)]
    [InlineData("Estoquista", true)]
    [InlineData("Gerente", false)]
    [InlineData("administrador", false)]
    [InlineData(null, false)]
    public void OnlyApprovedRolesAreAccepted(string? role, bool expected) => Assert.Equal(expected, Perfis.Valido(role));
}
