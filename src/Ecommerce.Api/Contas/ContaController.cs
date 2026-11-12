using System.Security.Claims;
using Ecommerce.Application.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Contas;

[ApiController]
[Route("api/v1/conta")]
[Authorize]
public class ContaController(IServicoConta servicoConta) : ControllerBase
{
    [HttpGet("perfil")]
    public async Task<ActionResult<PerfilDto>> ObterPerfil(CancellationToken ct)
    {
        var perfil = await servicoConta.ObterPerfilAsync(ObterUsuarioId(), ct);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    [HttpPut("perfil")]
    public async Task<ActionResult<PerfilDto>> AtualizarPerfil(AtualizarPerfilRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoConta.AtualizarPerfilAsync(new AtualizarPerfilComando(ObterUsuarioId(), requisicao.Nome, requisicao.Cpf), ct));

    [HttpPost("trocar-senha")]
    public async Task<IActionResult> TrocarSenha(TrocarSenhaRequisicao requisicao, CancellationToken ct)
    {
        await servicoConta.TrocarSenhaAsync(new TrocarSenhaComando(ObterUsuarioId(), requisicao.SenhaAtual, requisicao.NovaSenha), ct);
        return NoContent();
    }

    private Guid ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : Guid.Empty;
    }
}
