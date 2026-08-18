namespace Ecommerce.Application.Catalogo;

public record BuscarProdutosConsulta(
    Guid? CategoriaId,
    decimal? PrecoMinimo,
    decimal? PrecoMaximo,
    string? Termo,
    bool ApenasDisponiveis,
    OrdenacaoProduto Ordenacao,
    int Pagina,
    int TamanhoPagina);
