namespace Ecommerce.Application.Vendas;

/// <summary>Nome/preço lidos em tempo real do catálogo — nunca vêm do próprio carrinho.</summary>
public record ItemCarrinhoDto(
    Guid VarianteDeProdutoId,
    string NomeProduto,
    string NomeVariante,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal);
