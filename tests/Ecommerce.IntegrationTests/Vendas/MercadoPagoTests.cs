using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.IntegrationTests.Vendas;

public class MercadoPagoTests(FabricaDeAplicacaoMercadoPago fabrica) : IClassFixture<FabricaDeAplicacaoMercadoPago>
{
    private async Task<(HttpClient Cliente, CheckoutResultado Checkout)> FazerCheckoutAsync()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 80m);

        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id);

        var resposta = await cliente.FinalizarCompraAsync();
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (cliente, (await resposta.Content.ReadFromJsonAsync<CheckoutResultado>())!);
    }

    private static HttpRequestMessage CriarWebhook(string idPagamento, string? chaveSecreta = FabricaDeAplicacaoMercadoPago.ChaveSecretaWebhook)
    {
        const string idRequisicao = "req-123";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var manifesto = $"id:{idPagamento};request-id:{idRequisicao};ts:{ts};";
        var v1 = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(chaveSecreta!), Encoding.UTF8.GetBytes(manifesto)));

        var requisicao = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pagamentos/webhook/mercadopago?data.id={idPagamento}&type=payment")
        {
            Content = JsonContent.Create(new { action = "payment.updated", type = "payment", data = new { id = idPagamento } }),
        };
        requisicao.Headers.Add("x-signature", $"ts={ts},v1={v1}");
        requisicao.Headers.Add("x-request-id", idRequisicao);
        return requisicao;
    }

    [Fact]
    public async Task Checkout_DeveCriarPreferenceEDevolverUrlDePagamento()
    {
        var (cliente, checkout) = await FazerCheckoutAsync();

        checkout.UrlPagamento.Should().StartWith("https://mp.teste/checkout?pref=");
        checkout.ReferenciaPagamento.Should().StartWith("pref-");

        var preference = fabrica.ApiMercadoPago.PreferenciasRecebidas.Select(p => JsonDocument.Parse(p).RootElement)
            .Single(p => p.GetProperty("external_reference").GetString() == checkout.Pedido.Id.ToString());
        // Produto + frete: o Mercado Pago cobra o total do pedido.
        preference.GetProperty("items")[0].GetProperty("unit_price").GetDecimal().Should().Be(80m + AuxiliaresDeTeste.FretePac);
        preference.GetProperty("notification_url").GetString().Should().Be("https://api.loja.teste/api/v1/pagamentos/webhook/mercadopago");
        preference.GetProperty("back_urls").GetProperty("success").GetString().Should().StartWith("https://loja.teste/checkout/confirmacao");
        preference.GetProperty("auto_return").GetString().Should().Be("approved");

        // Pedido pendente expõe a URL pra retomar o pagamento.
        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.UrlPagamento.Should().Be(checkout.UrlPagamento);
    }

    [Fact]
    public async Task Webhook_PagamentoAprovado_DeveConfirmarPedido()
    {
        var (cliente, checkout) = await FazerCheckoutAsync();
        var idPagamento = Random.Shared.NextInt64(1_000_000, 9_999_999).ToString();
        fabrica.ApiMercadoPago.Pagamentos[idPagamento] = ("approved", checkout.Pedido.Id);

        using var webhook = CriarWebhook(idPagamento);
        var resposta = await fabrica.CriarCliente().SendAsync(webhook);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Status.Should().Be(StatusPedido.PagamentoConfirmado);
        pedido.UrlPagamento.Should().BeNull();
    }

    [Fact]
    public async Task Webhook_PagamentoRejeitado_DeveManterPedidoPendente()
    {
        var (cliente, checkout) = await FazerCheckoutAsync();
        var idPagamento = Random.Shared.NextInt64(1_000_000, 9_999_999).ToString();
        fabrica.ApiMercadoPago.Pagamentos[idPagamento] = ("rejected", checkout.Pedido.Id);

        using var webhook = CriarWebhook(idPagamento);
        (await fabrica.CriarCliente().SendAsync(webhook)).StatusCode.Should().Be(HttpStatusCode.OK);

        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public async Task Webhook_PagamentoCancelado_DeveRecusarPedido()
    {
        var (cliente, checkout) = await FazerCheckoutAsync();
        var idPagamento = Random.Shared.NextInt64(1_000_000, 9_999_999).ToString();
        fabrica.ApiMercadoPago.Pagamentos[idPagamento] = ("cancelled", checkout.Pedido.Id);

        using var webhook = CriarWebhook(idPagamento);
        (await fabrica.CriarCliente().SendAsync(webhook)).StatusCode.Should().Be(HttpStatusCode.OK);

        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Status.Should().Be(StatusPedido.PagamentoRecusado);
    }

    [Fact]
    public async Task Webhook_ComAssinaturaInvalida_DeveRetornarUnauthorized()
    {
        using var webhook = CriarWebhook("123", chaveSecreta: "chave-errada");

        var resposta = await fabrica.CriarCliente().SendAsync(webhook);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebhookFake_ComProvedorMercadoPago_DeveRetornarNotFound()
    {
        var resposta = await fabrica.CriarCliente().PostAsJsonAsync("/api/v1/pagamentos/webhook", new { referenciaExterna = "x", aprovado = true });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
