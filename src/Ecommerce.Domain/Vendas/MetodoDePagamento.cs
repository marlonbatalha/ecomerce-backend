namespace Ecommerce.Domain.Vendas;

/// <summary>Como o cliente paga. Os dois são assíncronos: o pedido fica pendente até o gateway confirmar.</summary>
public enum MetodoDePagamento
{
    Pix,
    Boleto,
}
