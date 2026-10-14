namespace Ecommerce.Application.Vendas;

/// <summary>
/// Abstração sobre o gateway de pagamento. Implementações em Infrastructure, escolhidas por
/// <c>Pagamentos:Provedor</c>: <c>ServicoDePagamentoMercadoPago</c> (Checkout Pro — Pix, cartão e
/// boleto na página do Mercado Pago) ou <c>ServicoDePagamentoFake</c> (dev/testes, não chama nada
/// externo). Trocar de gateway é uma implementação nova desta interface, sem mudar Carrinho/Pedido.
/// </summary>
public interface IServicoDePagamento
{
    /// <summary>Cria a cobrança no gateway — devolve a referência dela e a página onde o cliente paga.</summary>
    Task<CobrancaIniciada> IniciarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default);

    /// <summary>
    /// Consulta um pagamento direto no gateway (o webhook só diz "algo mudou no pagamento X" — o
    /// status confiável vem desta consulta). Null se o pagamento não existir no gateway.
    /// </summary>
    Task<PagamentoConsultado?> ConsultarPagamentoAsync(string idTransacao, CancellationToken ct = default);
}

public record SolicitacaoDeCobranca(Guid PedidoId, decimal Valor, string Descricao, string? EmailComprador);

public record CobrancaIniciada(string ReferenciaExterna, string? UrlPagamento);

public record PagamentoConsultado(Guid PedidoId, string IdTransacao, ResultadoDePagamento Resultado);

/// <summary>Status do gateway já traduzido pro que importa ao pedido.</summary>
public enum ResultadoDePagamento
{
    /// <summary>Ainda em andamento (ou recusado de um jeito que o cliente pode tentar de novo).</summary>
    Pendente,
    Aprovado,
    Recusado,
    Estornado,
}
