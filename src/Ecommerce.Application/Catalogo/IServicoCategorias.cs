namespace Ecommerce.Application.Catalogo;

/// <summary>
/// Casos de uso de categoria. Implementado em Infrastructure (depende do EF Core) — a Api
/// conhece só esta interface. Falhas de regra de negócio (nome inválido, categoria pai
/// inexistente, mover pra si mesma etc.) lançam <see cref="Ecommerce.Domain.Comum.ExcecaoDeDominio"/>,
/// traduzida globalmente pra 400 (ver <c>ManipuladorDeExcecoesDeDominio</c>).
/// </summary>
public interface IServicoCategorias
{
    Task<CategoriaDto> CriarAsync(CriarCategoriaComando comando, CancellationToken ct = default);

    Task<CategoriaDto> RenomearAsync(RenomearCategoriaComando comando, CancellationToken ct = default);

    Task<CategoriaDto> MoverAsync(MoverCategoriaComando comando, CancellationToken ct = default);

    /// <summary>Árvore completa (raízes com subcategorias aninhadas) — usada na listagem pública.</summary>
    Task<IReadOnlyCollection<CategoriaDto>> ListarArvoreAsync(CancellationToken ct = default);

    Task<CategoriaDto?> ObterPorSlugAsync(string slug, CancellationToken ct = default);
}
