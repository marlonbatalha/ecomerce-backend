using Ecommerce.Application.Comum;

namespace Ecommerce.Application.Catalogo;

/// <summary>
/// Casos de uso de produto/variante. Implementado em Infrastructure (depende do EF Core) — a Api
/// conhece só esta interface. Falhas de regra de negócio (SKU duplicado, categoria inexistente,
/// última variante ativa, etc.) lançam <see cref="Ecommerce.Domain.Comum.ExcecaoDeDominio"/>,
/// traduzida globalmente pra 400 (ver <c>ManipuladorDeExcecoesDeDominio</c>).
/// </summary>
public interface IServicoProdutos
{
    Task<ProdutoDto> CriarAsync(CriarProdutoComando comando, CancellationToken ct = default);

    Task<ProdutoDto> AtualizarAsync(AtualizarProdutoComando comando, CancellationToken ct = default);

    Task<ProdutoDto> InativarAsync(Guid produtoId, CancellationToken ct = default);

    Task<ProdutoDto> AtivarAsync(Guid produtoId, CancellationToken ct = default);

    Task<ProdutoDto> AdicionarVarianteAsync(AdicionarVarianteComando comando, CancellationToken ct = default);

    Task<ProdutoDto> AjustarEstoqueAsync(AjustarEstoqueComando comando, CancellationToken ct = default);

    Task<ProdutoDto> InativarVarianteAsync(InativarVarianteComando comando, CancellationToken ct = default);

    Task<ProdutoDto> AtivarVarianteAsync(AtivarVarianteComando comando, CancellationToken ct = default);

    Task<ProdutoDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Listagem paginada/filtrada — usada tanto na busca pública quanto no admin.</summary>
    Task<ResultadoPaginado<ProdutoResumoDto>> BuscarAsync(BuscarProdutosConsulta consulta, CancellationToken ct = default);

    /// <summary>Admin: histórico de ajustes manuais de estoque de uma variante, mais recente primeiro.</summary>
    Task<IReadOnlyCollection<AjusteDeEstoqueDto>> ObterHistoricoDeEstoqueAsync(Guid varianteId, CancellationToken ct = default);
}
