using GestaoComercial.Infrastructure;
using Xunit;

public sealed class CenarioMercadoTests
{
    [Fact]
    public void EmbalagensMaioresCustamMaisEProdutosTemPrecoPositivo()
    {
        Assert.Equal(350,CenarioMercado.Preco(0));
        Assert.Equal(700,CenarioMercado.Preco(1));
        Assert.Equal(549,CenarioMercado.Preco(42));
        Assert.Equal(3294,CenarioMercado.Preco(43));
        for(var i=0;i<100;i++)
        {
            Assert.InRange(CenarioMercado.Preco(i),1,10000);
            if(i%5!=0)Assert.True(CenarioMercado.Preco(i)>CenarioMercado.Preco(i-1));
        }
        Assert.Equal(100,Enumerable.Range(0,100).Select(CenarioMercado.Produto).Distinct().Count());
        Assert.Equal(52,Enumerable.Range(0,52).Select(CenarioMercado.Pessoa).Distinct().Count());
    }
}
