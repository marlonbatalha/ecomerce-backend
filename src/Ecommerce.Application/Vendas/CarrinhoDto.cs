namespace Ecommerce.Application.Vendas;

public record CarrinhoDto(Guid Id, IReadOnlyCollection<ItemCarrinhoDto> Itens, decimal Total);
