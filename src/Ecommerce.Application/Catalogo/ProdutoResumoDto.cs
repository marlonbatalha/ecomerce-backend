namespace Ecommerce.Application.Catalogo;

/// <summary>Versão leve — usada na listagem/busca paginada (sem carregar todas as variantes/imagens).</summary>
public record ProdutoResumoDto(
    Guid Id,
    string Nome,
    string Sku,
    decimal Preco,
    Guid CategoriaId,
    bool Ativo,
    string? ImagemPrincipalUrl);
