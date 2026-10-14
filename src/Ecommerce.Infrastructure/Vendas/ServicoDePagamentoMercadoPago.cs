using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Ecommerce.Application.Vendas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Vendas;

/// <summary>
/// Mercado Pago Checkout Pro: o checkout cria uma "preference" e o cliente é redirecionado pra
/// página do Mercado Pago (Pix, cartão, boleto — nenhum dado de pagamento passa por aqui). O
/// resultado chega por webhook (<c>PagamentosController.WebhookMercadoPago</c>), que só avisa "o
/// pagamento X mudou"; o status confiável vem de <see cref="ConsultarPagamentoAsync"/>.
/// Integração via HTTP direto (API REST v1) em vez do SDK oficial: são só duas chamadas, e assim
/// fica sob o mesmo <see cref="HttpClient"/> tipado/resiliente do resto da aplicação.
/// </summary>
public class ServicoDePagamentoMercadoPago(
    HttpClient http,
    IOptions<OpcoesPagamentos> opcoesPagamentos,
    IOptions<OpcoesAplicacao> opcoesAplicacao,
    ILogger<ServicoDePagamentoMercadoPago> logger) : IServicoDePagamento
{
    public const string UrlBase = "https://api.mercadopago.com/";

    private OpcoesMercadoPago Opcoes => opcoesPagamentos.Value.MercadoPago;

    public async Task<CobrancaIniciada> IniciarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default)
    {
        var urlFrontend = opcoesAplicacao.Value.UrlFrontend.TrimEnd('/');
        var urlApi = opcoesAplicacao.Value.UrlPublicaApi.TrimEnd('/');
        var urlRetorno = $"{urlFrontend}/checkout/confirmacao?pedido={solicitacao.PedidoId}";

        var preferencia = new PreferenciaRequisicao(
            // Um único item com o total já descontado: o valor cobrado tem que bater com o
            // Pedido.Total (cupom incluso), e o detalhamento dos itens fica no nosso pedido.
            [new ItemPreferencia(solicitacao.PedidoId.ToString(), solicitacao.Descricao, 1, solicitacao.Valor, "BRL")],
            solicitacao.EmailComprador is null ? null : new PagadorPreferencia(solicitacao.EmailComprador),
            solicitacao.PedidoId.ToString(),
            // notification_url precisa ser pública/HTTPS — em dev (sem UrlPublicaApi) fica de fora
            // e vale o webhook cadastrado no painel do Mercado Pago, se houver.
            string.IsNullOrEmpty(urlApi) ? null : $"{urlApi}/api/v1/pagamentos/webhook/mercadopago",
            new UrlsDeRetorno($"{urlRetorno}&pagamento=aprovado", $"{urlRetorno}&pagamento=pendente", $"{urlRetorno}&pagamento=falhou"),
            // O Mercado Pago recusa auto_return com URL de retorno não-HTTPS (ex.: localhost).
            urlFrontend.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "approved" : null);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences")
        {
            Content = JsonContent.Create(preferencia),
        };
        AdicionarAutorizacao(requisicao);
        // Retry do checkout não cria duas preferences pro mesmo pedido.
        requisicao.Headers.Add("X-Idempotency-Key", solicitacao.PedidoId.ToString());

        using var resposta = await http.SendAsync(requisicao, ct);
        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync(ct);
            logger.LogError("Mercado Pago recusou a preference do pedido {PedidoId}: {Status} {Corpo}", solicitacao.PedidoId, (int)resposta.StatusCode, corpo);
            throw new Domain.Comum.ExcecaoDeDominio("Não foi possível iniciar o pagamento agora. Tente novamente em instantes.");
        }

        var criada = await resposta.Content.ReadFromJsonAsync<PreferenciaResposta>(ct)
            ?? throw new InvalidOperationException("Resposta vazia do Mercado Pago ao criar preference.");

        return new CobrancaIniciada(criada.Id, Opcoes.UsarSandbox ? criada.SandboxInitPoint : criada.InitPoint);
    }

    public async Task<PagamentoConsultado?> ConsultarPagamentoAsync(string idTransacao, CancellationToken ct = default)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(idTransacao)}");
        AdicionarAutorizacao(requisicao);

        using var resposta = await http.SendAsync(requisicao, ct);
        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        resposta.EnsureSuccessStatusCode();

        var pagamento = await resposta.Content.ReadFromJsonAsync<PagamentoResposta>(ct);
        if (pagamento is null || !Guid.TryParse(pagamento.ExternalReference, out var pedidoId))
        {
            // Pagamento que não foi gerado por este checkout (ex.: cobrança avulsa na conta).
            return null;
        }

        return new PagamentoConsultado(pedidoId, pagamento.Id.ToString(CultureInfo.InvariantCulture), TraduzirStatus(pagamento.Status));
    }

    /// <summary>
    /// Ver "status" em https://www.mercadopago.com.br/developers/pt/reference/payments/_payments_id/get.
    /// "rejected" fica como Pendente de propósito: no Checkout Pro o cliente pode tentar outro
    /// cartão na mesma preference — recusar o pedido aqui mataria uma compra que ainda pode sair.
    /// "cancelled" (Pix/boleto expirado, cancelamento) é que encerra.
    /// </summary>
    internal static ResultadoDePagamento TraduzirStatus(string? status) => status switch
    {
        "approved" => ResultadoDePagamento.Aprovado,
        "cancelled" => ResultadoDePagamento.Recusado,
        "refunded" or "charged_back" => ResultadoDePagamento.Estornado,
        _ => ResultadoDePagamento.Pendente,
    };

    /// <summary>
    /// Valida o cabeçalho <c>x-signature</c> ("ts=...,v1=...") — HMAC-SHA256 do manifesto
    /// <c>id:{data.id};request-id:{x-request-id};ts:{ts};</c> com a assinatura secreta do webhook.
    /// Ver https://www.mercadopago.com.br/developers/pt/docs/your-integrations/notifications/webhooks.
    /// </summary>
    public static bool AssinaturaValida(string? cabecalhoAssinatura, string? idRequisicao, string? idDado, string chaveSecreta)
    {
        if (string.IsNullOrWhiteSpace(cabecalhoAssinatura) || string.IsNullOrWhiteSpace(chaveSecreta))
        {
            return false;
        }

        string? ts = null;
        string? v1 = null;
        foreach (var parte in cabecalhoAssinatura.Split(','))
        {
            var chaveValor = parte.Split('=', 2, StringSplitOptions.TrimEntries);
            if (chaveValor.Length != 2)
            {
                continue;
            }

            if (chaveValor[0] == "ts")
            {
                ts = chaveValor[1];
            }
            else if (chaveValor[0] == "v1")
            {
                v1 = chaveValor[1];
            }
        }

        if (ts is null || v1 is null)
        {
            return false;
        }

        // Partes ausentes saem do manifesto (regra da documentação); id alfanumérico vai em minúsculas.
        var manifesto = new StringBuilder();
        if (!string.IsNullOrEmpty(idDado))
        {
            manifesto.Append($"id:{idDado.ToLowerInvariant()};");
        }

        if (!string.IsNullOrEmpty(idRequisicao))
        {
            manifesto.Append($"request-id:{idRequisicao};");
        }

        manifesto.Append($"ts:{ts};");

        var esperado = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(chaveSecreta), Encoding.UTF8.GetBytes(manifesto.ToString())));

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(v1.ToLowerInvariant()));
    }

    private void AdicionarAutorizacao(HttpRequestMessage requisicao)
    {
        if (string.IsNullOrWhiteSpace(Opcoes.AccessToken))
        {
            throw new InvalidOperationException("Pagamentos:MercadoPago:AccessToken não configurado.");
        }

        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Opcoes.AccessToken);
    }

    private record PreferenciaRequisicao(
        [property: JsonPropertyName("items")] IReadOnlyList<ItemPreferencia> Itens,
        [property: JsonPropertyName("payer"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PagadorPreferencia? Pagador,
        [property: JsonPropertyName("external_reference")] string ReferenciaExterna,
        [property: JsonPropertyName("notification_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? UrlNotificacao,
        [property: JsonPropertyName("back_urls")] UrlsDeRetorno UrlsDeRetorno,
        [property: JsonPropertyName("auto_return"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RetornoAutomatico);

    private record ItemPreferencia(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Titulo,
        [property: JsonPropertyName("quantity")] int Quantidade,
        [property: JsonPropertyName("unit_price")] decimal PrecoUnitario,
        [property: JsonPropertyName("currency_id")] string Moeda);

    private record PagadorPreferencia([property: JsonPropertyName("email")] string Email);

    private record UrlsDeRetorno(
        [property: JsonPropertyName("success")] string Sucesso,
        [property: JsonPropertyName("pending")] string Pendente,
        [property: JsonPropertyName("failure")] string Falha);

    private record PreferenciaResposta(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("init_point")] string InitPoint,
        [property: JsonPropertyName("sandbox_init_point")] string SandboxInitPoint);

    private record PagamentoResposta(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);
}
