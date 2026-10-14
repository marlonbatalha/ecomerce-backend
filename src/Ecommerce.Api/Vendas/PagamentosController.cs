using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ecommerce.Application.Vendas;
using Ecommerce.Infrastructure.Vendas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ecommerce.Api.Vendas;

/// <summary>
/// Recebe a confirmação/recusa de pagamento do gateway. Cada provedor tem o seu endpoint, e só o
/// do provedor configurado (<c>Pagamentos:Provedor</c>) responde — o outro devolve 404, pra não
/// existir um caminho alternativo de "aprovar pedido" em produção.
/// </summary>
[ApiController]
[Route("api/v1/pagamentos")]
public class PagamentosController(
    IServicoDePedidos servicoDePedidos,
    IServicoDePagamento servicoDePagamento,
    IOptions<OpcoesWebhook> opcoesWebhook,
    IOptions<OpcoesPagamentos> opcoesPagamentos,
    ILogger<PagamentosController> logger) : ControllerBase
{
    private const string CabecalhoAssinatura = "X-Assinatura-Webhook";

    /// <summary>
    /// Gateway fake (dev/testes): HMAC-SHA256 de "referenciaExterna|aprovado" com
    /// <c>Webhook:ChaveSecreta</c> no cabeçalho <c>X-Assinatura-Webhook</c>.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(WebhookPagamentoRequisicao requisicao, CancellationToken ct)
    {
        if (opcoesPagamentos.Value.Provedor != OpcoesPagamentos.ProvedorFake)
        {
            return NotFound();
        }

        if (!Request.Headers.TryGetValue(CabecalhoAssinatura, out var assinaturaRecebida) ||
            !AssinaturaFakeValida(requisicao, assinaturaRecebida.ToString()))
        {
            return Unauthorized();
        }

        await servicoDePedidos.ProcessarConfirmacaoDePagamentoAsync(requisicao.ReferenciaExterna, requisicao.Aprovado, ct);

        return Ok();
    }

    /// <summary>
    /// Webhook do Mercado Pago. O corpo só diz "o pagamento X mudou" — o status é consultado na
    /// API do Mercado Pago (nunca confia no corpo). Responde 200 pra tudo que não for pagamento
    /// (merchant_order etc.), senão o Mercado Pago fica reenviando.
    /// </summary>
    [HttpPost("webhook/mercadopago")]
    [AllowAnonymous]
    public async Task<IActionResult> WebhookMercadoPago(CancellationToken ct)
    {
        if (opcoesPagamentos.Value.Provedor != OpcoesPagamentos.ProvedorMercadoPago)
        {
            return NotFound();
        }

        var (tipo, idDado) = await LerNotificacaoMercadoPagoAsync(ct);

        var assinaturaOk = ServicoDePagamentoMercadoPago.AssinaturaValida(
            Request.Headers["x-signature"].ToString(),
            Request.Headers["x-request-id"].ToString(),
            idDado,
            opcoesPagamentos.Value.MercadoPago.ChaveSecretaWebhook);

        if (!assinaturaOk)
        {
            logger.LogWarning("Webhook do Mercado Pago com assinatura inválida (data.id={IdDado})", idDado);
            return Unauthorized();
        }

        if (tipo != "payment" || string.IsNullOrEmpty(idDado))
        {
            return Ok();
        }

        var pagamento = await servicoDePagamento.ConsultarPagamentoAsync(idDado, ct);
        if (pagamento is not null)
        {
            await servicoDePedidos.ProcessarPagamentoDoGatewayAsync(pagamento, ct);
        }

        return Ok();
    }

    /// <summary>
    /// "type" e "data.id" vêm na query string (é o data.id da query que entra na assinatura); o
    /// corpo JSON repete os dois e serve de fallback.
    /// </summary>
    private async Task<(string? Tipo, string? IdDado)> LerNotificacaoMercadoPagoAsync(CancellationToken ct)
    {
        string? tipo = Request.Query["type"];
        string? idDado = Request.Query["data.id"];

        if (tipo is not null && idDado is not null)
        {
            return (tipo, idDado);
        }

        try
        {
            using var corpo = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            var raiz = corpo.RootElement;
            tipo ??= raiz.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (idDado is null && raiz.TryGetProperty("data", out var dado) && dado.TryGetProperty("id", out var id))
            {
                idDado = id.ValueKind == JsonValueKind.Number ? id.GetRawText() : id.GetString();
            }
        }
        catch (JsonException)
        {
            // Corpo vazio/inválido — fica só com o que veio na query.
        }

        return (tipo, idDado);
    }

    private bool AssinaturaFakeValida(WebhookPagamentoRequisicao requisicao, string assinaturaRecebida)
    {
        var mensagem = $"{requisicao.ReferenciaExterna}|{requisicao.Aprovado}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(opcoesWebhook.Value.ChaveSecreta), Encoding.UTF8.GetBytes(mensagem));
        var assinaturaEsperada = Convert.ToHexString(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(assinaturaEsperada),
            Encoding.UTF8.GetBytes(assinaturaRecebida.ToUpperInvariant()));
    }
}
