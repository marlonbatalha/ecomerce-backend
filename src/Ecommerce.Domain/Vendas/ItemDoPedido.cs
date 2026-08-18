using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Item de um <see cref="Pedido"/> — snapshot de nome/preço do produto no momento da compra
/// (pedido não muda de valor se o produto for repreçado depois). Só criado via
/// <see cref="Pedido.Criar"/> (factory <c>internal</c>).
/// </summary>
public class ItemDoPedido : Entidade
{
    public const int TamanhoMaximoNomeProduto = 150;

    public Guid PedidoId { get; private set; }

    public Guid VarianteDeProdutoId { get; private set; }

    /// <summary>Snapshot — nome do produto no momento da compra.</summary>
    public string NomeProduto { get; private set; } = null!;

    /// <summary>Snapshot — preço unitário no momento da compra.</summary>
    public decimal PrecoUnitario { get; private set; }

    public int Quantidade { get; private set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;

    private ItemDoPedido()
    {
    }

    private ItemDoPedido(Guid id, Guid pedidoId, Guid varianteDeProdutoId, string nomeProduto, decimal precoUnitario, int quantidade)
        : base(id)
    {
        PedidoId = pedidoId;
        VarianteDeProdutoId = varianteDeProdutoId;
        NomeProduto = nomeProduto;
        PrecoUnitario = precoUnitario;
        Quantidade = quantidade;
    }

    internal static ItemDoPedido Criar(Guid pedidoId, Guid varianteDeProdutoId, string nomeProduto, decimal precoUnitario, int quantidade)
    {
        if (string.IsNullOrWhiteSpace(nomeProduto))
        {
            throw new ExcecaoDeDominio("O nome do produto é obrigatório no item do pedido.");
        }

        if (precoUnitario < 0)
        {
            throw new ExcecaoDeDominio("O preço unitário do item não pode ser negativo.");
        }

        if (quantidade <= 0)
        {
            throw new ExcecaoDeDominio("A quantidade do item precisa ser maior que zero.");
        }

        return new ItemDoPedido(Guid.NewGuid(), pedidoId, varianteDeProdutoId, nomeProduto.Trim(), precoUnitario, quantidade);
    }
}
