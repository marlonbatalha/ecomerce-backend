using System.Security.Claims;
using Ecommerce.Application.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Contas;

[ApiController]
[Route("api/v1/conta/enderecos")]
[Authorize]
public class EnderecosController(IServicoEnderecos servicoEnderecos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EnderecoDto>>> Listar(CancellationToken ct) =>
        Ok(await servicoEnderecos.ListarDoUsuarioAsync(ObterUsuarioId(), ct));

    [HttpPost]
    public async Task<ActionResult<EnderecoDto>> Criar(CriarEnderecoRequisicao requisicao, CancellationToken ct)
    {
        var endereco = await servicoEnderecos.CriarAsync(new CriarEnderecoComando(
            ObterUsuarioId(), requisicao.Rotulo, requisicao.Logradouro, requisicao.Numero, requisicao.Complemento,
            requisicao.Bairro, requisicao.Cidade, requisicao.Estado, requisicao.Cep), ct);

        return Ok(endereco);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EnderecoDto>> Atualizar(Guid id, CriarEnderecoRequisicao requisicao, CancellationToken ct)
    {
        var endereco = await servicoEnderecos.AtualizarAsync(new AtualizarEnderecoComando(
            id, ObterUsuarioId(), requisicao.Rotulo, requisicao.Logradouro, requisicao.Numero, requisicao.Complemento,
            requisicao.Bairro, requisicao.Cidade, requisicao.Estado, requisicao.Cep), ct);

        return Ok(endereco);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await servicoEnderecos.RemoverAsync(id, ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/padrao")]
    public async Task<ActionResult<EnderecoDto>> MarcarComoPadrao(Guid id, CancellationToken ct) =>
        Ok(await servicoEnderecos.MarcarComoPadraoAsync(id, ObterUsuarioId(), ct));

    private Guid ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : Guid.Empty;
    }
}
