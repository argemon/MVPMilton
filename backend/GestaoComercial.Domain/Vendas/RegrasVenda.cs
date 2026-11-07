namespace GestaoComercial.Domain.Vendas;

public static class RegrasVenda
{
    public const long TotalMaximo=99999999999999L;
    public static long Subtotal(long preco,int quantidade)
    {
        if(preco is <1 or >999999999 || quantidade is <1 or >1000000)throw new ArgumentException("VALIDACAO");
        var total=checked(preco*quantidade);if(total>TotalMaximo)throw new ArgumentException("LIMITE_MONETARIO_EXCEDIDO");return total;
    }
    public static long Total(IEnumerable<long> subtotais)
    {
        long total=0;foreach(var value in subtotais){if(value<0)throw new ArgumentException("VALIDACAO");total=checked(total+value);if(total>TotalMaximo)throw new ArgumentException("LIMITE_MONETARIO_EXCEDIDO");}return total;
    }
    public static void Itens(IEnumerable<(Guid ProdutoId,int Quantidade)> itens)
    {
        var rows=itens.ToArray();if(rows.Length>100||rows.Select(x=>x.ProdutoId).Distinct().Count()!=rows.Length||rows.Any(x=>x.ProdutoId==Guid.Empty||x.Quantidade is <1 or >1000000))throw new ArgumentException("VALIDACAO");
    }
    public static string Transicao(string atual,string acao,int itens)
    {
        return (atual,acao) switch
        {
            ("Rascunho","confirmacao") when itens>0=>"Confirmada",
            ("Confirmada","cancelamento")=>"Cancelada",
            ("Rascunho","descarte")=>"Descartada",
            _=>throw new InvalidOperationException("ESTADO_INVALIDO")
        };
    }
}
