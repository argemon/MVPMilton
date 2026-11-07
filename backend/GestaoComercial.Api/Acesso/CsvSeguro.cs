using System.Globalization;
using System.Text;
namespace GestaoComercial.Api.Acesso;

public static class CsvSeguro
{
    public static string Texto(string? value)
    {
        var raw=value??"";var trimmed=raw.TrimStart(' ');
        var danger=trimmed.Length>0&&"=+-@\t\r\n＝＋－＠".Contains(trimmed[0]);
        var text=raw.Replace('\r',' ').Replace('\n',' ');
        if(danger)text="'"+text;
        return "\""+text.Replace("\"","\"\"")+"\"";
    }
    public static string Numero(long value)=>value.ToString(CultureInfo.InvariantCulture);
    public static string Dinheiro(decimal value)=>value.ToString("0.00",CultureInfo.InvariantCulture);
    public static byte[] Gerar(IEnumerable<string> header,IEnumerable<IEnumerable<string>> linhas)
    {
        var b=new StringBuilder();b.AppendJoin(';',header.Select(Texto)).Append("\r\n");
        foreach(var linha in linhas)b.AppendJoin(';',linha).Append("\r\n");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(b.ToString())).ToArray();
    }
}
