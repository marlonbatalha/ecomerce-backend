using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Application.Catalogo;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.IntegrationTests.Catalogo;

public class ProdutoEndpointsTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
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
        var respostaLogin = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        respostaLogin.EnsureSuccessStatusCode();

        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        return cliente;
    }

    private static async Task<Guid> CriarCategoriaAsync(HttpClient clienteAdmin, string nome)
    {
        var resposta = await clienteAdmin.PostAsJsonAsync("/api/v1/categorias", new { nome });
        resposta.EnsureSuccessStatusCode();
        var categoria = await resposta.Content.ReadFromJsonAsync<CategoriaDto>();
        return categoria!.Id;
    }

    [Fact]
    public async Task Buscar_SemProdutos_DeveRetornarPaginaVazia()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/v1/produtos?termo=produto-que-nao-existe-xyz");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await resposta.Content.ReadFromJsonAsync<ResultadoPaginado<ProdutoResumoDto>>();
        pagina!.Itens.Should().BeEmpty();
        pagina.TotalItens.Should().Be(0);
    }

    [Fact]
    public async Task Criar_SemAutenticacao_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = "Produto",
            descricao = "desc",
            sku = "sku-teste",
            preco = 10m,
            categoriaId = Guid.NewGuid(),
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComCategoriaInexistente_DeveRetornarBadRequest()
    {
        using var cliente = await CriarClienteAdminAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = "Produto Órfão",
            descricao = "desc",
            sku = $"sku-{Guid.NewGuid():N}",
            preco = 10m,
            categoriaId = Guid.NewGuid(),
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FluxoCompleto_CriarProdutoComVariante_DeveApareceNaBuscaPublicaEDetalhe()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var categoriaId = await CriarCategoriaAsync(clienteAdmin, $"Categoria {Guid.NewGuid():N}");
        var sku = $"sku-{Guid.NewGuid():N}";

        var respostaCriar = await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = "Camiseta de Teste",
            descricao = "100% algodão",
            sku,
            preco = 89.90m,
            categoriaId,
        });
        respostaCriar.StatusCode.Should().Be(HttpStatusCode.Created);
        var produto = await respostaCriar.Content.ReadFromJsonAsync<ProdutoDto>();

        // Sem variante ativa em estoque ainda -> não aparece na busca pública.
        var buscaAntes = await clienteAdmin.GetAsync($"/api/v1/produtos?categoriaId={categoriaId}");
        var paginaAntes = await buscaAntes.Content.ReadFromJsonAsync<ResultadoPaginado<ProdutoResumoDto>>();
        paginaAntes!.Itens.Should().NotContain(p => p.Id == produto!.Id);

        var respostaVariante = await clienteAdmin.PostAsJsonAsync($"/api/v1/produtos/{produto!.Id}/variantes", new
        {
            nome = "Único",
            sku = $"sku-variante-{Guid.NewGuid():N}",
            preco = 89.90m,
            estoque = 5,
        });
        respostaVariante.StatusCode.Should().Be(HttpStatusCode.OK);

        // Agora deve aparecer.
        using var clientePublico = CriarCliente();
        var buscaDepois = await clientePublico.GetAsync($"/api/v1/produtos?categoriaId={categoriaId}");
        var paginaDepois = await buscaDepois.Content.ReadFromJsonAsync<ResultadoPaginado<ProdutoResumoDto>>();
        paginaDepois!.Itens.Should().ContainSingle(p => p.Id == produto.Id);

        var respostaDetalhe = await clientePublico.GetAsync($"/api/v1/produtos/{produto.Id}");
        respostaDetalhe.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalhe = await respostaDetalhe.Content.ReadFromJsonAsync<ProdutoDto>();
        detalhe!.Variantes.Should().ContainSingle(v => v.Estoque == 5);
    }

    [Fact]
    public async Task Criar_ComSkuDuplicado_DeveRetornarBadRequest()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var categoriaId = await CriarCategoriaAsync(clienteAdmin, $"Categoria {Guid.NewGuid():N}");
        var sku = $"sku-duplicado-{Guid.NewGuid():N}";

        var corpo = new { nome = "Produto A", descricao = "desc", sku, preco = 10m, categoriaId };
        var primeira = await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", corpo);
        primeira.StatusCode.Should().Be(HttpStatusCode.Created);

        var segunda = await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", corpo);
        segunda.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InativarVariante_UltimaAtiva_DeveRetornarBadRequest()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var categoriaId = await CriarCategoriaAsync(clienteAdmin, $"Categoria {Guid.NewGuid():N}");

        var respostaCriar = await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = "Produto Único",
            descricao = "desc",
            sku = $"sku-{Guid.NewGuid():N}",
            preco = 10m,
            categoriaId,
        });
        var produto = await respostaCriar.Content.ReadFromJsonAsync<ProdutoDto>();

        var respostaVariante = await clienteAdmin.PostAsJsonAsync($"/api/v1/produtos/{produto!.Id}/variantes", new
        {
            nome = "Único",
            sku = $"sku-variante-{Guid.NewGuid():N}",
            preco = 10m,
            estoque = 1,
        });
        var produtoComVariante = await respostaVariante.Content.ReadFromJsonAsync<ProdutoDto>();
        var varianteId = produtoComVariante!.Variantes.Single().Id;

        var respostaInativar = await clienteAdmin.PostAsync($"/api/v1/produtos/{produto.Id}/variantes/{varianteId}/inativar", null);

        respostaInativar.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AjustarEstoque_ComUsuarioComum_DeveRetornarForbidden()
    {
        using var clienteAdmin = await CriarClienteAdminAsync();
        var categoriaId = await CriarCategoriaAsync(clienteAdmin, $"Categoria {Guid.NewGuid():N}");

        var respostaCriar = await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = "Produto",
            descricao = "desc",
            sku = $"sku-{Guid.NewGuid():N}",
            preco = 10m,
            categoriaId,
        });
        var produto = await respostaCriar.Content.ReadFromJsonAsync<ProdutoDto>();

        var emailCliente = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";
        using var clienteComum = CriarCliente();
        await clienteComum.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente", email = emailCliente, senha });
        var respostaLogin = await clienteComum.PostAsJsonAsync("/api/v1/auth/login", new { email = emailCliente, senha });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        clienteComum.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var respostaAjuste = await clienteComum.PutAsJsonAsync(
            $"/api/v1/produtos/{produto!.Id}/variantes/{Guid.NewGuid()}/estoque", new { novoEstoque = 100 });

        respostaAjuste.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
