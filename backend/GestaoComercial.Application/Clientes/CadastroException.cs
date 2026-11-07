namespace GestaoComercial.Application.Clientes;

public sealed class CadastroException(string code, int status = 400) : Exception("Cadastro não concluído.")
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
