namespace Ecommerce.Infrastructure.Vendas;

/// <summary>Vinculada à seção "Pagamentos" da configuração.</summary>
public class OpcoesPagamentos
{
    public const string SecaoConfiguracao = "Pagamentos";

    public const string ProvedorFake = "Fake";

    public const string ProvedorMercadoPago = "MercadoPago";

    /// <summary>
    /// <see cref="ProvedorFake"/> (padrão — dev/testes, confirmação via <c>POST /pagamentos/webhook</c>
    /// assinado com <c>Webhook:ChaveSecreta</c>) ou <see cref="ProvedorMercadoPago"/>.
    /// </summary>
    public string Provedor { get; set; } = ProvedorFake;

    public OpcoesMercadoPago MercadoPago { get; set; } = new();
}

/// <summary>Credenciais do Mercado Pago — sempre via secret/variável de ambiente, nunca commitadas.</summary>
public class OpcoesMercadoPago
{
    /// <summary>Access token da aplicação (as credenciais de teste começam com <c>TEST-</c>).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// "Assinatura secreta" gerada em Suas integrações → Webhooks — valida o cabeçalho
    /// <c>x-signature</c>. Sem ela, todo webhook é rejeitado (fail-safe).
    /// </summary>
    public string ChaveSecretaWebhook { get; set; } = string.Empty;

    /// <summary>Usa o <c>sandbox_init_point</c> em vez do <c>init_point</c> (contas de teste antigas).</summary>
    public bool UsarSandbox { get; set; }
}
