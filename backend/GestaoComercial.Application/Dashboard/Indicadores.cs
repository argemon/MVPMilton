using GestaoComercial.Application.Clientes;
namespace GestaoComercial.Application.Dashboard;

public static class Indicadores
{
    public static long Ticket(long receita,long quantidade)
    {
        if(receita<0||quantidade<0)throw new CadastroException("VALIDACAO");
        if(quantidade==0)return 0;
        var quociente=receita/quantidade;var resto=receita%quantidade;
        return checked(quociente+(resto>=quantidade/2+quantidade%2?1:0));
    }
    public static (DateOnly Inicio,DateOnly Fim,DateTime InicioUtc,DateTime FimUtc) Periodo(DateOnly? inicio,DateOnly? fim,string fuso,DateTimeOffset agora,bool padrao=true)
    {
        var zone=TimeZoneInfo.FindSystemTimeZoneById(fuso);
        if(inicio is null&&fim is null&&padrao){var hoje=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora,zone).DateTime);inicio=new(hoje.Year,hoje.Month,1);fim=inicio.Value.AddMonths(1);}
        if(inicio is null||fim is null||inicio>=fim||fim.Value.DayNumber-inicio.Value.DayNumber>366)throw new CadastroException("VALIDACAO");
        var a=inicio.Value.ToDateTime(TimeOnly.MinValue);var b=fim.Value.ToDateTime(TimeOnly.MinValue);
        if(zone.IsInvalidTime(a)||zone.IsInvalidTime(b))throw new CadastroException("VALIDACAO");
        return(inicio.Value,fim.Value,TimeZoneInfo.ConvertTimeToUtc(a,zone),TimeZoneInfo.ConvertTimeToUtc(b,zone));
    }
}
