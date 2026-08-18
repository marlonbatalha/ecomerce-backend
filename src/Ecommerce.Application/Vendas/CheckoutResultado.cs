namespace Ecommerce.Application.Vendas;

/// <summary>
/// <paramref name="ReferenciaPagamento"/> é a referência externa junto ao gateway — com um
/// gateway real, o cliente normalmente usaria isso (ou uma URL derivada) pra completar o
/// pagamento. Com o gateway fake atual (ver IServicoDePagamento), é só informativo.
/// </summary>
public record CheckoutResultado(PedidoDto Pedido, string ReferenciaPagamento);
