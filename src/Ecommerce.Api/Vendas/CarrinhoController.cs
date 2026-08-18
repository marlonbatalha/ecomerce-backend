using System.Security.Claims;
using Ecommerce.Application.Vendas;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Vendas;

/// <summary>
/// Carrinho de usuário autenticado ou anônimo (cookie de sessão) — nenhuma rota exige
/// <c>[Authorize]</c> de propósito, o carrinho funciona pros dois casos (ver ResolverCarrinhoAsync).
/// </summary>
[ApiController]
[Route("api/v1/carrinho")]
public class CarrinhoController(IServicoCarrinho servicoCarrinho) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CarrinhoDto>> Obter(CancellationToken ct) => Ok(await ResolverCarrinhoAsync(ct));

    [HttpPost("itens")]
    public async Task<ActionResult<CarrinhoDto>> AdicionarItem(AdicionarItemRequisicao requisicao, CancellationToken ct)
    {
        var carrinho = await ResolverCarrinhoAsync(ct);
        var atualizado = await servicoCarrinho.AdicionarItemAsync(
            new AdicionarItemCarrinhoComando(carrinho.Id, requisicao.VarianteDeProdutoId, requisicao.Quantidade), ct);

        return Ok(atualizado);
    }

    [HttpPut("itens/{varianteId:guid}")]
    public async Task<ActionResult<CarrinhoDto>> AtualizarQuantidade(Guid varianteId, AtualizarQuantidadeRequisicao requisicao, CancellationToken ct)
    {
        var carrinho = await ResolverCarrinhoAsync(ct);
        var atualizado = await servicoCarrinho.AtualizarQuantidadeAsync(
            new AtualizarQuantidadeCarrinhoComando(carrinho.Id, varianteId, requisicao.Quantidade), ct);

        return Ok(atualizado);
    }

    [HttpDelete("itens/{varianteId:guid}")]
    public async Task<ActionResult<CarrinhoDto>> RemoverItem(Guid varianteId, CancellationToken ct)
    {
        var carrinho = await ResolverCarrinhoAsync(ct);
        var atualizado = await servicoCarrinho.RemoverItemAsync(new RemoverItemCarrinhoComando(carrinho.Id, varianteId), ct);

        return Ok(atualizado);
    }

    private async Task<CarrinhoDto> ResolverCarrinhoAsync(CancellationToken ct)
    {
        var usuarioId = ObterUsuarioIdAutenticado();
        if (usuarioId is not null)
        {
            return await servicoCarrinho.ObterOuCriarParaUsuarioAsync(usuarioId.Value, ct);
        }

        var idSessao = Request.Cookies[NomesDeCookies.SessaoCarrinho];
        if (string.IsNullOrWhiteSpace(idSessao))
        {
            idSessao = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(NomesDeCookies.SessaoCarrinho, idSessao, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
            });
        }

        return await servicoCarrinho.ObterOuCriarAnonimoAsync(idSessao, ct);
    }

    private Guid? ObterUsuarioIdAutenticado()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : null;
    }
}
