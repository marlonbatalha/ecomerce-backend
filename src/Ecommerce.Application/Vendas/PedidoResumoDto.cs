using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

public record PedidoResumoDto(Guid Id, Guid UsuarioId, StatusPedido Status, DateTime CriadoEmUtc, decimal Total, int QuantidadeDeItens);
