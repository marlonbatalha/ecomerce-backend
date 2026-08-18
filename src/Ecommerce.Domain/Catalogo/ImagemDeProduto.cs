using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Catalogo;

/// <summary>
/// Referência a uma imagem de <see cref="Produto"/> guardada em storage externo (Object Storage —
/// ver seção 1 do plano) — só a URL fica no banco, nunca o binário. Criada através de
/// <see cref="Produto.AdicionarImagem"/>.
/// </summary>
public class ImagemDeProduto : Entidade
{
    public const int TamanhoMaximoUrl = 500;

    public Guid ProdutoId { get; private set; }

    public string Url { get; private set; } = null!;

    /// <summary>Posição de exibição (0 = principal) — define a ordem no carrossel do produto.</summary>
    public int Ordem { get; private set; }

    private ImagemDeProduto()
    {
    }

    private ImagemDeProduto(Guid id, Guid produtoId, string url, int ordem)
        : base(id)
    {
        ProdutoId = produtoId;
        Url = url;
        Ordem = ordem;
    }

    internal static ImagemDeProduto Criar(Guid produtoId, string url, int ordem)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ExcecaoDeDominio("A URL da imagem é obrigatória.");
        }

        if (url.Trim().Length > TamanhoMaximoUrl)
        {
            throw new ExcecaoDeDominio($"A URL da imagem não pode ter mais de {TamanhoMaximoUrl} caracteres.");
        }

        return new ImagemDeProduto(Guid.NewGuid(), produtoId, url.Trim(), ordem);
    }
}
