namespace GestaoComercial.Contracts;
public sealed record RestaurarCargaRequest(bool Confirmar);
public sealed record CargaDemonstracaoResponse(int Funcionarios,int Usuarios,int Clientes,int Produtos,int Vendas,int Canceladas);
public sealed record EstadoCargaResponse(string Banco,DateTimeOffset? UltimaExecucao);
