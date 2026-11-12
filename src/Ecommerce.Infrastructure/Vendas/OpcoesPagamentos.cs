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

    /// <summary>
    /// Validade do Pix. Curta de propósito: o estoque fica reservado até o Pix vencer. O Mercado
    /// Pago aceita de 30 minutos a 30 dias.
    /// </summary>
    public int ValidadeDoPixEmMinutos { get; set; } = 30;

    /// <summary>Validade do boleto (1 a 30 dias). 3 é o padrão do Mercado Pago: a compensação leva até 2 dias úteis.</summary>
    public int ValidadeDoBoletoEmDias { get; set; } = 3;
}
