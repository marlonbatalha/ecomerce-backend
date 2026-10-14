namespace Ecommerce.Application.Vendas;

/// <summary>
/// <paramref name="ReferenciaPagamento"/> é a referência da cobrança no gateway.
/// <paramref name="UrlPagamento"/> é pra onde o frontend redireciona o cliente pagar (Checkout Pro
/// do Mercado Pago) — null quando não há o que pagar (cupom de 100%) ou com o gateway fake.
/// </summary>
public record CheckoutResultado(PedidoDto Pedido, string? ReferenciaPagamento, string? UrlPagamento);
