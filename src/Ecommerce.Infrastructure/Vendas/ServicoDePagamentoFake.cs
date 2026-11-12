using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Vendas;

namespace Ecommerce.Infrastructure.Vendas;

/// <summary>
/// Gateway de pagamento fake: não chama nenhum serviço externo (dev/testes, ou enquanto não há
/// credenciais do Mercado Pago). Devolve instruções com cara de verdade (código Pix, linha
/// digitável) para o frontend ter o que mostrar, mas nenhuma delas paga nada. A "confirmação"
/// chega via <c>PagamentosController.Webhook</c> (HMAC próprio) com a referência gerada aqui.
/// </summary>
public class ServicoDePagamentoFake(TimeProvider relogio) : IServicoDePagamento
{
    public Task<CobrancaIniciada> IniciarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default)
    {
        var referencia = $"fake_{solicitacao.PedidoId:N}_{Guid.NewGuid():N}";
        var agora = relogio.GetUtcNow().UtcDateTime;

        var instrucoes = solicitacao.Metodo == MetodoDePagamento.Boleto
            ? new InstrucoesDaCobranca(
                LinhaDigitavel: "00190.00009 01234.567004 00000.000000 1 00000000000000",
                ExpiraEmUtc: agora.AddDays(3))
            : new InstrucoesDaCobranca(
                CodigoPix: $"00020126580014br.gov.bcb.pix0136{solicitacao.PedidoId}5204000053039865802BR5913LOJA DE TESTE6009SAO PAULO6304FAKE",
                ExpiraEmUtc: agora.AddMinutes(30));

        return Task.FromResult(new CobrancaIniciada(referencia, instrucoes));
    }

    /// <summary>O fake não tem pagamentos "do lado de lá" pra consultar.</summary>
    public Task<PagamentoConsultado?> ConsultarPagamentoAsync(string idTransacao, CancellationToken ct = default) =>
        Task.FromResult<PagamentoConsultado?>(null);
}
