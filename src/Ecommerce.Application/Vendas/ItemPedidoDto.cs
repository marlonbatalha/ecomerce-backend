namespace Ecommerce.Application.Vendas;

public record ItemPedidoDto(Guid VarianteDeProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade, decimal Subtotal);
