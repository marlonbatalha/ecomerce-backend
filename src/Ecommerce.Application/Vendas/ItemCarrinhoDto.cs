namespace Ecommerce.Application.Vendas;

/// <summary>
/// Nome/preço lidos em tempo real do catálogo — nunca vêm do próprio carrinho.
/// <paramref name="EstoqueDisponivel"/> deixa a tela limitar a quantidade (a API revalida de
/// qualquer jeito); <paramref name="ProdutoTemOpcoes"/> diz se vale mostrar o nome da variante
/// (produto de variante única não tem o que distinguir).
/// </summary>
public record ItemCarrinhoDto(
    Guid VarianteDeProdutoId,
    string NomeProduto,
    string NomeVariante,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal,
    string ProdutoSlug,
    string? ImagemUrl,
    int EstoqueDisponivel,
    bool ProdutoTemOpcoes);
