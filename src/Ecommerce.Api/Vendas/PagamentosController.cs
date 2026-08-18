using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Vendas;
using Ecommerce.Infrastructure.Vendas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ecommerce.Api.Vendas;

/// <summary>
/// Recebe a confirmação/recusa de pagamento do gateway. Esquema de assinatura próprio e
/// provisório (HMAC-SHA256 de "referenciaExterna|aprovado" com chave compartilhada) — o gateway
/// real (ainda não decidido, ver seção 9 do plano) terá seu próprio formato, a adaptar aqui
/// quando <see cref="IServicoDePagamento"/> ganhar uma implementação de verdade.
/// </summary>
[ApiController]
[Route("api/v1/pagamentos")]
public class PagamentosController(IServicoDePedidos servicoDePedidos, IOptions<OpcoesWebhook> opcoesWebhook) : ControllerBase
{
    private const string CabecalhoAssinatura = "X-Assinatura-Webhook";

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(WebhookPagamentoRequisicao requisicao, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(CabecalhoAssinatura, out var assinaturaRecebida) ||
            !AssinaturaValida(requisicao, assinaturaRecebida.ToString()))
        {
            return Unauthorized();
        }

        await servicoDePedidos.ProcessarConfirmacaoDePagamentoAsync(requisicao.ReferenciaExterna, requisicao.Aprovado, ct);

        return Ok();
    }

    private bool AssinaturaValida(WebhookPagamentoRequisicao requisicao, string assinaturaRecebida)
    {
        var mensagem = $"{requisicao.ReferenciaExterna}|{requisicao.Aprovado}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(opcoesWebhook.Value.ChaveSecreta), Encoding.UTF8.GetBytes(mensagem));
        var assinaturaEsperada = Convert.ToHexString(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(assinaturaEsperada),
            Encoding.UTF8.GetBytes(assinaturaRecebida.ToUpperInvariant()));
    }
}
