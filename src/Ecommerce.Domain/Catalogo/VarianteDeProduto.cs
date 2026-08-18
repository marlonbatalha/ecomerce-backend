using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Catalogo;

/// <summary>
/// Variação de um <see cref="Produto"/> (ex.: tamanho/cor), com estoque e preço próprios.
/// Só é criada através de <see cref="Produto.AdicionarVariante"/> — o construtor/factory é
/// <c>internal</c> de propósito: <see cref="Produto"/> é a raiz do agregado, é quem garante a
/// invariante "produto não pode ficar sem nenhuma variante ativa" (ver <see cref="Produto.InativarVariante"/>).
/// </summary>
public class VarianteDeProduto : Entidade
{
    public const int TamanhoMaximoNome = 100;

    public const int TamanhoMaximoSku = 50;

    public Guid ProdutoId { get; private set; }

    public string Nome { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    public bool Ativo { get; private set; } = true;

    private VarianteDeProduto()
    {
    }

    private VarianteDeProduto(Guid id, Guid produtoId, string nome, string sku, decimal preco, int estoque)
        : base(id)
    {
        ProdutoId = produtoId;
        Nome = nome;
        Sku = sku;
        Preco = preco;
        Estoque = estoque;
    }

    internal static VarianteDeProduto Criar(Guid produtoId, string nome, string sku, decimal preco, int estoque)
    {
        GarantirNomeValido(nome);
        GarantirSkuValido(sku);
        GarantirPrecoValido(preco);
        GarantirEstoqueValido(estoque);

        return new VarianteDeProduto(Guid.NewGuid(), produtoId, nome.Trim(), NormalizarSku(sku), preco, estoque);
    }

    public void AtualizarPreco(decimal novoPreco)
    {
        GarantirPrecoValido(novoPreco);
        Preco = novoPreco;
    }

    public void AjustarEstoque(int novoEstoque)
    {
        GarantirEstoqueValido(novoEstoque);
        Estoque = novoEstoque;
    }

    internal void Inativar() => Ativo = false;

    internal void Ativar() => Ativo = true;

    private static void GarantirNomeValido(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ExcecaoDeDominio("O nome da variante é obrigatório.");
        }

        if (nome.Trim().Length > TamanhoMaximoNome)
        {
            throw new ExcecaoDeDominio($"O nome da variante não pode ter mais de {TamanhoMaximoNome} caracteres.");
        }
    }

    private static void GarantirSkuValido(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ExcecaoDeDominio("O SKU da variante é obrigatório.");
        }

        if (sku.Trim().Length > TamanhoMaximoSku)
        {
            throw new ExcecaoDeDominio($"O SKU da variante não pode ter mais de {TamanhoMaximoSku} caracteres.");
        }
    }

    private static void GarantirPrecoValido(decimal preco)
    {
        if (preco < 0)
        {
            throw new ExcecaoDeDominio("O preço da variante não pode ser negativo.");
        }
    }

    private static void GarantirEstoqueValido(int estoque)
    {
        if (estoque < 0)
        {
            throw new ExcecaoDeDominio("O estoque da variante não pode ser negativo.");
        }
    }

    private static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();
}
