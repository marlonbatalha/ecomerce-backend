using Ecommerce.Application.Vendas;

namespace Ecommerce.Infrastructure.Vendas;

/// <summary>
/// Gateway de pagamento fake — não chama nenhum serviço externo (dev/testes, ou enquanto não há
/// credenciais do Mercado Pago). A "confirmação" chega via <c>PagamentosController.Webhook</c>
/// (esquema HMAC próprio) usando a mesma referência gerada aqui.
/// </summary>
public class ServicoDePagamentoFake : IServicoDePagamento
{
    public Task<CobrancaIniciada> IniciarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default) =>
        Task.FromResult(new CobrancaIniciada($"fake_{solicitacao.PedidoId:N}_{Guid.NewGuid():N}", UrlPagamento: null));

    /// <summary>O fake não tem pagamentos "do lado de lá" pra consultar.</summary>
    public Task<PagamentoConsultado?> ConsultarPagamentoAsync(string idTransacao, CancellationToken ct = default) =>
        Task.FromResult<PagamentoConsultado?>(null);
}
