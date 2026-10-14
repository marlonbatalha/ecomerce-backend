using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Vendas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Vendas;

[ApiController]
[Route("api/v1/cupons")]
public class CuponsController(IServicoCupons servicoCupons) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<CupomDto>>> Listar(CancellationToken ct) =>
        Ok(await servicoCupons.ListarAsync(ct));

    [HttpPost]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<CupomDto>> Criar(CriarCupomRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoCupons.CriarAsync(new CriarCupomComando(
            requisicao.Codigo, requisicao.Descricao ?? string.Empty, requisicao.Tipo, requisicao.Valor,
            requisicao.ValorMinimoDoPedido, requisicao.ValidoAteUtc, requisicao.LimiteDeUsos), ct));

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<CupomDto>> Ativar(Guid id, CancellationToken ct) =>
        Ok(await servicoCupons.AtivarAsync(id, ct));

    [HttpPost("{id:guid}/inativar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<CupomDto>> Inativar(Guid id, CancellationToken ct) =>
        Ok(await servicoCupons.InativarAsync(id, ct));

    /// <summary>
    /// Prévia do desconto no carrinho — público (carrinho anônimo também mostra), com rate limit
    /// pra não virar oráculo de força bruta de códigos. O checkout recalcula tudo de novo.
    /// </summary>
    [HttpPost("validar")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeRateLimiting.Cupons)]
    public async Task<ActionResult<ValidacaoDeCupomDto>> Validar(ValidarCupomRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoCupons.ValidarAsync(requisicao.Codigo, requisicao.Subtotal, ct));
}
