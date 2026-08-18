using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Item de um <see cref="Carrinho"/>. Só guarda quantidade — preço é sempre lido em tempo real do
/// catálogo (ver seção "Regras" da Fase 3 do plano), nunca fica "preso" aqui. Só é criado através
/// de <see cref="Carrinho.AdicionarItem"/> (factory <c>internal</c>) — Carrinho é a raiz do agregado.
/// </summary>
public class ItemDoCarrinho : Entidade
{
    public Guid CarrinhoId { get; private set; }

    public Guid VarianteDeProdutoId { get; private set; }

    public int Quantidade { get; private set; }

    private ItemDoCarrinho()
    {
    }

    private ItemDoCarrinho(Guid id, Guid carrinhoId, Guid varianteDeProdutoId, int quantidade)
        : base(id)
    {
        CarrinhoId = carrinhoId;
        VarianteDeProdutoId = varianteDeProdutoId;
        Quantidade = quantidade;
    }

    internal static ItemDoCarrinho Criar(Guid carrinhoId, Guid varianteDeProdutoId, int quantidade)
    {
        GarantirQuantidadeValida(quantidade);
        return new ItemDoCarrinho(Guid.NewGuid(), carrinhoId, varianteDeProdutoId, quantidade);
    }

    internal void AtualizarQuantidade(int quantidade)
    {
        GarantirQuantidadeValida(quantidade);
        Quantidade = quantidade;
    }

    private static void GarantirQuantidadeValida(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ExcecaoDeDominio("A quantidade precisa ser maior que zero.");
        }
    }
}
