using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Ecommerce.Application.Vendas;
using Ecommerce.Infrastructure.Vendas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests;

/// <summary>
/// Sobe a API com <c>Pagamentos:Provedor = MercadoPago</c>, mas com a API do Mercado Pago
/// simulada por <see cref="ApiMercadoPagoFalsa"/> — testa a integração (montagem da preference,
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
/// Responde como a API do Mercado Pago: <c>POST /checkout/preferences</c> devolve um init_point, e
/// <c>GET /v1/payments/{id}</c> devolve o que o teste cadastrou em <see cref="Pagamentos"/>.
/// </summary>
public class ApiMercadoPagoFalsa : HttpMessageHandler
{
    public ConcurrentQueue<string> PreferenciasRecebidas { get; } = new();

    /// <summary>id do pagamento → (status, external_reference).</summary>
    public ConcurrentDictionary<string, (string Status, Guid PedidoId)> Pagamentos { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requisicao, CancellationToken ct)
    {
        if (requisicao.Headers.Authorization?.Parameter != "TEST-token")
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        var caminho = requisicao.RequestUri!.AbsolutePath;

        if (requisicao.Method == HttpMethod.Post && caminho == "/checkout/preferences")
        {
            PreferenciasRecebidas.Enqueue(await requisicao.Content!.ReadAsStringAsync(ct));
            var id = $"pref-{Guid.NewGuid():N}";
            return Json($$"""{"id":"{{id}}","init_point":"https://mp.teste/checkout?pref={{id}}","sandbox_init_point":"https://sandbox.mp.teste/checkout?pref={{id}}"}""");
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
