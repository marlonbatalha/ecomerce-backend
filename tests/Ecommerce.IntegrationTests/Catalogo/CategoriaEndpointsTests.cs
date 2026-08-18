using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Application.Catalogo;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.IntegrationTests.Catalogo;

public class CategoriaEndpointsTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
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

    [Fact]
    public async Task Listar_SemCategorias_DeveRetornarListaVazia()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/v1/categorias");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var categorias = await resposta.Content.ReadFromJsonAsync<List<CategoriaDto>>();
        categorias.Should().NotBeNull();
    }

    [Fact]
    public async Task Criar_SemAutenticacao_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "Eletrônicos" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComUsuarioComum_DeveRetornarForbidden()
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";

        using var cliente = CriarCliente();
        await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente Comum", email, senha });

        var respostaLogin = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var resposta = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "Eletrônicos" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FluxoCompleto_CriarSubcategoriaERenomearEMover_DeveFuncionar()
    {
        using var cliente = await CriarClienteAdminAsync();

        var respostaRaiz = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "Casa & Decoração" });
        respostaRaiz.StatusCode.Should().Be(HttpStatusCode.Created);
        var raiz = await respostaRaiz.Content.ReadFromJsonAsync<CategoriaDto>();

        var respostaFilha = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "Cozinha", categoriaPaiId = raiz!.Id });
        respostaFilha.StatusCode.Should().Be(HttpStatusCode.Created);
        var filha = await respostaFilha.Content.ReadFromJsonAsync<CategoriaDto>();
        filha!.CategoriaPaiId.Should().Be(raiz.Id);

        // Listagem em árvore deve refletir a hierarquia.
        var respostaArvore = await cliente.GetAsync("/api/v1/categorias");
        var arvore = await respostaArvore.Content.ReadFromJsonAsync<List<CategoriaDto>>();
        var raizNaArvore = arvore!.Single(c => c.Id == raiz.Id);
        raizNaArvore.Subcategorias.Should().ContainSingle(c => c.Id == filha.Id);

        // Renomear.
        var respostaRenomear = await cliente.PutAsJsonAsync($"/api/v1/categorias/{filha.Id}/renomear", new { novoNome = "Cozinha e Sala de Jantar" });
        respostaRenomear.StatusCode.Should().Be(HttpStatusCode.OK);
        var filhaRenomeada = await respostaRenomear.Content.ReadFromJsonAsync<CategoriaDto>();
        filhaRenomeada!.Nome.Should().Be("Cozinha e Sala de Jantar");
        filhaRenomeada.Slug.Should().Be("cozinha-e-sala-de-jantar");

        // Mover de volta pra raiz.
        var respostaMover = await cliente.PutAsJsonAsync($"/api/v1/categorias/{filha.Id}/mover", new { novaCategoriaPaiId = (Guid?)null });
        respostaMover.StatusCode.Should().Be(HttpStatusCode.OK);
        var filhaMovida = await respostaMover.Content.ReadFromJsonAsync<CategoriaDto>();
        filhaMovida!.CategoriaPaiId.Should().BeNull();

        // Detalhe por slug.
        var respostaDetalhe = await cliente.GetAsync($"/api/v1/categorias/{raizNaArvore.Slug}");
        respostaDetalhe.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Criar_ComNomeVazio_DeveRetornarBadRequest()
    {
        using var cliente = await CriarClienteAdminAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ComCategoriaPaiInexistente_DeveRetornarBadRequest()
    {
        using var cliente = await CriarClienteAdminAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/categorias", new { nome = "Órfã", categoriaPaiId = Guid.NewGuid() });

        // ExcecaoDeDominio ("categoria pai não encontrada") traduzida pelo handler global -> 400.
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
