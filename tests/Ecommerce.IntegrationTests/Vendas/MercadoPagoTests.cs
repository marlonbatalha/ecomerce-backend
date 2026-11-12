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
    private async Task<(HttpClient Cliente, HttpResponseMessage Resposta)> TentarCheckoutAsync(string metodo)
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 80m);

        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id);

        return (cliente, await cliente.FinalizarCompraAsync(metodoDePagamento: metodo));
    }

    private async Task<(HttpClient Cliente, CheckoutResultado Checkout)> FazerCheckoutAsync(string metodo = "Pix")
    {
        var (cliente, resposta) = await TentarCheckoutAsync(metodo);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (cliente, (await resposta.Content.ReadFromJsonAsync<CheckoutResultado>())!);
    }

    private JsonElement CorpoEnviadoAoMercadoPago(Guid pedidoId) =>
        fabrica.ApiMercadoPago.PagamentosCriados
            .Select(p => JsonDocument.Parse(p.Corpo).RootElement)
            .Single(p => p.GetProperty("external_reference").GetString() == pedidoId.ToString());

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
    public async Task CheckoutPix_DeveCriarPagamentoEDevolverCodigoPix()
    {
        var (cliente, checkout) = await FazerCheckoutAsync("Pix");

        var instrucoes = checkout.Pedido.Pagamento!;
        instrucoes.Metodo.Should().Be(MetodoDePagamento.Pix);
        instrucoes.CodigoPix.Should().StartWith("00020126580014br.gov.bcb.pix");
        instrucoes.UrlPagamento.Should().StartWith("https://www.mercadopago.com.br/payments/");
        instrucoes.LinhaDigitavel.Should().BeNull();
        // Validade padrão do Pix: 30 minutos.
        instrucoes.ExpiraEmUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));

        var corpo = CorpoEnviadoAoMercadoPago(checkout.Pedido.Id);
        corpo.GetProperty("payment_method_id").GetString().Should().Be("pix");
        // Produto + frete: o Mercado Pago cobra o total do pedido.
        corpo.GetProperty("transaction_amount").GetDecimal().Should().Be(80m + AuxiliaresDeTeste.FretePac);
        corpo.GetProperty("notification_url").GetString().Should().Be("https://api.loja.teste/api/v1/pagamentos/webhook/mercadopago");
        var pagador = corpo.GetProperty("payer");
        pagador.GetProperty("identification").GetProperty("type").GetString().Should().Be("CPF");
        pagador.GetProperty("identification").GetProperty("number").GetString().Should().MatchRegex("^[0-9]{11}$");
        pagador.GetProperty("first_name").GetString().Should().Be("Cliente");
        pagador.GetProperty("last_name").GetString().Should().Be("Teste");
        // Pix não precisa de endereço.
        pagador.TryGetProperty("address", out _).Should().BeFalse();

        var chave = fabrica.ApiMercadoPago.PagamentosCriados
            .Single(p => p.Corpo.Contains(checkout.Pedido.Id.ToString())).ChaveDeIdempotencia;
        chave.Should().Be($"{checkout.Pedido.Id}:Pix");

        // Pedido pendente continua expondo o Pix pra quem voltar à página do pedido.
        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Pagamento!.CodigoPix.Should().Be(instrucoes.CodigoPix);
    }

    [Fact]
    public async Task CheckoutBoleto_DeveEnviarEnderecoEDevolverBoleto()
    {
        var (_, checkout) = await FazerCheckoutAsync("Boleto");

        var instrucoes = checkout.Pedido.Pagamento!;
        instrucoes.Metodo.Should().Be(MetodoDePagamento.Boleto);
        instrucoes.UrlPagamento.Should().StartWith("https://www.mercadopago.com.br/payments/");
        instrucoes.LinhaDigitavel.Should().Be("23793.38128 60000.000003 00000.000400 1 84340000010000");
        instrucoes.CodigoPix.Should().BeNull();
        instrucoes.ExpiraEmUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromMinutes(1));
        checkout.UrlPagamento.Should().Be(instrucoes.UrlPagamento);

        var corpo = CorpoEnviadoAoMercadoPago(checkout.Pedido.Id);
        corpo.GetProperty("payment_method_id").GetString().Should().Be("bolbradesco");
        var endereco = corpo.GetProperty("payer").GetProperty("address");
        // Endereço de entrega do pedido (AuxiliaresDeTeste.CriarEnderecoAsync), CEP só com dígitos.
        endereco.GetProperty("zip_code").GetString().Should().Be("01310100");
        endereco.GetProperty("street_name").GetString().Should().Be("Av. Paulista");
        endereco.GetProperty("street_number").GetString().Should().Be("1000");
        endereco.GetProperty("neighborhood").GetString().Should().Be("Bela Vista");
        endereco.GetProperty("city").GetString().Should().Be("São Paulo");
        endereco.GetProperty("federal_unit").GetString().Should().Be("SP");
    }

    [Fact]
    public async Task Checkout_MercadoPagoRecusa_DeveFalharSemReservarEstoque()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 80m, estoque: 5);
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id, quantidade: 2);
        fabrica.ApiMercadoPago.RecusarProximaCriacao = true;

        var resposta = await cliente.FinalizarCompraAsync(metodoDePagamento: "Boleto");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("Não foi possível gerar o boleto");
        // Nada foi gravado: o estoque e o carrinho continuam como estavam.
        var produtoDepois = await cliente.GetFromJsonAsync<Application.Catalogo.ProdutoDto>($"/api/v1/produtos/{produto.Id}");
        produtoDepois!.Variantes.Single().Estoque.Should().Be(5);
        var carrinho = await cliente.GetFromJsonAsync<CarrinhoDto>("/api/v1/carrinho");
        carrinho!.Itens.Should().ContainSingle(i => i.Quantidade == 2);
    }

    [Fact]
    public async Task Webhook_PagamentoAprovado_DeveConfirmarPedido()
    {
        var (cliente, checkout) = await FazerCheckoutAsync();
        fabrica.ApiMercadoPago.Pagamentos[checkout.ReferenciaPagamento!] = ("approved", checkout.Pedido.Id);

        using var webhook = CriarWebhook(checkout.ReferenciaPagamento!);
        var resposta = await fabrica.CriarCliente().SendAsync(webhook);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Status.Should().Be(StatusPedido.PagamentoConfirmado);
        // Pago: nada mais a mostrar para pagar.
        pedido.Pagamento.Should().BeNull();
        pedido.UrlPagamento.Should().BeNull();
    }

    [Theory]
    [InlineData("cancelled")] // Pix ou boleto venceu sem pagamento
    [InlineData("rejected")]
    public async Task Webhook_PagamentoCanceladoOuRecusado_DeveRecusarPedido(string status)
    {
        var (cliente, checkout) = await FazerCheckoutAsync();
        fabrica.ApiMercadoPago.Pagamentos[checkout.ReferenciaPagamento!] = (status, checkout.Pedido.Id);

        using var webhook = CriarWebhook(checkout.ReferenciaPagamento!);
        (await fabrica.CriarCliente().SendAsync(webhook)).StatusCode.Should().Be(HttpStatusCode.OK);

        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Status.Should().Be(StatusPedido.PagamentoRecusado);
    }

    [Fact]
    public async Task Webhook_PagamentoPendente_DeveManterPedidoPendente()
    {
        var (cliente, checkout) = await FazerCheckoutAsync();
        fabrica.ApiMercadoPago.Pagamentos[checkout.ReferenciaPagamento!] = ("pending", checkout.Pedido.Id);

        using var webhook = CriarWebhook(checkout.ReferenciaPagamento!);
        (await fabrica.CriarCliente().SendAsync(webhook)).StatusCode.Should().Be(HttpStatusCode.OK);

        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout.Pedido.Id}");
        pedido!.Status.Should().Be(StatusPedido.Pendente);
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
