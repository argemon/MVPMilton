namespace GestaoComercial.Infrastructure;

// Synthetic people; familiar shelf labels, without real customer information.
internal static class CenarioMercado
{
    internal static readonly string[] Categorias = ["Arroz e grãos", "Mercearia doce", "Leites e derivados", "Limpeza", "Higiene pessoal"];
    internal static readonly string[] Marcas = ["Camil", "Nestlé", "Piracanjuba", "Ypê", "Colgate"];
    private static readonly string[] Nomes = ["Ana", "Bruno", "Carla", "Diego", "Elisa", "Felipe", "Gabriela", "Hugo", "Isabela", "João", "Laura", "Marcos", "Natália"];
    private static readonly string[] Sobrenomes = ["Almeida", "Barbosa", "Cardoso", "Duarte"];
    internal static string Pessoa(int index) => $"{Nomes[index % 13]} {Sobrenomes[index / 13]}";
    private static readonly string[] Produtos = [
        "Arroz branco Camil", "Arroz integral Camil", "Feijão carioca Camil", "Feijão preto Camil",
        "Chocolate ao leite Nestlé", "Chocolate meio amargo Nestlé", "Achocolatado Nescau", "Cereal Neston",
        "Leite integral Piracanjuba", "Leite desnatado Piracanjuba", "Leite sem lactose Piracanjuba", "Bebida láctea Piracanjuba",
        "Detergente neutro Ypê", "Detergente limão Ypê", "Amaciante Ypê", "Desinfetante Ypê",
        "Creme dental Colgate", "Creme dental Colgate Total", "Escova dental Colgate macia", "Escova dental Colgate infantil"];
    private static readonly string[][] Embalagens = [
        ["500 g", "1 kg", "2 kg", "4 kg", "5 kg"],
        ["80 g", "90 g", "150 g", "180 g", "200 g"],
        ["200 ml", "500 ml", "1 L", "kit 6 × 1 L", "caixa 12 × 1 L"],
        ["500 ml", "1 L", "2 L", "3 L", "5 L"],
        ["50 g", "70 g", "90 g", "120 g", "180 g"]];
    internal static string Produto(int index)
    {
        var grupo = index / 20;
        var item = index / 5;
        var embalagem = grupo == 4 && item >= 18 ? $"pacote {index % 5 + 1} UN" : Embalagens[grupo][index % 5];
        return $"{Produtos[item]} {embalagem}";
    }
    internal static long Preco(int index)
    {
        long[] bases = [350,450,500,525,60,65,35,40,549,559,699,499,249,249,499,399,299,499,799,599];
        var item=index/5;var tamanho=index%5;
        return (index/20) switch
        {
            0 => bases[item]*new[]{1,2,4,8,10}[tamanho],
            1 => bases[item]*new[]{8,9,15,18,20}[tamanho],
            2 => bases[item]*new[]{2,5,10,60,120}[tamanho]/10,
            3 => bases[item]*new[]{1,2,4,6,10}[tamanho],
            _ => item>=18?bases[item]*(tamanho+1):bases[item]*new[]{5,7,9,12,18}[tamanho]/5
        };
    }
}
