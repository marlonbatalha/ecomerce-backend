using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Comum;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Vendas;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ecommerce.IntegrationTests.Vendas;

public class FluxoCompraTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private HttpClient CriarCliente() => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    private async Task<HttpClient> CriarClienteAdminAsync()
    {
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";
        await fabrica.CriarUsuarioAdminAsync(email, senha);

        var cliente = CriarCliente();
        var resposta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        var token = await resposta.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        return cliente;
    }

    private static async Task<(Guid ProdutoId, Guid VarianteId)> CriarProdutoComEstoqueAsync(HttpClient clienteAdmin, int estoque = 10)
    {
        var respostaCategoria = await clienteAdmin.PostAsJsonAsync("/api/v1/categorias", new { nome = $"Categoria {Guid.NewGuid():N}" });
        var categoria = await respostaCategoria.Content.ReadFromJsonAsync<CategoriaDto>();

        var respostaProduto = await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = "Produto de Teste",
            descricao = "desc",
            sku = $"sku-{Guid.NewGuid():N}",
            preco = 100m,
            categoriaId = categoria!.Id,
        });
        var produto = await respostaProduto.Content.ReadFromJsonAsync<ProdutoDto>();

        var respostaVariante = await clienteAdmin.PostAsJsonAsync($"/api/v1/produtos/{produto!.Id}/variantes", new
        {
            nome = "Único",
            sku = $"sku-variante-{Guid.NewGuid():N}",
            preco = 100m,
            estoque,
        });
        var produtoComVariante = await respostaVariante.Content.ReadFromJsonAsync<ProdutoDto>();

        return (produto.Id, produtoComVariante!.Variantes.Single().Id);
    }

    private async Task<HttpClient> CriarClienteLogadoAsync()
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";

        var clienteRegistro = CriarCliente();
        await clienteRegistro.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente", email, senha });

        var respostaLogin = await clienteRegistro.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        clienteRegistro.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        return clienteRegistro;
    }

    private string AssinarWebhook(string referenciaExterna, bool aprovado)
    {
        var chaveSecreta = fabrica.Services.GetRequiredService<IOptions<OpcoesWebhook>>().Value.ChaveSecreta;
        var mensagem = $"{referenciaExterna}|{aprovado}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(chaveSecreta), Encoding.UTF8.GetBytes(mensagem));
        return Convert.ToHexString(hash);
    }

    [Fact]
    public async Task FluxoCompleto_AdicionarAoCarrinho_Checkout_Webhook_Enviar_Entregar_DeveFuncionar()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (_, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin, estoque: 10);

        using var cliente = await CriarClienteLogadoAsync();

        var respostaAdicionar = await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { varianteDeProdutoId = varianteId, quantidade = 2 });
        respostaAdicionar.StatusCode.Should().Be(HttpStatusCode.OK);
        var carrinho = await respostaAdicionar.Content.ReadFromJsonAsync<CarrinhoDto>();
        carrinho!.Total.Should().Be(200m);
        carrinho.Itens.Single().EstoqueDisponivel.Should().Be(10);
        carrinho.Itens.Single().ProdutoSlug.Should().NotBeNullOrWhiteSpace();

        var respostaCheckout = await cliente.FinalizarCompraAsync();
        respostaCheckout.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkout = await respostaCheckout.Content.ReadFromJsonAsync<CheckoutResultado>();
        checkout!.Pedido.Status.Should().Be(StatusPedido.Pendente);
        checkout.Pedido.Total.Should().Be(200m + AuxiliaresDeTeste.FretePac);
        checkout.Pedido.MetodoDeEnvio.Should().Be("PAC");
        checkout.Pedido.ValorFrete.Should().Be(AuxiliaresDeTeste.FretePac);
        checkout.Pedido.Entrega!.Cep.Should().Be("01310-100");
        checkout.Pedido.Entrega.Cidade.Should().Be("São Paulo");

        // Carrinho foi esvaziado no checkout.
        var respostaCarrinhoDepois = await cliente.GetAsync("/api/v1/carrinho");
        var carrinhoDepois = await respostaCarrinhoDepois.Content.ReadFromJsonAsync<CarrinhoDto>();
        carrinhoDepois!.Itens.Should().BeEmpty();

        // Webhook confirma o pagamento.
        var assinatura = AssinarWebhook(checkout.ReferenciaPagamento!, aprovado: true);
        using var requisicaoWebhook = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pagamentos/webhook")
        {
            Content = JsonContent.Create(new { referenciaExterna = checkout.ReferenciaPagamento, aprovado = true }),
        };
        requisicaoWebhook.Headers.Add("X-Assinatura-Webhook", assinatura);
        var respostaWebhook = await cliente.SendAsync(requisicaoWebhook);
        respostaWebhook.StatusCode.Should().Be(HttpStatusCode.OK);

        var respostaPedido = await cliente.GetAsync($"/api/v1/pedidos/{checkout.Pedido.Id}");
        var pedidoConfirmado = await respostaPedido.Content.ReadFromJsonAsync<PedidoDto>();
        pedidoConfirmado!.Status.Should().Be(StatusPedido.PagamentoConfirmado);

        // Pago: o cliente não cancela mais sozinho (não haveria estorno).
        var respostaCancelarPago = await cliente.PostAsync($"/api/v1/pedidos/{checkout.Pedido.Id}/cancelar", null);
        respostaCancelarPago.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Admin avança o pedido.
        var respostaEnviar = await clienteAdmin.PostAsJsonAsync($"/api/v1/pedidos/{checkout.Pedido.Id}/enviar", new { codigoRastreio = "BR123456789" });
        respostaEnviar.StatusCode.Should().Be(HttpStatusCode.OK);
        var pedidoEnviado = await respostaEnviar.Content.ReadFromJsonAsync<PedidoDto>();
        pedidoEnviado!.Status.Should().Be(StatusPedido.Enviado);
        pedidoEnviado.CodigoRastreio.Should().Be("BR123456789");

        var respostaEntregar = await clienteAdmin.PostAsync($"/api/v1/pedidos/{checkout.Pedido.Id}/entregar", null);
        respostaEntregar.StatusCode.Should().Be(HttpStatusCode.OK);
        var pedidoEntregue = await respostaEntregar.Content.ReadFromJsonAsync<PedidoDto>();
        pedidoEntregue!.Status.Should().Be(StatusPedido.Entregue);
    }

    [Fact]
    public async Task Webhook_ComAssinaturaInvalida_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pagamentos/webhook")
        {
            Content = JsonContent.Create(new { referenciaExterna = "ref-qualquer", aprovado = true }),
        };
        requisicao.Headers.Add("X-Assinatura-Webhook", "assinatura-forjada");

        var resposta = await cliente.SendAsync(requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_ComCarrinhoVazio_DeveRetornarBadRequest()
    {
        using var cliente = await CriarClienteLogadoAsync();

        var resposta = await cliente.FinalizarCompraAsync();

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_ComQuantidadeAcimaDoEstoqueAtual_DeveRetornarBadRequest()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (produtoId, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin, estoque: 1);

        using var cliente = await CriarClienteLogadoAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { varianteDeProdutoId = varianteId, quantidade = 1 });

        // Admin zera o estoque depois que o item já estava no carrinho — checkout precisa
        // revalidar o estoque atual, não confiar só na checagem de quando entrou no carrinho.
        await clienteAdmin.PutAsJsonAsync(
            $"/api/v1/produtos/{produtoId}/variantes/{varianteId}/estoque", new { novoEstoque = 0, motivo = "Ajuste de teste" });

        var respostaCheckout = await cliente.FinalizarCompraAsync();

        respostaCheckout.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MesclarCarrinhoAnonimo_AoLogar_DeveTransferirItens()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (_, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin, estoque: 10);

        using var cliente = CriarCliente();

        // Adiciona ao carrinho anônimo (sem login).
        var respostaAdicionar = await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { varianteDeProdutoId = varianteId, quantidade = 3 });
        respostaAdicionar.StatusCode.Should().Be(HttpStatusCode.OK);

        // Registra/loga — o carrinho anônimo deve ser mesclado no carrinho do usuário.
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";
        await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente", email, senha });
        var respostaLogin = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var respostaCarrinho = await cliente.GetAsync("/api/v1/carrinho");
        var carrinho = await respostaCarrinho.Content.ReadFromJsonAsync<CarrinhoDto>();

        carrinho!.Itens.Should().ContainSingle(i => i.VarianteDeProdutoId == varianteId && i.Quantidade == 3);
    }

    [Fact]
    public async Task ListarPedidosAdmin_DeveVerPedidoDeQualquerCliente()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (_, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin, estoque: 10);

        using var cliente = await CriarClienteLogadoAsync();
        await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { varianteDeProdutoId = varianteId, quantidade = 1 });
        var respostaCheckout = await cliente.FinalizarCompraAsync();
        var checkout = await respostaCheckout.Content.ReadFromJsonAsync<CheckoutResultado>();

        var respostaListaAdmin = await clienteAdmin.GetAsync("/api/v1/pedidos/admin");
        respostaListaAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await respostaListaAdmin.Content.ReadFromJsonAsync<ResultadoPaginado<PedidoResumoDto>>();
        pagina!.Itens.Should().Contain(p => p.Id == checkout!.Pedido.Id);

        var respostaDetalheAdmin = await clienteAdmin.GetAsync($"/api/v1/pedidos/admin/{checkout!.Pedido.Id}");
        respostaDetalheAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListarPedidosAdmin_ComUsuarioComum_DeveRetornarForbidden()
    {
        using var cliente = await CriarClienteLogadoAsync();

        var resposta = await cliente.GetAsync("/api/v1/pedidos/admin");

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Checkout_SemEnderecoOuMetodoDeEnvio_DeveRetornarBadRequest()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (_, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin);
        using var cliente = await CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(varianteId);

        var semCorpo = await cliente.PostAsync("/api/v1/pedidos/checkout", JsonContent.Create(new { }));

        semCorpo.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await cliente.GetFromJsonAsync<CarrinhoDto>("/api/v1/carrinho"))!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Checkout_ComEnderecoDeOutroUsuario_DeveRecusarSemMexerNoEstoque()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (produtoId, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin, estoque: 5);

        using var outro = await CriarClienteLogadoAsync();
        var enderecoDoOutro = await outro.CriarEnderecoAsync();

        using var cliente = await CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(varianteId, 2);

        var resposta = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { enderecoId = enderecoDoOutro, metodoDeEnvio = "PAC" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("Endereço de entrega não encontrado");
        var produto = await clienteAdmin.GetFromJsonAsync<ProdutoDto>($"/api/v1/produtos/{produtoId}");
        produto!.Variantes.Single().Estoque.Should().Be(5);
    }

    [Fact]
    public async Task Checkout_ComMetodoDeEnvioInexistente_DeveRetornarBadRequest()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (_, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin);
        using var cliente = await CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(varianteId);
        var enderecoId = await cliente.CriarEnderecoAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { enderecoId, metodoDeEnvio = "TELETRANSPORTE" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("Método de envio indisponível");
    }

    [Fact]
    public async Task Checkout_DeveGuardarCopiaDoEnderecoMesmoSeOClienteEditarDepois()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var (_, varianteId) = await CriarProdutoComEstoqueAsync(clienteAdmin);
        using var cliente = await CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(varianteId);
        var enderecoId = await cliente.CriarEnderecoAsync();

        var checkout = await (await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { enderecoId, metodoDeEnvio = "sedex" }))
            .Content.ReadFromJsonAsync<CheckoutResultado>();

        (await cliente.PutAsJsonAsync($"/api/v1/conta/enderecos/{enderecoId}", new
        {
            rotulo = "Casa",
            logradouro = "Rua Nova",
            numero = "1",
            complemento = (string?)null,
            bairro = "Centro",
            cidade = "Campinas",
            estado = "SP",
            cep = "13010-000",
        })).EnsureSuccessStatusCode();

        var pedido = await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{checkout!.Pedido.Id}");
        pedido!.Entrega!.Logradouro.Should().Be("Av. Paulista");
        pedido.Entrega.Cidade.Should().Be("São Paulo");
        pedido.MetodoDeEnvio.Should().Be("SEDEX");
        pedido.ValorFrete.Should().Be(34.90m);
    }
}
