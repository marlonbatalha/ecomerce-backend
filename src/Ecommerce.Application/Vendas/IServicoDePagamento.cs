using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

/// <summary>
/// Abstração sobre o gateway de pagamento. Implementações em Infrastructure, escolhidas por
/// <c>Pagamentos:Provedor</c>: <c>ServicoDePagamentoMercadoPago</c> (checkout transparente: Pix e
/// boleto criados pela API e exibidos na própria loja) ou <c>ServicoDePagamentoFake</c> (dev/testes,
/// não chama nada externo). Trocar de gateway é uma implementação nova desta interface.
/// </summary>
public interface IServicoDePagamento
{
    /// <summary>Cria a cobrança no gateway e devolve a referência dela e as instruções para pagar.</summary>
    Task<CobrancaIniciada> IniciarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default);

    /// <summary>
    /// Consulta um pagamento direto no gateway (o webhook só diz "algo mudou no pagamento X"; o
    /// status confiável vem desta consulta). Null se o pagamento não existir no gateway.
    /// </summary>
    Task<PagamentoConsultado?> ConsultarPagamentoAsync(string idTransacao, CancellationToken ct = default);
}

public record SolicitacaoDeCobranca(
    Guid PedidoId,
    decimal Valor,
    string Descricao,
    MetodoDePagamento Metodo,
    PagadorDaCobranca Pagador);

/// <summary>
/// Quem paga. O gateway exige e-mail e CPF para Pix e boleto; o boleto também exige nome e
/// endereço (o de entrega do pedido).
/// </summary>
/// <param name="Cpf">Só os 11 dígitos.</param>
public record PagadorDaCobranca(string Email, string Nome, string Cpf, EnderecoDoPagador Endereco);

public record EnderecoDoPagador(string Cep, string Logradouro, string Numero, string Bairro, string Cidade, string Uf);

public record CobrancaIniciada(string ReferenciaExterna, InstrucoesDaCobranca Instrucoes);

public record PagamentoConsultado(Guid PedidoId, string IdTransacao, ResultadoDePagamento Resultado);

/// <summary>Status do gateway já traduzido pro que importa ao pedido.</summary>
public enum ResultadoDePagamento
{
    /// <summary>Ainda aguardando o cliente pagar.</summary>
    Pendente,
    Aprovado,
    Recusado,
    Estornado,
}
