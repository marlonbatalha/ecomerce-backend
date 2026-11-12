using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Ecommerce.Application.Vendas;
using Ecommerce.Infrastructure.Vendas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests;

/// <summary>
/// Sobe a API com <c>Pagamentos:Provedor = MercadoPago</c>, mas com a API do Mercado Pago
/// simulada por <see cref="ApiMercadoPagoFalsa"/>: testa a integração (montagem do pagamento,
/// webhook assinado, consulta do pagamento) sem rede.
/// </summary>
public class FabricaDeAplicacaoMercadoPago : FabricaDeAplicacaoTeste
{
    public const string ChaveSecretaWebhook = "segredo-webhook-teste";

    public ApiMercadoPagoFalsa ApiMercadoPago { get; } = new();

    protected override IDictionary<string, string?> Configuracoes
    {
        get
        {
            var configuracoes = base.Configuracoes;
            configuracoes["Pagamentos:Provedor"] = "MercadoPago";
            configuracoes["Pagamentos:MercadoPago:AccessToken"] = "TEST-token";
            configuracoes["Pagamentos:MercadoPago:ChaveSecretaWebhook"] = ChaveSecretaWebhook;
            configuracoes["Aplicacao:UrlFrontend"] = "https://loja.teste";
            configuracoes["Aplicacao:UrlPublicaApi"] = "https://api.loja.teste";
            return configuracoes;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(servicos =>
            servicos.AddHttpClient<IServicoDePagamento, ServicoDePagamentoMercadoPago>()
                .ConfigurePrimaryHttpMessageHandler(() => ApiMercadoPago));
    }
}

/// <summary>
/// Responde como a API do Mercado Pago: <c>POST /v1/payments</c> cria um Pix ou boleto pendente
/// (no formato documentado), e <c>GET /v1/payments/{id}</c> devolve o que o teste cadastrou em
/// <see cref="Pagamentos"/>. <see cref="RecusarProximaCriacao"/> simula um 400 do Mercado Pago.
/// </summary>
public class ApiMercadoPagoFalsa : HttpMessageHandler
{
    /// <summary>Corpo e chave de idempotência de cada <c>POST /v1/payments</c>.</summary>
    public ConcurrentQueue<(string Corpo, string? ChaveDeIdempotencia)> PagamentosCriados { get; } = new();

    /// <summary>id do pagamento → (status, external_reference).</summary>
    public ConcurrentDictionary<string, (string Status, Guid PedidoId)> Pagamentos { get; } = new();

    public volatile bool RecusarProximaCriacao;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requisicao, CancellationToken ct)
    {
        if (requisicao.Headers.Authorization?.Parameter != "TEST-token")
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        var caminho = requisicao.RequestUri!.AbsolutePath;

        if (requisicao.Method == HttpMethod.Post && caminho == "/v1/payments")
        {
            var corpo = await requisicao.Content!.ReadAsStringAsync(ct);
            requisicao.Headers.TryGetValues("X-Idempotency-Key", out var chaves);
            PagamentosCriados.Enqueue((corpo, chaves?.FirstOrDefault()));

            if (RecusarProximaCriacao)
            {
                RecusarProximaCriacao = false;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""{"message":"Invalid user identification number","status":400}""", Encoding.UTF8, "application/json"),
                };
            }

            var json = JsonDocument.Parse(corpo).RootElement;
            var id = Random.Shared.NextInt64(10_000_000, 99_999_999);
            var validade = json.GetProperty("date_of_expiration").GetString();

            object resposta = json.GetProperty("payment_method_id").GetString() == "pix"
                ? new
                {
                    id,
                    status = "pending",
                    status_detail = "pending_waiting_transfer",
                    date_of_expiration = validade,
                    point_of_interaction = new
                    {
                        type = "PIX",
                        transaction_data = new
                        {
                            qr_code = $"00020126580014br.gov.bcb.pix{id}",
                            qr_code_base64 = "iVBORw0KGgo=",
                            ticket_url = $"https://www.mercadopago.com.br/payments/{id}/ticket",
                        },
                    },
                }
                : new
                {
                    id,
                    status = "pending",
                    status_detail = "pending_waiting_payment",
                    date_of_expiration = validade,
                    transaction_details = new
                    {
                        external_resource_url = $"https://www.mercadopago.com.br/payments/{id}/ticket",
                        digitable_line = "23793.38128 60000.000003 00000.000400 1 84340000010000",
                    },
                };

            return Json(JsonSerializer.Serialize(resposta));
        }

        if (requisicao.Method == HttpMethod.Get && caminho.StartsWith("/v1/payments/", StringComparison.Ordinal))
        {
            var idPagamento = caminho["/v1/payments/".Length..];
            return Pagamentos.TryGetValue(idPagamento, out var pagamento)
                ? Json($$"""{"id":{{idPagamento}},"status":"{{pagamento.Status}}","external_reference":"{{pagamento.PedidoId}}"}""")
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };
}
