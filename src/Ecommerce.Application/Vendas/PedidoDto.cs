using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

/// <summary>
/// <paramref name="UrlPagamento"/> e <paramref name="Pagamento"/> permitem ao cliente retomar o
/// pagamento de um pedido ainda pendente (só vêm preenchidos nesse caso).
/// <paramref name="Entrega"/> é null só em pedidos de antes da entrega existir no checkout.
/// <paramref name="UsuarioId"/> deixa o admin chegar no cliente (<c>GET /clientes/{id}</c>).
/// </summary>
public record PedidoDto(
    Guid Id,
    Guid UsuarioId,
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
    string? UrlPagamento = null,
    InstrucoesDePagamentoDto? Pagamento = null);

/// <summary>
/// O que a loja mostra para o cliente pagar. <paramref name="CodigoPix"/> é o "copia e cola" e
/// também o conteúdo do QR Code (o frontend desenha a imagem a partir dele).
/// </summary>
public record InstrucoesDePagamentoDto(
    MetodoDePagamento Metodo,
    string? CodigoPix,
    string? LinhaDigitavel,
    string? UrlPagamento,
    DateTime? ExpiraEmUtc);

public record EnderecoDeEntregaDto(
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep);
