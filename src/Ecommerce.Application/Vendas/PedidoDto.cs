using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

public record PedidoDto(
    Guid Id,
    StatusPedido Status,
    DateTime CriadoEmUtc,
    string? CodigoRastreio,
    decimal Total,
    IReadOnlyCollection<ItemPedidoDto> Itens);
