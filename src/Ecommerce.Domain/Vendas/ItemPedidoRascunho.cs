namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Dados já resolvidos (nome/preço lidos do catálogo no momento do checkout) usados por
/// <see cref="Pedido.Criar"/> pra montar o snapshot de cada item — Pedido não conhece Produto.
/// </summary>
public record ItemPedidoRascunho(Guid VarianteDeProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade);
