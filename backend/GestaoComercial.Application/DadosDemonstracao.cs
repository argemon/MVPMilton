namespace GestaoComercial.Application.Demonstracao;
using GestaoComercial.Application.Acesso;
public sealed record ResultadoCarga(int Funcionarios,int Usuarios,int Clientes,int Produtos,int Vendas,int Canceladas);
public sealed record EstadoCarga(string Banco,DateTimeOffset? UltimaExecucao);
public interface IDadosDemonstracao
{
    Task<EstadoCarga> ConsultarAsync(SessaoAtual ator,CancellationToken ct);
    Task<ResultadoCarga> ExecutarAsync(SessaoAtual ator,bool restaurar,bool confirmar,CancellationToken ct);
    Task LimparAsync(SessaoAtual ator,Guid operacaoId,string confirmacao,CancellationToken ct);
}
