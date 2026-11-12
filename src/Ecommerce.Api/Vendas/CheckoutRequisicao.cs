namespace Ecommerce.Api.Vendas;

/// <summary>
/// Endereço (um dos salvos na conta), método de envio e método de pagamento são obrigatórios; cupom
/// é opcional. O valor do frete não vem daqui: o servidor recalcula pelo CEP e pelos itens.
/// </summary>
/// <param name="MetodoDePagamento">"Pix" ou "Boleto" (sem diferenciar maiúsculas).</param>
public record CheckoutRequisicao(Guid EnderecoId, string MetodoDeEnvio, string MetodoDePagamento, string? CodigoCupom);
