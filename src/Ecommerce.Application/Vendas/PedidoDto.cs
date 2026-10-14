using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

/// <summary><paramref name="UrlPagamento"/> permite ao cliente retomar o pagamento de um pedido ainda pendente.</summary>
public record PedidoDto(
    Guid Id,
    StatusPedido Status,
    DateTime CriadoEmUtc,
    string? CodigoRastreio,
    decimal Subtotal,
    decimal Desconto,
    string? CodigoCupom,
    decimal Total,
    IReadOnlyCollection<ItemPedidoDto> Itens,
    string? UrlPagamento = null);
