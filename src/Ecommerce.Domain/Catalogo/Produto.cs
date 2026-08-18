using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Catalogo;

/// <summary>
/// Produto do catálogo. É a raiz do agregado que inclui suas <see cref="VarianteDeProduto"/> e
/// <see cref="ImagemDeProduto"/> — variantes e imagens só existem através dos métodos daqui
/// (<see cref="AdicionarVariante"/>, <see cref="AdicionarImagem"/>), nunca soltas.
/// </summary>
public class Produto : Entidade
{
    public const int TamanhoMaximoNome = 150;

    public const int TamanhoMaximoDescricao = 4000;

    public const int TamanhoMaximoSku = 50;

    private readonly List<VarianteDeProduto> _variantes = [];
    private readonly List<ImagemDeProduto> _imagens = [];

    public string Nome { get; private set; } = null!;

    public string Descricao { get; private set; } = string.Empty;

    public string Sku { get; private set; } = null!;

    /// <summary>Preço-base do produto — cada variante pode ter o seu próprio (ver <see cref="VarianteDeProduto.Preco"/>).</summary>
    public decimal Preco { get; private set; }

    public Guid CategoriaId { get; private set; }

    public bool Ativo { get; private set; } = true;

    /// <summary>Usado pra ordenar por "mais recente" na busca — Guid não tem ordem cronológica.</summary>
    public DateTime CriadoEmUtc { get; private set; }

    public IReadOnlyCollection<VarianteDeProduto> Variantes => _variantes.AsReadOnly();

    public IReadOnlyCollection<ImagemDeProduto> Imagens => _imagens.AsReadOnly();

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private Produto()
    {
    }

    private Produto(Guid id, string nome, string descricao, string sku, decimal preco, Guid categoriaId)
        : base(id)
    {
        Nome = nome;
        Descricao = descricao;
        Sku = sku;
        Preco = preco;
        CategoriaId = categoriaId;
        CriadoEmUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cria o produto sem variantes — precisa de <see cref="AdicionarVariante"/> pelo menos uma vez
    /// antes de ficar disponível pra venda (a listagem pública filtra por variante ativa, não é
    /// invariante de construção: permite montar o cadastro em etapas no admin).
    /// </summary>
    public static Produto Criar(string nome, string descricao, string sku, decimal preco, Guid categoriaId)
    {
        GarantirNomeValido(nome);
        GarantirSkuValido(sku);
        GarantirPrecoValido(preco);

        return new Produto(Guid.NewGuid(), nome.Trim(), (descricao ?? string.Empty).Trim(), NormalizarSku(sku), preco, categoriaId);
    }

    public void AtualizarDados(string nome, string descricao, decimal preco)
    {
        GarantirNomeValido(nome);
        GarantirPrecoValido(preco);

        Nome = nome.Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        Preco = preco;
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    public VarianteDeProduto AdicionarVariante(string nome, string sku, decimal preco, int estoque)
    {
        var variante = VarianteDeProduto.Criar(Id, nome, sku, preco, estoque);
        _variantes.Add(variante);
        return variante;
    }

    /// <summary>Lança <see cref="ExcecaoDeDominio"/> se essa for a última variante ativa do produto.</summary>
    public void InativarVariante(Guid varianteId)
    {
        var variante = ObterVarianteOuFalhar(varianteId);

        if (variante.Ativo && _variantes.Count(v => v.Ativo) <= 1)
        {
            throw new ExcecaoDeDominio("O produto precisa ter ao menos uma variante ativa.");
        }

        variante.Inativar();
    }

    public void AtivarVariante(Guid varianteId) => ObterVarianteOuFalhar(varianteId).Ativar();

    public void AjustarEstoqueDaVariante(Guid varianteId, int novoEstoque) =>
        ObterVarianteOuFalhar(varianteId).AjustarEstoque(novoEstoque);

    public void AtualizarPrecoDaVariante(Guid varianteId, decimal novoPreco) =>
        ObterVarianteOuFalhar(varianteId).AtualizarPreco(novoPreco);

    public ImagemDeProduto AdicionarImagem(string url)
    {
        var proximaOrdem = _imagens.Count == 0 ? 0 : _imagens.Max(i => i.Ordem) + 1;
        var imagem = ImagemDeProduto.Criar(Id, url, proximaOrdem);
        _imagens.Add(imagem);
        return imagem;
    }

    private VarianteDeProduto ObterVarianteOuFalhar(Guid varianteId) =>
        _variantes.FirstOrDefault(v => v.Id == varianteId)
            ?? throw new ExcecaoDeDominio("Variante não encontrada neste produto.");

    private static void GarantirNomeValido(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ExcecaoDeDominio("O nome do produto é obrigatório.");
        }

        if (nome.Trim().Length > TamanhoMaximoNome)
        {
            throw new ExcecaoDeDominio($"O nome do produto não pode ter mais de {TamanhoMaximoNome} caracteres.");
        }
    }

    private static void GarantirSkuValido(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ExcecaoDeDominio("O SKU do produto é obrigatório.");
        }

        if (sku.Trim().Length > TamanhoMaximoSku)
        {
            throw new ExcecaoDeDominio($"O SKU do produto não pode ter mais de {TamanhoMaximoSku} caracteres.");
        }
    }

    private static void GarantirPrecoValido(decimal preco)
    {
        if (preco < 0)
        {
            throw new ExcecaoDeDominio("O preço do produto não pode ser negativo.");
        }
    }

    private static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();
}
