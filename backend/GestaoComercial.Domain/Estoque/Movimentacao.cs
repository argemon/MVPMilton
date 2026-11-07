namespace GestaoComercial.Domain.Estoque;

public static class Movimentacao
{
    public static int Delta(string tipo, int quantidade)
    {
        if (tipo == "Ajuste")
        {
            if (quantidade is 0 or < -1000000 or > 1000000) throw new ArgumentException("VALIDACAO");
            return quantidade;
        }
        if (quantidade is < 1 or > 1000000 || tipo is not ("Entrada" or "SaidaManual")) throw new ArgumentException("VALIDACAO");
        return tipo == "Entrada" ? quantidade : -quantidade;
    }

    public static string Motivo(string? motivo)
    {
        var value = motivo?.Trim();
        if (value is null || value.Length is < 10 or > 500) throw new ArgumentException("VALIDACAO");
        return value;
    }

    public static int SaldoFinal(int saldo, int delta)
    {
        var result = checked((long)saldo + delta);
        if (saldo < 0 || result < 0) throw new InvalidOperationException("ESTOQUE_INSUFICIENTE");
        if (result > int.MaxValue) throw new InvalidOperationException("SALDO_EXCEDE_LIMITE");
        return checked((int)result);
    }
}
