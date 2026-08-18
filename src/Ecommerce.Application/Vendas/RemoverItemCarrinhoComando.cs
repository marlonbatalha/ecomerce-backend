namespace Ecommerce.Application.Vendas;

public record RemoverItemCarrinhoComando(Guid CarrinhoId, Guid VarianteDeProdutoId);
