using System.Text.RegularExpressions;
namespace GestaoComercial.Domain.Cadastros;

public sealed record DadosProduto(string Sku,string Nome,string? Descricao,Guid CategoriaId,Guid? MarcaId,long PrecoCentavos,int EstoqueMinimo)
{
    public static DadosProduto Criar(string? sku,string? nome,string? descricao,Guid categoriaId,Guid? marcaId,long precoCentavos,int minimo)
    {
        var code=(sku??"").Trim().ToUpperInvariant(); var n=Regex.Replace((nome??"").Trim(),@"\s+"," ");
        if (!Regex.IsMatch(code,"^[A-Z0-9._-]{1,40}$") || n.Length is <2 or >150 || descricao?.Length>2000 || descricao?.IndexOfAny(['<','>'])>=0 || categoriaId==Guid.Empty || marcaId==Guid.Empty || precoCentavos is <1 or >999999999 || minimo is <0 or >1000000) throw new ArgumentException("Dados de produto inválidos.");
        return new(code,n,string.IsNullOrWhiteSpace(descricao)?null:descricao.Trim(),categoriaId,marcaId,precoCentavos,minimo);
    }
    public static string NomeReferencia(string? nome)
    { var n=Regex.Replace((nome??"").Trim(),@"\s+"," "); if(n.Length is <2 or >80) throw new ArgumentException("Nome deve ter de 2 a 80 caracteres."); return n; }
}
