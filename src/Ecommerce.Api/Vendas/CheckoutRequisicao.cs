namespace Ecommerce.Api.Vendas;

/// <summary>
/// Endereço (um dos salvos na conta) e método de envio são obrigatórios; cupom é opcional. O valor
/// do frete não vem daqui — o servidor recalcula pelo CEP do endereço e pelos itens do carrinho.
/// </summary>
public record CheckoutRequisicao(Guid EnderecoId, string MetodoDeEnvio, string? CodigoCupom);
