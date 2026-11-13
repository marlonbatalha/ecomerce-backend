using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Contas;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.IntegrationTests.Vendas;

/// <summary>
/// Regras do checkout com Pix e boleto que não dependem do gateway (roda com o fake): CPF
/// obrigatório, método válido, instruções no pedido. A integração com o Mercado Pago em si fica em
/// <see cref="MercadoPagoTests"/>.
/// </summary>
public class PixEBoletoTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private async Task<(HttpClient Cliente, ProdutoDto Produto)> PrepararCarrinhoAsync(bool comCpf = true, int estoque = 10)
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 50m, estoque: estoque);
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync(comCpf);
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id);
        return (cliente, produto);
    }

    [Theory]
    [InlineData("Pix")]
    [InlineData("boleto")] // não diferencia maiúsculas
    public async Task Checkout_DeveDevolverInstrucoesDoMetodoEscolhido(string metodo)
    {
        var (cliente, _) = await PrepararCarrinhoAsync();

        var resposta = await cliente.FinalizarCompraAsync(metodoDePagamento: metodo);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkout = await resposta.Content.ReadFromJsonAsync<CheckoutResultado>();
        var instrucoes = checkout!.Pedido.Pagamento!;
        if (metodo == "Pix")
        {
            instrucoes.Metodo.Should().Be(MetodoDePagamento.Pix);
            instrucoes.CodigoPix.Should().NotBeNullOrEmpty();
        }
        else
        {
            instrucoes.Metodo.Should().Be(MetodoDePagamento.Boleto);
            instrucoes.LinhaDigitavel.Should().NotBeNullOrEmpty();
        }

        instrucoes.ExpiraEmUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Checkout_SemCpfNoPerfil_DeveRecusarSemReservarEstoque()
    {
        var (cliente, produto) = await PrepararCarrinhoAsync(comCpf: false, estoque: 4);

        var resposta = await cliente.FinalizarCompraAsync();

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("CPF");
        var depois = await cliente.GetFromJsonAsync<ProdutoDto>($"/api/v1/produtos/{produto.Id}");
        depois!.Variantes.Single().Estoque.Should().Be(4);

        // Cadastrou o CPF: agora passa, com o mesmo carrinho.
        await cliente.CadastrarCpfAsync();
        (await cliente.FinalizarCompraAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Cartao")]
    [InlineData("1")]
    [InlineData("Pix,Boleto")]
    [InlineData("")]
    public async Task Checkout_ComMetodoInvalido_DeveRetornarBadRequest(string metodo)
    {
        var (cliente, _) = await PrepararCarrinhoAsync();

        var resposta = await cliente.FinalizarCompraAsync(metodoDePagamento: metodo);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Perfil_DeveGuardarCpfSoComDigitos()
    {
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync(comCpf: false);
        var cpf = AuxiliaresDeTeste.GerarCpf();
        var comPontuacao = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";

        var resposta = await cliente.PutAsJsonAsync("/api/v1/conta/perfil", new { nome = "Maria da Silva", cpf = comPontuacao });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var perfil = await cliente.GetFromJsonAsync<PerfilDto>("/api/v1/conta/perfil");
        perfil!.Cpf.Should().Be(cpf);
        perfil.Nome.Should().Be("Maria da Silva");
    }

    [Fact]
    public async Task Perfil_SemCpfNaRequisicao_DeveManterOAtual()
    {
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync(comCpf: false);
        var cpf = await cliente.CadastrarCpfAsync();

        await cliente.PutAsJsonAsync("/api/v1/conta/perfil", new { nome = "Outro Nome" });

        var perfil = await cliente.GetFromJsonAsync<PerfilDto>("/api/v1/conta/perfil");
        perfil!.Cpf.Should().Be(cpf);
    }

    [Fact]
    public async Task Perfil_ComCpfInvalido_DeveRetornarBadRequest()
    {
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync(comCpf: false);

        var resposta = await cliente.PutAsJsonAsync("/api/v1/conta/perfil", new { nome = "Maria", cpf = "111.111.111-11" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Perfil_ComCpfDeOutraConta_DeveRetornarBadRequest()
    {
        var (primeiro, _) = await fabrica.CriarClienteLogadoAsync(comCpf: false);
        var cpf = await primeiro.CadastrarCpfAsync();
        var (segundo, _) = await fabrica.CriarClienteLogadoAsync(comCpf: false);

        var resposta = await segundo.PutAsJsonAsync("/api/v1/conta/perfil", new { nome = "Outra Pessoa", cpf });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("outra conta");
    }
}
