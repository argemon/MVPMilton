using System.Net.Mail;
using System.Text.RegularExpressions;
namespace GestaoComercial.Domain.Cadastros;

public sealed record Contato(string Nome, string? Email, string? Telefone)
{
    public static Contato Criar(string? nome, string? email, string? telefone)
    {
        var n = Regex.Replace((nome ?? "").Trim(), @"\s+", " ");
        if (n.Length is < 2 or > 150) throw new ArgumentException("Nome deve ter de 2 a 150 caracteres.");
        var e = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (e is not null && (e.Length > 254 || !MailAddress.TryCreate(e, out var parsed) || parsed.Address != e)) throw new ArgumentException("Email inválido.");
        string? t = null;
        if (!string.IsNullOrWhiteSpace(telefone))
        {
            if (telefone.Any(c => !char.IsAsciiDigit(c) && c is not ' ' and not '+' and not '(' and not ')' and not '-')) throw new ArgumentException("Telefone inválido.");
            t = new string(telefone.Where(char.IsAsciiDigit).ToArray());
            if (t.Length is < 10 or > 15) throw new ArgumentException("Telefone deve ter de 10 a 15 dígitos.");
        }
        return new(n, e, t);
    }
}
