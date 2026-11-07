namespace GestaoComercial.Domain.Cadastros;

public static class DocumentoCliente
{
    public static string? Validar(string? tipo, string? documento)
    {
        if (tipo is not "PF" and not "PJ") throw new ArgumentException("Tipo deve ser PF ou PJ.");
        if (documento is null) return null;
        var d = documento.Trim();
        if (d.Length != (tipo == "PF" ? 11 : 14) || d.Any(c => !char.IsAsciiDigit(c)) || d.Distinct().Count() == 1) throw new ArgumentException("Documento inválido.");
        var digits = d.Select(c => c - '0').ToArray();
        for (var stage = 0; stage < 2; stage++)
        {
            var count = (tipo == "PF" ? 9 : 12) + stage;
            var sum = 0;
            for (var i = 0; i < count; i++) sum += digits[i] * (tipo == "PF" ? count + 1 - i : (count - 1 - i) % 8 + 2);
            var remainder = sum % 11; var expected = remainder < 2 ? 0 : 11 - remainder;
            if (digits[count] != expected) throw new ArgumentException("Documento inválido.");
        }
        return d;
    }
}
