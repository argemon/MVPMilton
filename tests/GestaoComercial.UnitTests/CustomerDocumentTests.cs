using GestaoComercial.Domain.Cadastros;
using Xunit;

public sealed class CustomerDocumentTests
{
    [Theory] [InlineData("PF","12345678909")] [InlineData("PJ","11222333000181")] [InlineData("PF",null)] [InlineData("PJ",null)]
    public void AcceptsValidOptionalDocuments(string tipo,string? document) => Assert.Equal(document,DocumentoCliente.Validar(tipo,document));
    [Theory] [InlineData("PF","")] [InlineData("PF","11111111111")] [InlineData("PF","12345678900")] [InlineData("PJ","11222333000180")]
    [InlineData("PJ","12345678909")] [InlineData("PF","123.456.789-09")] [InlineData("XX",null)]
    public void RejectsInvalidTypeOrDocument(string tipo,string? document) => Assert.Throws<ArgumentException>(()=>DocumentoCliente.Validar(tipo,document));
}
