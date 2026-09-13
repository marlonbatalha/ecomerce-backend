namespace Ecommerce.Application.Catalogo;

/// <summary>Detalhe completo — usado no "obter por id" (endpoint público de detalhe e admin).</summary>
public record ProdutoDto(
    Guid Id,
    string Nome,
    string Slug,
    string Descricao,
    string Sku,
    decimal Preco,
    Guid CategoriaId,
    bool Ativo,
    IReadOnlyCollection<VarianteDto> Variantes,
    IReadOnlyCollection<ImagemDto> Imagens);
