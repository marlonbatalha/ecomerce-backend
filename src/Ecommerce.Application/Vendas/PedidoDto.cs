using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

/// <summary>
/// <paramref name="UrlPagamento"/> permite ao cliente retomar o pagamento de um pedido ainda pendente.
/// <paramref name="Entrega"/> é null só em pedidos de antes da entrega existir no checkout.
/// </summary>
public record PedidoDto(
    Guid Id,
    StatusPedido Status,
    DateTime CriadoEmUtc,
    string? CodigoRastreio,
    decimal Subtotal,
    decimal Desconto,
    string? CodigoCupom,
    decimal Total,
    IReadOnlyCollection<ItemPedidoDto> Itens,
    EnderecoDeEntregaDto? Entrega,
    string? MetodoDeEnvio,
    decimal ValorFrete,
    int? PrazoEntregaDias,
    string? UrlPagamento = null);

public record EnderecoDeEntregaDto(
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep);
