using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Pedido confirmado a partir de um carrinho, com snapshot de preço/nome dos itens. Raiz do
/// agregado que inclui <see cref="ItemDoPedido"/>. Transições de <see cref="StatusPedido"/> são
/// restritas — ver <see cref="TransicoesPermitidas"/> e a seção "Fase 3" do plano do projeto.
/// </summary>
public class Pedido : Entidade
{
    private static readonly Dictionary<StatusPedido, StatusPedido[]> TransicoesPermitidas = new()
    {
        [StatusPedido.Pendente] = [StatusPedido.PagamentoConfirmado, StatusPedido.PagamentoRecusado, StatusPedido.Cancelado],
        [StatusPedido.PagamentoConfirmado] = [StatusPedido.Enviado, StatusPedido.Cancelado, StatusPedido.Reembolsado],
        [StatusPedido.Enviado] = [StatusPedido.Entregue, StatusPedido.Reembolsado],
        [StatusPedido.Entregue] = [StatusPedido.Reembolsado],
        [StatusPedido.PagamentoRecusado] = [],
        [StatusPedido.Cancelado] = [],
        [StatusPedido.Reembolsado] = [],
    };

    private readonly List<ItemDoPedido> _itens = [];

    public Guid UsuarioId { get; private set; }

    public StatusPedido Status { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public string? CodigoRastreio { get; private set; }

    public IReadOnlyCollection<ItemDoPedido> Itens => _itens.AsReadOnly();

    public decimal Total => _itens.Sum(i => i.Subtotal);

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private Pedido()
    {
    }

    private Pedido(Guid id, Guid usuarioId)
        : base(id)
    {
        UsuarioId = usuarioId;
        Status = StatusPedido.Pendente;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static Pedido Criar(Guid usuarioId, IReadOnlyCollection<ItemPedidoRascunho> itens)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O pedido precisa estar associado a um usuário.");
        }

        if (itens.Count == 0)
        {
            throw new ExcecaoDeDominio("O pedido precisa ter ao menos um item.");
        }

        var pedido = new Pedido(Guid.NewGuid(), usuarioId);

        foreach (var item in itens)
        {
            pedido._itens.Add(ItemDoPedido.Criar(pedido.Id, item.VarianteDeProdutoId, item.NomeProduto, item.PrecoUnitario, item.Quantidade));
        }

        return pedido;
    }

    public void ConfirmarPagamento() => TransicionarPara(StatusPedido.PagamentoConfirmado);

    public void RecusarPagamento() => TransicionarPara(StatusPedido.PagamentoRecusado);

    public void Cancelar() => TransicionarPara(StatusPedido.Cancelado);

    public void MarcarComoEnviado(string codigoRastreio)
    {
        if (string.IsNullOrWhiteSpace(codigoRastreio))
        {
            throw new ExcecaoDeDominio("O código de rastreio é obrigatório.");
        }

        TransicionarPara(StatusPedido.Enviado);
        CodigoRastreio = codigoRastreio.Trim();
    }

    public void MarcarComoEntregue() => TransicionarPara(StatusPedido.Entregue);

    public void Reembolsar() => TransicionarPara(StatusPedido.Reembolsado);

    private void TransicionarPara(StatusPedido novoStatus)
    {
        if (!TransicoesPermitidas[Status].Contains(novoStatus))
        {
            throw new ExcecaoDeDominio($"Não é possível mudar o pedido de '{Status}' para '{novoStatus}'.");
        }

        Status = novoStatus;
    }
}
