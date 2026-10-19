namespace Ecommerce.Application.Vendas;

/// <param name="EnderecoId">Endereço da conta do próprio usuário — o pedido guarda uma cópia.</param>
/// <param name="MetodoDeEnvio">Nome de uma das opções de <see cref="IServicoDeFrete"/> (ex.: "PAC"); o valor é recalculado no servidor.</param>
public record FinalizarCompraComando(
    Guid UsuarioId,
    Guid CarrinhoId,
    Guid EnderecoId,
    string MetodoDeEnvio,
    string? CodigoCupom = null);
