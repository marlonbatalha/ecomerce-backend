using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Catalog;

/// <summary>
/// Categoria de produto. Suporta hierarquia via auto-relacionamento (<see cref="ParentCategoryId"/>) —
/// ver seção 5 do plano do projeto.
/// </summary>
public class Category : Entity
{
    private const int MaxNameLength = 100;

    private readonly List<Category> _subCategories = [];

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public Guid? ParentCategoryId { get; private set; }

    public Category? ParentCategory { get; private set; }

    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private Category()
    {
    }

    private Category(Guid id, string name, Guid? parentCategoryId)
        : base(id)
    {
        Name = name;
        Slug = Slugify(name);
        ParentCategoryId = parentCategoryId;
    }

    public static Category Create(string name, Guid? parentCategoryId = null)
    {
        EnsureNameIsValid(name);

        return new Category(Guid.NewGuid(), name.Trim(), parentCategoryId);
    }

    public void Rename(string newName)
    {
        EnsureNameIsValid(newName);

        Name = newName.Trim();
        Slug = Slugify(newName);
    }

    /// <summary>
    /// Move a categoria para debaixo de outra (ou para a raiz, se <paramref name="newParent"/> for null).
    /// </summary>
    public void MoveTo(Category? newParent)
    {
        if (newParent is not null && newParent.Id == Id)
        {
            throw new DomainException("Uma categoria não pode ser pai de si mesma.");
        }

        ParentCategory = newParent;
        ParentCategoryId = newParent?.Id;
    }

    private static void EnsureNameIsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("O nome da categoria é obrigatório.");
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new DomainException($"O nome da categoria não pode ter mais de {MaxNameLength} caracteres.");
        }
    }

    private static string Slugify(string name)
    {
        var withoutDiacritics = RemoveDiacritics(name.Trim().ToLowerInvariant());
        return Regex.Replace(withoutDiacritics, "[^a-z0-9]+", "-").Trim('-');
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
