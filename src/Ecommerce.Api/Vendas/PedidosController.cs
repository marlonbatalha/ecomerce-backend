using System.Security.Claims;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Vendas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Vendas;

[ApiController]
[Route("api/v1/pedidos")]
[Authorize]
public class PedidosController(IServicoDePedidos servicoDePedidos, IServicoCarrinho servicoCarrinho) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResultado>> Checkout(CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        var carrinho = await servicoCarrinho.ObterOuCriarParaUsuarioAsync(usuarioId, ct);
        var resultado = await servicoDePedidos.FinalizarCompraAsync(new FinalizarCompraComando(usuarioId, carrinho.Id), ct);

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PedidoResumoDto>>> Listar(CancellationToken ct) =>
        Ok(await servicoDePedidos.ListarDoUsuarioAsync(ObterUsuarioId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PedidoDto>> ObterPorId(Guid id, CancellationToken ct)
    {
        var pedido = await servicoDePedidos.ObterDoUsuarioAsync(id, ObterUsuarioId(), ct);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<PedidoDto>> Cancelar(Guid id, CancellationToken ct) =>
        Ok(await servicoDePedidos.CancelarAsync(id, ObterUsuarioId(), ct));

    [HttpPost("{id:guid}/enviar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<PedidoDto>> MarcarComoEnviado(Guid id, MarcarComoEnviadoRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoDePedidos.MarcarComoEnviadoAsync(id, requisicao.CodigoRastreio, ct));

    [HttpPost("{id:guid}/entregar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<PedidoDto>> MarcarComoEntregue(Guid id, CancellationToken ct) =>
        Ok(await servicoDePedidos.MarcarComoEntregueAsync(id, ct));

    private Guid ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : Guid.Empty;
    }
}
