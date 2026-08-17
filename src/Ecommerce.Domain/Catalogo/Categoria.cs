using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Catalogo;

/// <summary>
/// Categoria de produto. Suporta hierarquia via auto-relacionamento (<see cref="CategoriaPaiId"/>) —
/// ver seção 5 do plano do projeto.
/// </summary>
public class Categoria : Entidade
{
    private const int TamanhoMaximoNome = 100;

    private readonly List<Categoria> _subcategorias = [];

    public string Nome { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public Guid? CategoriaPaiId { get; private set; }

    public Categoria? CategoriaPai { get; private set; }

    public IReadOnlyCollection<Categoria> Subcategorias => _subcategorias.AsReadOnly();

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private Categoria()
    {
    }

    private Categoria(Guid id, string nome, Guid? categoriaPaiId)
        : base(id)
    {
        Nome = nome;
        Slug = GerarSlug(nome);
        CategoriaPaiId = categoriaPaiId;
    }

    public static Categoria Criar(string nome, Guid? categoriaPaiId = null)
    {
        GarantirNomeValido(nome);

        return new Categoria(Guid.NewGuid(), nome.Trim(), categoriaPaiId);
    }

    public void Renomear(string novoNome)
    {
        GarantirNomeValido(novoNome);

        Nome = novoNome.Trim();
        Slug = GerarSlug(novoNome);
    }

    /// <summary>
    /// Move a categoria para debaixo de outra (ou para a raiz, se <paramref name="novaCategoriaPai"/> for null).
    /// </summary>
    public void MoverPara(Categoria? novaCategoriaPai)
    {
        if (novaCategoriaPai is not null && novaCategoriaPai.Id == Id)
        {
            throw new ExcecaoDeDominio("Uma categoria não pode ser pai de si mesma.");
        }

        CategoriaPai = novaCategoriaPai;
        CategoriaPaiId = novaCategoriaPai?.Id;
    }

    private static void GarantirNomeValido(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ExcecaoDeDominio("O nome da categoria é obrigatório.");
        }

        if (nome.Trim().Length > TamanhoMaximoNome)
        {
            throw new ExcecaoDeDominio($"O nome da categoria não pode ter mais de {TamanhoMaximoNome} caracteres.");
        }
    }

    private static string GerarSlug(string nome)
    {
        var semAcentos = RemoverAcentos(nome.Trim().ToLowerInvariant());
        return Regex.Replace(semAcentos, "[^a-z0-9]+", "-").Trim('-');
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
