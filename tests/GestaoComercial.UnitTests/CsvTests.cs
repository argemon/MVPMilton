extern alias ApiHost;
using System.Text;
using Xunit;
using Csv=ApiHost::GestaoComercial.Api.Acesso.CsvSeguro;

public sealed class CsvTests
{
    [Theory][InlineData("=1+1")][InlineData("+SUM(A1)")][InlineData("-1+1")][InlineData("@cmd")][InlineData("\t=1")][InlineData("\r=1")][InlineData("\n=1")][InlineData("   =1")][InlineData("＝1")]
    public void FormulaPrefixesAreNeutralized(string value)=>Assert.StartsWith("\"'",Csv.Texto(value));
    [Fact]public void QuotesSeparatorsNewlinesAndUnicodeRemainOneCell()
    {
        Assert.Equal("\"Ação; \"\"sim\"\"  fim\"",Csv.Texto("Ação; \"sim\"\r\nfim"));
        Assert.Equal("-2",Csv.Numero(-2));Assert.Equal("12.50",Csv.Dinheiro(12.50m));
        var bytes=Csv.Gerar(["Nome","Valor"],[new[]{Csv.Texto("=1;2"),Csv.Dinheiro(12.50m)}]);Assert.Equal(new byte[]{239,187,191},bytes.Take(3));var text=Encoding.UTF8.GetString(bytes);Assert.Contains("\"'=1;2\";12.50\r\n",text);Assert.DoesNotContain("\"'=1;2\";1250",text);
    }
}
