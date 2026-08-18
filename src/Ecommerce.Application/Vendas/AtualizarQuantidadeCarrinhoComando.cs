namespace Ecommerce.Application.Vendas;

public record AtualizarQuantidadeCarrinhoComando(Guid CarrinhoId, Guid VarianteDeProdutoId, int Quantidade);
