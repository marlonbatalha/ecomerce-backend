namespace Ecommerce.Infrastructure;

/// <summary>
/// Vinculada à seção "Aplicacao": endereços públicos usados pra montar links que saem do sistema
/// (e-mails transacionais, URLs de retorno/notificação do gateway de pagamento).
/// </summary>
public class OpcoesAplicacao
{
    public const string SecaoConfiguracao = "Aplicacao";

    /// <summary>Ex.: <c>https://loja.exemplo.com</c> (dev: <c>http://localhost:3000</c>).</summary>
    public string UrlFrontend { get; set; } = "http://localhost:3000";

    /// <summary>URL pública desta API — o gateway chama o webhook nela. Ex.: <c>https://api.loja.exemplo.com</c>.</summary>
    public string UrlPublicaApi { get; set; } = string.Empty;
}
