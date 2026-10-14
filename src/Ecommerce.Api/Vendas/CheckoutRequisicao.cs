namespace Ecommerce.Api.Vendas;

/// <summary>Corpo opcional do checkout — sem corpo, finaliza sem cupom.</summary>
public record CheckoutRequisicao(string? CodigoCupom);
