namespace Ecommerce.Infrastructure.Vendas;

/// <summary>Vinculada à seção "Webhook" da configuração. Ver <c>PagamentosController.Webhook</c>.</summary>
public class OpcoesWebhook
{
    public const string SecaoConfiguracao = "Webhook";

    /// <summary>
    /// Chave compartilhada usada pra assinar/validar o webhook de pagamento (HMAC-SHA256). Vazia
    /// aqui é o placeholder esperado (nunca commitada com valor real) — ver fallback de
    /// desenvolvimento em <c>InjecaoDeDependencia</c>. Esquema próprio, provisório: o gateway real
    /// (ainda não decidido — seção 9 do plano) terá seu próprio formato de assinatura, a trocar
    /// junto com <c>IServicoDePagamento</c>.
    /// </summary>
    public string ChaveSecreta { get; set; } = string.Empty;
}
