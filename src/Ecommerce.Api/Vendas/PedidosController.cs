using System.Security.Claims;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Comum;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Vendas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ecommerce.Api.Vendas;

[ApiController]
[Route("api/v1/pedidos")]
[Authorize]
public class PedidosController(IServicoDePedidos servicoDePedidos, IServicoCarrinho servicoCarrinho) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResultado>> Checkout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CheckoutRequisicao? requisicao, CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        var carrinho = await servicoCarrinho.ObterOuCriarParaUsuarioAsync(usuarioId, ct);
        var resultado = await servicoDePedidos.FinalizarCompraAsync(
            new FinalizarCompraComando(usuarioId, carrinho.Id, requisicao?.CodigoCupom), ct);

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<PedidoResumoDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default) =>
        Ok(await servicoDePedidos.ListarDoUsuarioAsync(ObterUsuarioId(), pagina, tamanhoPagina, ct));

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

    /// <summary>Admin: pedidos de qualquer cliente, opcionalmente filtrado por status.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ResultadoPaginado<PedidoResumoDto>>> ListarTodos(
        [FromQuery] StatusPedido? status, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default) =>
        Ok(await servicoDePedidos.ListarTodosAsync(status, pagina, tamanhoPagina, ct));

    /// <summary>Admin: detalhe de qualquer pedido, sem checagem de dono.</summary>
    [HttpGet("admin/{id:guid}")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<PedidoDto>> ObterPorIdAdmin(Guid id, CancellationToken ct)
    {
        var pedido = await servicoDePedidos.ObterPorIdAsync(id, ct);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    private Guid ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : Guid.Empty;
    }
}
