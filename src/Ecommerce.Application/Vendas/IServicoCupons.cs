namespace Ecommerce.Application.Vendas;

/// <summary>
/// Cadastro (admin) e validação (cliente) de cupons. A aplicação de fato acontece no checkout
/// (<see cref="IServicoDePedidos.FinalizarCompraAsync"/>), que revalida tudo.
/// </summary>
public interface IServicoCupons
{
    Task<IReadOnlyCollection<CupomDto>> ListarAsync(CancellationToken ct = default);

    Task<CupomDto> CriarAsync(CriarCupomComando comando, CancellationToken ct = default);

    Task<CupomDto> AtivarAsync(Guid cupomId, CancellationToken ct = default);

    Task<CupomDto> InativarAsync(Guid cupomId, CancellationToken ct = default);

    /// <summary>Lança <see cref="Ecommerce.Domain.Comum.ExcecaoDeDominio"/> com a razão se o cupom não servir.</summary>
    Task<ValidacaoDeCupomDto> ValidarAsync(string codigo, decimal subtotal, CancellationToken ct = default);
}
