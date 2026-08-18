using Ecommerce.Application.Comum;

namespace Ecommerce.Application.Contas;

/// <summary>Busca de cliente pro admin — ver Fase 4 do plano.</summary>
public interface IServicoClientes
{
    Task<ResultadoPaginado<ClienteResumoDto>> BuscarAsync(string? termo, int pagina, int tamanhoPagina, CancellationToken ct = default);

    Task<ClienteResumoDto?> ObterPorIdAsync(Guid usuarioId, CancellationToken ct = default);
}
