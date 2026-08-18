namespace Ecommerce.Application.Vendas;

/// <summary>
/// Abstração sobre o gateway de pagamento — ainda não decidido (Stripe/Mercado Pago/PagBank, ver
/// seção 9 do plano). Implementado por enquanto por um gateway fake (não chama nenhum serviço
/// externo de verdade) só pra deixar o fluxo de checkout completo e testável; trocar pela
/// integração real é uma implementação nova desta interface, sem mudar Carrinho/Pedido.
/// </summary>
public interface IServicoDePagamento
{
    /// <summary>Inicia a cobrança e retorna a referência externa (id da transação no gateway).</summary>
    Task<string> IniciarCobrancaAsync(Guid pedidoId, decimal valor, CancellationToken ct = default);
}
