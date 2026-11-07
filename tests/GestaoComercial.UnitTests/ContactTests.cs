using GestaoComercial.Domain.Cadastros;
using Xunit;

public sealed class ContactTests
{
    [Fact] public void NormalizesContactWithoutCreatingCredentials()
    {
        var c = Contato.Criar("  Pessoa   Sintética ", " contato@example.test ", "+55 (65) 99999-0000");
        Assert.Equal("Pessoa Sintética", c.Nome); Assert.Equal("contato@example.test", c.Email); Assert.Equal("5565999990000", c.Telefone);
        Assert.Null(Contato.Criar("Sem contato", " ", null).Email);
    }
    [Theory]
    [InlineData("a", null, null)] [InlineData(null, null, null)]
    [InlineData("Pessoa", "invalido", null)] [InlineData("Pessoa", "Nome <p@example.test>", null)]
    [InlineData("Pessoa", null, "123")] [InlineData("Pessoa", null, "abc65999990000")]
    public void RejectsInvalidFields(string? nome, string? email, string? telefone) => Assert.Throws<ArgumentException>(() => Contato.Criar(nome, email, telefone));
}
