using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Registro de uma cobrança junto ao gateway de pagamento (ainda não decidido — ver seção 9 do
/// plano; por ora atrás de <c>IServicoDePagamento</c>, com implementação fake). Guarda só a
/// referência externa e o status — nunca dado de cartão (isso fica 100% no gateway).
/// <see cref="Aprovar"/>/<see cref="Recusar"/>/<see cref="Estornar"/> são idempotentes: reaplicar
/// o mesmo resultado não lança erro (o gateway pode reenviar o mesmo evento de webhook).
/// </summary>
public class Pagamento : Entidade
{
    public const int TamanhoMaximoReferenciaExterna = 200;

    public Guid PedidoId { get; private set; }

    public string ReferenciaExterna { get; private set; } = null!;

    public decimal Valor { get; private set; }

    public StatusPagamento Status { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    private Pagamento()
    {
    }

    private Pagamento(Guid id, Guid pedidoId, string referenciaExterna, decimal valor)
        : base(id)
    {
        PedidoId = pedidoId;
        ReferenciaExterna = referenciaExterna;
        Valor = valor;
        Status = StatusPagamento.Pendente;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static Pagamento Criar(Guid pedidoId, string referenciaExterna, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(referenciaExterna))
        {
            throw new ExcecaoDeDominio("A referência externa do pagamento é obrigatória.");
        }

        if (valor <= 0)
        {
            throw new ExcecaoDeDominio("O valor do pagamento precisa ser maior que zero.");
        }

        return new Pagamento(Guid.NewGuid(), pedidoId, referenciaExterna.Trim(), valor);
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
