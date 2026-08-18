using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Comum;
using Ecommerce.Application.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Contas;

[ApiController]
[Route("api/v1/clientes")]
[Authorize(Roles = Papeis.Admin)]
public class ClientesController(IServicoClientes servicoClientes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<ClienteResumoDto>>> Buscar(
        [FromQuery] string? termo, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default) =>
        Ok(await servicoClientes.BuscarAsync(termo, pagina, tamanhoPagina, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteResumoDto>> ObterPorId(Guid id, CancellationToken ct)
    {
        var cliente = await servicoClientes.ObterPorIdAsync(id, ct);
        return cliente is null ? NotFound() : Ok(cliente);
    }
}
