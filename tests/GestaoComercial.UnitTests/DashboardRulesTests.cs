using GestaoComercial.Application.Clientes;
using GestaoComercial.Application.Dashboard;
using Xunit;

public sealed class DashboardRulesTests
{
    [Theory][InlineData(201,2,101)][InlineData(200,2,100)][InlineData(0,0,0)][InlineData(long.MaxValue,1,long.MaxValue)][InlineData(1,3,0)][InlineData(2,3,1)]
    public void TicketIsExactWithSingleRounding(long total,long count,long expected)=>Assert.Equal(expected,Indicadores.Ticket(total,count));
    [Fact]public void DefaultMonthUsesInstallationTimezoneAndEndIsExclusive()
    {
        var p=Indicadores.Periodo(null,null,"America/Cuiaba",new DateTimeOffset(2026,11,1,2,0,0,TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026,10,1),p.Inicio);Assert.Equal(new DateOnly(2026,11,1),p.Fim);Assert.Equal(new DateTime(2026,10,1,4,0,0,DateTimeKind.Utc),p.InicioUtc);
    }
    [Fact]public void InvalidAndOverlongPeriodsAreRejected()
    {
        foreach(var (a,b) in new (DateOnly?,DateOnly?)[]{(new(2026,1,1),null),(null,new(2026,1,1)),(new(2026,1,1),new(2026,1,1)),(new(2026,1,1),new(2028,1,1))})Assert.Throws<CadastroException>(()=>Indicadores.Periodo(a,b,"America/Cuiaba",DateTimeOffset.UtcNow));
    }
}
