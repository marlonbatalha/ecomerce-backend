using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Comum;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.IntegrationTests.Vendas;

public class CuponsTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private static async Task<CupomDto> CriarCupomAsync(HttpClient admin, object cupom)
    {
        var resposta = await admin.PostAsJsonAsync("/api/v1/cupons", cupom);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resposta.Content.ReadFromJsonAsync<CupomDto>())!;
    }

    private static string CodigoUnico(string prefixo) => $"{prefixo}{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    [Fact]
    public async Task Validar_CupomPercentual_DeveDevolverPreviaDoDesconto()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var codigo = CodigoUnico("DEZ");
        await CriarCupomAsync(admin, new { codigo, descricao = "10% off", tipo = TipoDeDesconto.Percentual, valor = 10m });

        using var anonimo = fabrica.CriarCliente();
        var resposta = await anonimo.PostAsJsonAsync("/api/v1/cupons/validar", new { codigo = codigo.ToLowerInvariant(), subtotal = 250m });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var validacao = await resposta.Content.ReadFromJsonAsync<ValidacaoDeCupomDto>();
        validacao!.Desconto.Should().Be(25m);
        validacao.Codigo.Should().Be(codigo);
    }

    [Fact]
    public async Task Validar_CupomInexistente_DeveRetornarBadRequest()
    {
        using var anonimo = fabrica.CriarCliente();

        var resposta = await anonimo.PostAsJsonAsync("/api/v1/cupons/validar", new { codigo = "NAOEXISTE", subtotal = 100m });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ComUsuarioComum_DeveRetornarForbidden()
    {
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/cupons", new { codigo = "HACK", tipo = TipoDeDesconto.Percentual, valor = 100m });

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Criar_CodigoDuplicado_DeveRetornarBadRequest()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var codigo = CodigoUnico("DUP");
        await CriarCupomAsync(admin, new { codigo, tipo = TipoDeDesconto.ValorFixo, valor = 5m });

        var resposta = await admin.PostAsJsonAsync("/api/v1/cupons", new { codigo, tipo = TipoDeDesconto.ValorFixo, valor = 5m });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_ComCupom_DeveAplicarDescontoECobrarTotalReduzido()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 100m);
        var codigo = CodigoUnico("VINTE");
        await CriarCupomAsync(admin, new { codigo, tipo = TipoDeDesconto.ValorFixo, valor = 20m, limiteDeUsos = 1 });

        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id, 2);

        var resposta = await cliente.FinalizarCompraAsync(codigo);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkout = await resposta.Content.ReadFromJsonAsync<CheckoutResultado>();
        checkout!.Pedido.Subtotal.Should().Be(200m);
        checkout.Pedido.Desconto.Should().Be(20m);
        // O cupom desconta só os produtos; o frete entra inteiro.
        checkout.Pedido.Total.Should().Be(180m + AuxiliaresDeTeste.FretePac);
        checkout.Pedido.CodigoCupom.Should().Be(codigo);

        var cupons = await admin.GetFromJsonAsync<List<CupomDto>>("/api/v1/cupons");
        cupons!.Single(c => c.Codigo == codigo).QuantidadeDeUsos.Should().Be(1);
    }

    [Fact]
    public async Task Checkout_ComCupomEsgotado_DeveRetornarBadRequestSemCriarPedido()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 50m, estoque: 10);
        var varianteId = produto.Variantes.Single().Id;
        var codigo = CodigoUnico("UNICO");
        await CriarCupomAsync(admin, new { codigo, tipo = TipoDeDesconto.ValorFixo, valor = 10m, limiteDeUsos = 1 });

        var (primeiro, _) = await fabrica.CriarClienteLogadoAsync();
        await primeiro.AdicionarAoCarrinhoAsync(varianteId);
        (await primeiro.FinalizarCompraAsync(codigo)).EnsureSuccessStatusCode();

        var (segundo, _) = await fabrica.CriarClienteLogadoAsync();
        await segundo.AdicionarAoCarrinhoAsync(varianteId);
        var resposta = await segundo.FinalizarCompraAsync(codigo);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await segundo.GetFromJsonAsync<ResultadoPaginado<PedidoResumoDto>>("/api/v1/pedidos"))!.Itens.Should().BeEmpty();
        // Carrinho e estoque intactos — nada foi gravado.
        (await segundo.GetFromJsonAsync<CarrinhoDto>("/api/v1/carrinho"))!.Itens.Should().ContainSingle();
        (await admin.GetFromJsonAsync<ProdutoDto>($"/api/v1/produtos/{produto.Id}"))!.Variantes.Single().Estoque.Should().Be(9);
    }

    [Fact]
    public async Task Checkout_ComCupomDeCemPorCento_DeveCobrarSoOFrete()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 30m);
        var codigo = CodigoUnico("CORTESIA");
        await CriarCupomAsync(admin, new { codigo, tipo = TipoDeDesconto.Percentual, valor = 100m });

        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id);
        var resposta = await cliente.FinalizarCompraAsync(codigo);

        var checkout = await resposta.Content.ReadFromJsonAsync<CheckoutResultado>();
        checkout!.Pedido.Desconto.Should().Be(30m);
        checkout.Pedido.Total.Should().Be(AuxiliaresDeTeste.FretePac);
        checkout.Pedido.Status.Should().Be(StatusPedido.Pendente);
        checkout.ReferenciaPagamento.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancelar_PedidoPendente_DeveDevolverEstoque()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(estoque: 5);

        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id, 3);
        var checkout = await (await cliente.FinalizarCompraAsync()).Content.ReadFromJsonAsync<CheckoutResultado>();
        (await admin.GetFromJsonAsync<ProdutoDto>($"/api/v1/produtos/{produto.Id}"))!.Variantes.Single().Estoque.Should().Be(2);

        (await cliente.PostAsync($"/api/v1/pedidos/{checkout!.Pedido.Id}/cancelar", null)).EnsureSuccessStatusCode();

        (await admin.GetFromJsonAsync<ProdutoDto>($"/api/v1/produtos/{produto.Id}"))!.Variantes.Single().Estoque.Should().Be(5);
    }
}
