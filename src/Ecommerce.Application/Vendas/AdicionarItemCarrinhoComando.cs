namespace Ecommerce.Application.Vendas;

public record AdicionarItemCarrinhoComando(Guid CarrinhoId, Guid VarianteDeProdutoId, int Quantidade);
