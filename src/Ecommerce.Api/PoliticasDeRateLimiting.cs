namespace Ecommerce.Api;

public static class PoliticasDeRateLimiting
{
    /// <summary>Aplicada às rotas de login/registro — ver seção 1.7 do plano.</summary>
    public const string Autenticacao = "autenticacao";

    /// <summary>Validação pública de cupom — evita força bruta de códigos.</summary>
    public const string Cupons = "cupons";

    /// <summary>Busca pública de produtos — com termo é a consulta O(n) mais cara da API.</summary>
    public const string BuscaDeProdutos = "busca-de-produtos";
}
