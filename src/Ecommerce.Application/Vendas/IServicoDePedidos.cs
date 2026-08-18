namespace Ecommerce.Application.Vendas;

/// <summary>
/// Casos de uso de pedido: checkout, consulta, transições de status (admin) e processamento do
/// webhook de pagamento. Implementado em Infrastructure.
/// </summary>
public interface IServicoDePedidos
{
    /// <summary>
    /// Cria o pedido a partir do carrinho (snapshot de preço/nome, débito de estoque com
    /// concorrência otimista) e inicia a cobrança junto ao gateway. Esvazia o carrinho ao final.
    /// </summary>
    Task<CheckoutResultado> FinalizarCompraAsync(FinalizarCompraComando comando, CancellationToken ct = default);

    /// <summary>Só retorna o pedido se pertencer a <paramref name="usuarioId"/> — evita IDOR entre clientes.</summary>
    Task<PedidoDto?> ObterDoUsuarioAsync(Guid pedidoId, Guid usuarioId, CancellationToken ct = default);

    Task<IReadOnlyCollection<PedidoResumoDto>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task<PedidoDto> CancelarAsync(Guid pedidoId, Guid usuarioId, CancellationToken ct = default);

    Task<PedidoDto> MarcarComoEnviadoAsync(Guid pedidoId, string codigoRastreio, CancellationToken ct = default);

    Task<PedidoDto> MarcarComoEntregueAsync(Guid pedidoId, CancellationToken ct = default);

    /// <summary>Chamado pelo webhook de pagamento — idempotente (reenvio do mesmo evento não falha).</summary>
    Task ProcessarConfirmacaoDePagamentoAsync(string referenciaExterna, bool aprovado, CancellationToken ct = default);
}
