using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Registro de uma cobrança junto ao gateway de pagamento (Mercado Pago — ou o fake em dev, ver
/// <c>IServicoDePagamento</c>). Guarda só referências externas e o status — nunca dado de cartão
/// (isso fica 100% no gateway). <see cref="Aprovar"/>/<see cref="Recusar"/>/<see cref="Estornar"/>
/// são idempotentes: reaplicar o mesmo resultado não lança erro (o gateway pode reenviar o mesmo
/// evento de webhook).
/// </summary>
public class Pagamento : Entidade
{
    public const int TamanhoMaximoReferenciaExterna = 200;

    public const int TamanhoMaximoUrlPagamento = 1000;

    public Guid PedidoId { get; private set; }

    /// <summary>Id da cobrança no gateway (no Mercado Pago, o id da "preference" do Checkout Pro).</summary>
    public string ReferenciaExterna { get; private set; } = null!;

    /// <summary>Página do gateway onde o cliente paga — null quando o gateway não tem uma (fake).</summary>
    public string? UrlPagamento { get; private set; }

    /// <summary>Id do pagamento efetivo no gateway — só conhecido quando o webhook chega.</summary>
    public string? IdTransacao { get; private set; }

    public decimal Valor { get; private set; }

    public StatusPagamento Status { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    private Pagamento()
    {
    }

    private Pagamento(Guid id, Guid pedidoId, string referenciaExterna, string? urlPagamento, decimal valor)
        : base(id)
    {
        PedidoId = pedidoId;
        ReferenciaExterna = referenciaExterna;
        UrlPagamento = urlPagamento;
        Valor = valor;
        Status = StatusPagamento.Pendente;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static Pagamento Criar(Guid pedidoId, string referenciaExterna, decimal valor, string? urlPagamento = null)
    {
        if (string.IsNullOrWhiteSpace(referenciaExterna))
        {
            throw new ExcecaoDeDominio("A referência externa do pagamento é obrigatória.");
        }

        if (valor <= 0)
        {
            throw new ExcecaoDeDominio("O valor do pagamento precisa ser maior que zero.");
        }

        if (urlPagamento is { Length: > TamanhoMaximoUrlPagamento })
        {
            throw new ExcecaoDeDominio($"A URL de pagamento não pode ter mais de {TamanhoMaximoUrlPagamento} caracteres.");
        }

        return new Pagamento(Guid.NewGuid(), pedidoId, referenciaExterna.Trim(), urlPagamento, valor);
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
