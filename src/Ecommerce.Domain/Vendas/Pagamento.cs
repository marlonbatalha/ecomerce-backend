using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Registro de uma cobrança Pix ou boleto junto ao gateway (Mercado Pago, ou o fake em dev; ver
/// <c>IServicoDePagamento</c>). Guarda as referências externas, o status e o que o cliente precisa
/// para pagar (código Pix copia e cola, linha digitável, link do boleto, validade). <see cref="Aprovar"/>/<see cref="Recusar"/>/<see cref="Estornar"/>
/// são idempotentes: reaplicar o mesmo resultado não lança erro (o gateway pode reenviar o mesmo
/// evento de webhook).
/// </summary>
public class Pagamento : Entidade
{
    public const int TamanhoMaximoReferenciaExterna = 200;

    public const int TamanhoMaximoUrlPagamento = 1000;

    /// <summary>O "copia e cola" do Pix (padrão EMV do Banco Central) costuma ter 200 a 500 caracteres.</summary>
    public const int TamanhoMaximoCodigoPix = 1000;

    public const int TamanhoMaximoLinhaDigitavel = 100;

    public Guid PedidoId { get; private set; }

    public MetodoDePagamento Metodo { get; private set; }

    /// <summary>Id da cobrança no gateway (no Mercado Pago, o id do pagamento em <c>/v1/payments</c>).</summary>
    public string ReferenciaExterna { get; private set; } = null!;

    /// <summary>Página do gateway com as instruções (QR do Pix ou o boleto). Null no gateway fake.</summary>
    public string? UrlPagamento { get; private set; }

    /// <summary>Pix "copia e cola"; o QR Code é a imagem deste mesmo texto. Só em Pix.</summary>
    public string? CodigoPix { get; private set; }

    /// <summary>Linha digitável do boleto. Só em boleto, e só quando o gateway a informa.</summary>
    public string? LinhaDigitavel { get; private set; }

    /// <summary>Depois disso o gateway cancela a cobrança e o pedido é recusado (estoque volta).</summary>
    public DateTime? ExpiraEmUtc { get; private set; }

    /// <summary>Id do pagamento efetivo no gateway. No Mercado Pago é o mesmo da referência externa.</summary>
    public string? IdTransacao { get; private set; }

    public decimal Valor { get; private set; }

    public StatusPagamento Status { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    private Pagamento()
    {
    }

    private Pagamento(Guid id, Guid pedidoId, MetodoDePagamento metodo, string referenciaExterna, decimal valor, InstrucoesDaCobranca instrucoes)
        : base(id)
    {
        PedidoId = pedidoId;
        Metodo = metodo;
        ReferenciaExterna = referenciaExterna;
        UrlPagamento = instrucoes.UrlPagamento;
        CodigoPix = instrucoes.CodigoPix;
        LinhaDigitavel = instrucoes.LinhaDigitavel;
        ExpiraEmUtc = instrucoes.ExpiraEmUtc;
        Valor = valor;
        Status = StatusPagamento.Pendente;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static Pagamento Criar(
        Guid pedidoId, MetodoDePagamento metodo, string referenciaExterna, decimal valor, InstrucoesDaCobranca? instrucoes = null)
    {
        instrucoes ??= InstrucoesDaCobranca.Nenhuma;

        if (string.IsNullOrWhiteSpace(referenciaExterna))
        {
            throw new ExcecaoDeDominio("A referência externa do pagamento é obrigatória.");
        }

        if (valor <= 0)
        {
            throw new ExcecaoDeDominio("O valor do pagamento precisa ser maior que zero.");
        }

        if (instrucoes.UrlPagamento is { Length: > TamanhoMaximoUrlPagamento })
        {
            throw new ExcecaoDeDominio($"A URL de pagamento não pode ter mais de {TamanhoMaximoUrlPagamento} caracteres.");
        }

        if (instrucoes.CodigoPix is { Length: > TamanhoMaximoCodigoPix })
        {
            throw new ExcecaoDeDominio($"O código Pix não pode ter mais de {TamanhoMaximoCodigoPix} caracteres.");
        }

        if (instrucoes.LinhaDigitavel is { Length: > TamanhoMaximoLinhaDigitavel })
        {
            throw new ExcecaoDeDominio($"A linha digitável não pode ter mais de {TamanhoMaximoLinhaDigitavel} caracteres.");
        }

        return new Pagamento(Guid.NewGuid(), pedidoId, metodo, referenciaExterna.Trim(), valor, instrucoes);
    }

    public void RegistrarTransacao(string idTransacao)
    {
        if (!string.IsNullOrWhiteSpace(idTransacao))
        {
            IdTransacao = idTransacao.Trim();
        }
    }

    public void Aprovar()
    {
        if (Status == StatusPagamento.Aprovado)
        {
            return;
        }

        if (Status != StatusPagamento.Pendente)
        {
            throw new ExcecaoDeDominio($"Não é possível aprovar um pagamento com status '{Status}'.");
        }

        Status = StatusPagamento.Aprovado;
    }

    public void Recusar()
    {
        if (Status == StatusPagamento.Recusado)
        {
            return;
        }

        if (Status != StatusPagamento.Pendente)
        {
            throw new ExcecaoDeDominio($"Não é possível recusar um pagamento com status '{Status}'.");
        }

        Status = StatusPagamento.Recusado;
    }

    public void Estornar()
    {
        if (Status == StatusPagamento.Estornado)
        {
            return;
        }

        if (Status != StatusPagamento.Aprovado)
        {
            throw new ExcecaoDeDominio($"Só é possível estornar um pagamento aprovado (status atual: '{Status}').");
        }

        Status = StatusPagamento.Estornado;
    }
}

/// <summary>O que o cliente precisa para pagar, devolvido pelo gateway ao criar a cobrança.</summary>
public record InstrucoesDaCobranca(
    string? UrlPagamento = null,
    string? CodigoPix = null,
    string? LinhaDigitavel = null,
    DateTime? ExpiraEmUtc = null)
{
    public static readonly InstrucoesDaCobranca Nenhuma = new();
}
