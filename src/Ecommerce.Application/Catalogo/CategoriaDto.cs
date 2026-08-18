namespace Ecommerce.Application.Catalogo;

public record CategoriaDto(
    Guid Id,
    string Nome,
    string Slug,
    Guid? CategoriaPaiId,
    IReadOnlyCollection<CategoriaDto> Subcategorias);
