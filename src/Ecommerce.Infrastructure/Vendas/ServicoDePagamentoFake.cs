using Ecommerce.Application.Vendas;

namespace Ecommerce.Infrastructure.Vendas;

/// <summary>
/// Gateway de pagamento fake — não chama nenhum serviço externo de verdade. Só existe pra deixar
/// o fluxo de checkout completo e testável enquanto o gateway real não é decidido
/// (Stripe/Mercado Pago/PagBank — ver seção 9 do plano). A "confirmação" chega via
/// PagamentosController.Webhook usando a mesma referência gerada aqui — trocar pelo gateway real é
/// só uma nova implementação de <see cref="IServicoDePagamento"/>, sem tocar em Carrinho/Pedido.
/// </summary>
public class ServicoDePagamentoFake : IServicoDePagamento
{
    public Task<string> IniciarCobrancaAsync(Guid pedidoId, decimal valor, CancellationToken ct = default) =>
        Task.FromResult($"fake_{pedidoId:N}_{Guid.NewGuid():N}");
}
