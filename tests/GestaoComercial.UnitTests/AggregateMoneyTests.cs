using GestaoComercial.Application.Clientes;
using GestaoComercial.Infrastructure.Dashboard;
using MongoDB.Bson;
using Xunit;

public sealed class AggregateMoneyTests
{
    [Theory][InlineData("9007199254740993",9007199254740993L)][InlineData("9223372036854775807",long.MaxValue)]
    public void IntegerCentavosRemainExactBeyondDoublePrecision(string value,long expected)=>Assert.Equal(expected,MongoDashboard.Exact(new BsonDecimal128(Decimal128.Parse(value))));
    [Theory][InlineData("9223372036854775808")][InlineData("999999999999999999999999999999999")][InlineData("1.5")]
    public void AggregateOverflowOrFractionIsRejectedWithoutRounding(string value)
    {
        var e=Assert.Throws<CadastroException>(()=>MongoDashboard.Exact(new BsonDecimal128(Decimal128.Parse(value))));Assert.Equal("LIMITE_MONETARIO_EXCEDIDO",e.Code);Assert.Equal(400,e.Status);
    }
}
