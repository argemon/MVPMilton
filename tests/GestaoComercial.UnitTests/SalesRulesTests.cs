using GestaoComercial.Domain.Vendas;
using Xunit;

public sealed class SalesRulesTests
{
    [Fact]public void MonetaryTotalsAreExactAndBounded()
    {
        Assert.Equal(2500,RegrasVenda.Subtotal(1250,2));Assert.Equal(3750,RegrasVenda.Total([2500,1250]));Assert.Equal(0,RegrasVenda.Total([]));
        Assert.Throws<ArgumentException>(()=>RegrasVenda.Subtotal(0,1));Assert.Throws<ArgumentException>(()=>RegrasVenda.Subtotal(1250,0));Assert.Throws<ArgumentException>(()=>RegrasVenda.Subtotal(999999999,1000000));
        Assert.Throws<ArgumentException>(()=>RegrasVenda.Total([RegrasVenda.TotalMaximo,1]));
    }
    [Fact]public void ItemsMustBeDistinctValidAndLimited()
    {
        var id=Guid.NewGuid();RegrasVenda.Itens([]);Assert.Throws<ArgumentException>(()=>RegrasVenda.Itens([(id,1),(id,2)]));Assert.Throws<ArgumentException>(()=>RegrasVenda.Itens([(Guid.Empty,1)]));Assert.Throws<ArgumentException>(()=>RegrasVenda.Itens([(id,-1)]));Assert.Throws<ArgumentException>(()=>RegrasVenda.Itens(Enumerable.Range(0,101).Select(_=>(Guid.NewGuid(),1))));
    }
    [Theory][InlineData("Rascunho","confirmacao",1,"Confirmada")][InlineData("Confirmada","cancelamento",1,"Cancelada")][InlineData("Rascunho","descarte",0,"Descartada")]
    public void StateTransitionsPreserveLifecycle(string state,string action,int count,string expected)=>Assert.Equal(expected,RegrasVenda.Transicao(state,action,count));
    [Theory][InlineData("Rascunho","confirmacao",0)][InlineData("Cancelada","cancelamento",1)][InlineData("Confirmada","descarte",1)][InlineData("Descartada","confirmacao",1)]
    public void InvalidStatesNeverTransition(string state,string action,int count)=>Assert.Throws<InvalidOperationException>(()=>RegrasVenda.Transicao(state,action,count));
}
