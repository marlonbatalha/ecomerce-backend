using System.Security.Claims;
using Ecommerce.Application.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Contas;

/// <summary>
/// Lista de favoritos do cliente logado. <c>PUT</c>/<c>DELETE</c> no produto (em vez de <c>POST</c>
/// com corpo) porque as duas operações são idempotentes: repetir dá o mesmo resultado.
/// </summary>
[ApiController]
[Route("api/v1/conta/favoritos")]
[Authorize]
public class FavoritosController(IServicoFavoritos servicoFavoritos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<FavoritoDto>>> Listar(CancellationToken ct) =>
        Ok(await servicoFavoritos.ListarAsync(ObterUsuarioId(), ct));

    [HttpGet("ids")]
    public async Task<ActionResult<IReadOnlyCollection<Guid>>> ListarIds(CancellationToken ct) =>
        Ok(await servicoFavoritos.ListarIdsDeProdutosAsync(ObterUsuarioId(), ct));

    [HttpPut("{produtoId:guid}")]
    public async Task<IActionResult> Adicionar(Guid produtoId, CancellationToken ct)
    {
        await servicoFavoritos.AdicionarAsync(ObterUsuarioId(), produtoId, ct);
        return NoContent();
    }

    [HttpDelete("{produtoId:guid}")]
    public async Task<IActionResult> Remover(Guid produtoId, CancellationToken ct)
    {
        await servicoFavoritos.RemoverAsync(ObterUsuarioId(), produtoId, ct);
        return NoContent();
    }

    private Guid ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : Guid.Empty;
    }
}
