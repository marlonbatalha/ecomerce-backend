namespace Ecommerce.Application.Autenticacao;

/// <summary>Nomes dos papéis (roles) do Identity — únicas duas roles do MVP (ver seção 2.4 do plano).</summary>
public static class Papeis
{
    public const string Cliente = "cliente";

    public const string Admin = "admin";

    public static readonly IReadOnlyCollection<string> Todos = [Cliente, Admin];
}
