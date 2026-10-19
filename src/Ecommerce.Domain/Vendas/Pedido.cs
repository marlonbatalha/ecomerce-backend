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

    /// <summary>Código do <see cref="Cupom"/> aplicado (snapshot — o cupom pode mudar depois).</summary>
    public string? CodigoCupom { get; private set; }

    public decimal Desconto { get; private set; }

    /// <summary>Pra onde o pedido vai. Null só em pedidos antigos, de antes da entrega existir no checkout.</summary>
    public EnderecoDeEntrega? Entrega { get; private set; }

    /// <summary>Nome do método escolhido (ex.: "PAC", "SEDEX") — snapshot, como o preço dos itens.</summary>
    public string? MetodoDeEnvio { get; private set; }

    public decimal ValorFrete { get; private set; }

    public int? PrazoEntregaDias { get; private set; }

    public decimal Subtotal => _itens.Sum(i => i.Subtotal);

    /// <summary>O cupom desconta só dos produtos; o frete é cobrado inteiro.</summary>
    public decimal Total => Subtotal - Desconto + ValorFrete;

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

    /// <summary>
    /// Aplica o desconto de um cupom — só uma vez e só enquanto o pedido está pendente (o valor já
    /// foi calculado/validado por <see cref="Cupom.CalcularDesconto"/>).
    /// </summary>
    public void AplicarDesconto(string codigoCupom, decimal desconto)
    {
        if (Status != StatusPedido.Pendente)
        {
            throw new ExcecaoDeDominio("Só é possível aplicar desconto a um pedido pendente.");
        }

        if (CodigoCupom is not null)
        {
            throw new ExcecaoDeDominio("O pedido já tem um cupom aplicado.");
        }

        if (desconto <= 0 || desconto > Subtotal)
        {
            throw new ExcecaoDeDominio("O desconto precisa ser maior que zero e no máximo o subtotal do pedido.");
        }

        CodigoCupom = codigoCupom;
        Desconto = desconto;
    }

    /// <summary>
    /// Define endereço e frete — uma vez só, enquanto pendente. O valor vem do cálculo de frete do
    /// servidor (<c>IServicoDeFrete</c>), nunca do que o cliente mandou.
    /// </summary>
    public void DefinirEntrega(EnderecoDeEntrega endereco, string metodoDeEnvio, decimal valorFrete, int prazoEntregaDias)
    {
        if (Status != StatusPedido.Pendente)
        {
            throw new ExcecaoDeDominio("Só é possível definir a entrega de um pedido pendente.");
        }

        if (Entrega is not null)
        {
            throw new ExcecaoDeDominio("A entrega do pedido já foi definida.");
        }

        if (string.IsNullOrWhiteSpace(metodoDeEnvio))
        {
            throw new ExcecaoDeDominio("O método de envio é obrigatório.");
        }

        if (valorFrete < 0)
        {
            throw new ExcecaoDeDominio("O valor do frete não pode ser negativo.");
        }

        if (prazoEntregaDias < 0)
        {
            throw new ExcecaoDeDominio("O prazo de entrega não pode ser negativo.");
        }

        Entrega = endereco ?? throw new ExcecaoDeDominio("O endereço de entrega é obrigatório.");
        MetodoDeEnvio = metodoDeEnvio.Trim();
        ValorFrete = valorFrete;
        PrazoEntregaDias = prazoEntregaDias;
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
