using GestaoComercial.Domain.Estoque;
using Xunit;

public sealed class StockRulesTests
{
    [Theory][InlineData("Entrada",10,10)][InlineData("SaidaManual",2,-2)][InlineData("Ajuste",-1,-1)][InlineData("Ajuste",1,1)]
    public void DeltaPreservesExplicitOperationSign(string type,int quantity,int expected) => Assert.Equal(expected,Movimentacao.Delta(type,quantity));
    [Theory][InlineData("Entrada",0)][InlineData("Entrada",-1)][InlineData("SaidaManual",-2)][InlineData("Ajuste",0)][InlineData("Ajuste",-1000001)][InlineData("Entrada",1000001)][InlineData("Venda",1)]
    public void InvalidMovementIsRejected(string type,int quantity) => Assert.Throws<ArgumentException>(()=>Movimentacao.Delta(type,quantity));
    [Fact] public void NegativeAndOverflowNeverProduceBalance()
    {
        Assert.Equal(0,Movimentacao.SaldoFinal(7,-7));
        Assert.Equal("ESTOQUE_INSUFICIENTE",Assert.Throws<InvalidOperationException>(()=>Movimentacao.SaldoFinal(1,-2)).Message);
        Assert.Equal("SALDO_EXCEDE_LIMITE",Assert.Throws<InvalidOperationException>(()=>Movimentacao.SaldoFinal(int.MaxValue,1)).Message);
        Assert.Equal(int.MaxValue,Movimentacao.SaldoFinal(int.MaxValue-1,1));
    }
    [Theory][InlineData(null)][InlineData("curto")][InlineData("          ")]
    public void ReasonIsRequired(string? reason)=>Assert.Throws<ArgumentException>(()=>Movimentacao.Motivo(reason));
    [Fact] public void ReasonIsTrimmedAndBounded() { Assert.Equal("Motivo válido",Movimentacao.Motivo(" Motivo válido "));Assert.Throws<ArgumentException>(()=>Movimentacao.Motivo(new string('a',501))); }
}
