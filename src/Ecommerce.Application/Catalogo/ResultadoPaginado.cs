namespace Ecommerce.Application.Catalogo;

public record ResultadoPaginado<T>(IReadOnlyCollection<T> Itens, int Pagina, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
