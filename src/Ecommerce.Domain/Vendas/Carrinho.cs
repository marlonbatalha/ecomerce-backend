using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Carrinho de compras — de um usuário autenticado (<see cref="UsuarioId"/>) ou anônimo
/// (<see cref="IdSessao"/>, vindo de cookie — ver <c>CarrinhoController</c>). Nunca guarda preço:
/// quem exibe o carrinho lê o preço atual da variante (ver <see cref="ItemDoCarrinho"/>).
/// </summary>
public class Carrinho : Entidade
{
    private readonly List<ItemDoCarrinho> _itens = [];

    public Guid? UsuarioId { get; private set; }

    public string? IdSessao { get; private set; }

    public IReadOnlyCollection<ItemDoCarrinho> Itens => _itens.AsReadOnly();

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private Carrinho()
    {
    }

    private Carrinho(Guid id, Guid? usuarioId, string? idSessao)
        : base(id)
    {
        UsuarioId = usuarioId;
        IdSessao = idSessao;
    }

    public static Carrinho CriarParaUsuario(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O carrinho precisa estar associado a um usuário.");
        }

        return new Carrinho(Guid.NewGuid(), usuarioId, null);
    }

    public static Carrinho CriarAnonimo(string idSessao)
    {
        if (string.IsNullOrWhiteSpace(idSessao))
        {
            throw new ExcecaoDeDominio("O carrinho anônimo precisa de um identificador de sessão.");
        }

        return new Carrinho(Guid.NewGuid(), null, idSessao.Trim());
    }

    /// <summary>
    /// Adiciona (ou soma à quantidade existente) um item. Retorna o item recém-criado quando é
    /// a primeira vez que a variante entra no carrinho — <c>null</c> quando só atualizou a
    /// quantidade de um item já existente. O chamador (Infrastructure) usa isso pra saber se
    /// precisa rastrear a entidade nova explicitamente no EF Core (ver ServicoCarrinho).
    /// </summary>
    public ItemDoCarrinho? AdicionarItem(Guid varianteDeProdutoId, int quantidade, int estoqueDisponivel)
    {
        var itemExistente = _itens.FirstOrDefault(i => i.VarianteDeProdutoId == varianteDeProdutoId);
        if (itemExistente is not null)
        {
            var novaQuantidade = itemExistente.Quantidade + quantidade;
            GarantirDentroDoEstoque(novaQuantidade, estoqueDisponivel);
            itemExistente.AtualizarQuantidade(novaQuantidade);
            return null;
        }

        GarantirDentroDoEstoque(quantidade, estoqueDisponivel);
        var novoItem = ItemDoCarrinho.Criar(Id, varianteDeProdutoId, quantidade);
        _itens.Add(novoItem);
        return novoItem;
    }

    public void AtualizarQuantidade(Guid varianteDeProdutoId, int quantidade, int estoqueDisponivel)
    {
        GarantirDentroDoEstoque(quantidade, estoqueDisponivel);
        ObterItemOuFalhar(varianteDeProdutoId).AtualizarQuantidade(quantidade);
    }

    public ItemDoCarrinho RemoverItem(Guid varianteDeProdutoId)
    {
        var item = ObterItemOuFalhar(varianteDeProdutoId);
        _itens.Remove(item);
        return item;
    }

    /// <summary>Esvazia o carrinho (ex.: pós-checkout) — retorna os itens removidos pro chamador remover do EF explicitamente.</summary>
    public IReadOnlyCollection<ItemDoCarrinho> Esvaziar()
    {
        var itensRemovidos = _itens.ToList();
        _itens.Clear();
        return itensRemovidos;
    }

    /// <summary>
    /// Funde os itens de outro carrinho (tipicamente o anônimo) neste — usado no login (ver
    /// seção "Regras" da Fase 3). <paramref name="estoquesDisponiveis"/> precisa vir pré-carregado
    /// pelo chamador (Infrastructure) — o domínio não faz I/O. Retorna os itens novos criados aqui,
    /// pro chamador rastrear explicitamente no EF Core.
    /// </summary>
    public IReadOnlyCollection<ItemDoCarrinho> MesclarCom(Carrinho outro, IReadOnlyDictionary<Guid, int> estoquesDisponiveis)
    {
        var itensNovos = new List<ItemDoCarrinho>();

        foreach (var item in outro.Itens)
        {
            var estoqueDisponivel = estoquesDisponiveis.GetValueOrDefault(item.VarianteDeProdutoId, 0);
            var novoItem = AdicionarItem(item.VarianteDeProdutoId, item.Quantidade, estoqueDisponivel);
            if (novoItem is not null)
            {
                itensNovos.Add(novoItem);
            }
        }

        return itensNovos;
    }

    private ItemDoCarrinho ObterItemOuFalhar(Guid varianteDeProdutoId) =>
        _itens.FirstOrDefault(i => i.VarianteDeProdutoId == varianteDeProdutoId)
            ?? throw new ExcecaoDeDominio("Item não encontrado no carrinho.");

    private static void GarantirDentroDoEstoque(int quantidade, int estoqueDisponivel)
    {
        if (quantidade <= 0)
        {
            throw new ExcecaoDeDominio("A quantidade precisa ser maior que zero.");
        }

        if (quantidade > estoqueDisponivel)
        {
            throw new ExcecaoDeDominio("Quantidade solicitada maior que o estoque disponível.");
        }
    }
}
