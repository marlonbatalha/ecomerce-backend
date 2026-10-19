using Ecommerce.Application.Catalogo;

namespace Ecommerce.Api.Catalogo;

/// <summary>
/// Query string de <c>GET /api/v1/produtos</c>. Classe com valores padrão (não record posicional)
/// pra que um parâmetro ausente na URL mantenha o padrão em vez de virar <c>default(T)</c>.
/// </summary>
public class BuscarProdutosRequisicao
{
    public Guid? CategoriaId { get; init; }

    public decimal? PrecoMinimo { get; init; }

    public decimal? PrecoMaximo { get; init; }

    public string? Termo { get; init; }

    public OrdenacaoProduto Ordenacao { get; init; } = OrdenacaoProduto.MaisRecente;

    public int Pagina { get; init; } = 1;

    public int TamanhoPagina { get; init; } = 20;
}
