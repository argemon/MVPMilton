namespace GestaoComercial.Domain.Acesso;

public static class Perfis
{
    public const string Administrador = "Administrador", Vendedor = "Vendedor", Estoquista = "Estoquista";
    public static bool Valido(string? perfil) => perfil is Administrador or Vendedor or Estoquista;
}
