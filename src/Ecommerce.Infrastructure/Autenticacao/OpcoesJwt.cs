namespace Ecommerce.Infrastructure.Autenticacao;

/// <summary>Vinculada à seção "Jwt" da configuração (appsettings/variáveis de ambiente).</summary>
public class OpcoesJwt
{
    public const string SecaoConfiguracao = "Jwt";

    public string Emissor { get; set; } = string.Empty;

    public string Audiencia { get; set; } = string.Empty;

    /// <summary>
    /// Chave de assinatura HMAC-SHA256. Nunca commitada com valor real — vem de variável de
    /// ambiente/secret em staging/produção (ver seção 1.7 do plano). Vazia aqui é o placeholder
    /// esperado; ver <c>InjecaoDeDependencia</c> para o fallback de desenvolvimento.
    /// </summary>
    public string ChaveAssinatura { get; set; } = string.Empty;

    public int MinutosExpiracaoAccessToken { get; set; } = 15;

    public int DiasExpiracaoRefreshToken { get; set; } = 7;
}
