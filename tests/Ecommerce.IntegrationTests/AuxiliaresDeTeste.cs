using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Application.Catalogo;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.IntegrationTests;

/// <summary>Atalhos de arrange compartilhados pelos testes de integração mais novos.</summary>
public static class AuxiliaresDeTeste
{
    public const string Senha = "SenhaForte123!";

    public static HttpClient CriarCliente(this FabricaDeAplicacaoTeste fabrica) => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    public static async Task<HttpClient> CriarClienteAdminAsync(this FabricaDeAplicacaoTeste fabrica)
    {
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";
        await fabrica.CriarUsuarioAdminAsync(email, Senha);
        return await LogarAsync(fabrica.CriarCliente(), email);
    }

    public static async Task<(HttpClient Cliente, string Email)> CriarClienteLogadoAsync(this FabricaDeAplicacaoTeste fabrica)
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        var cliente = fabrica.CriarCliente();
        (await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente Teste", email, senha = Senha })).EnsureSuccessStatusCode();
        return (await LogarAsync(cliente, email), email);
    }

    public static async Task<ProdutoDto> CriarProdutoComVarianteAsync(this HttpClient clienteAdmin, decimal preco = 100m, int estoque = 10)
    {
        var categoria = await (await clienteAdmin.PostAsJsonAsync("/api/v1/categorias", new { nome = $"Categoria {Guid.NewGuid():N}" }))
            .Content.ReadFromJsonAsync<CategoriaDto>();

        var produto = await (await clienteAdmin.PostAsJsonAsync("/api/v1/produtos", new
        {
            nome = $"Produto {Guid.NewGuid():N}",
            descricao = "",
            sku = $"sku-{Guid.NewGuid():N}",
            preco,
            categoriaId = categoria!.Id,
        })).Content.ReadFromJsonAsync<ProdutoDto>();

        var resposta = await clienteAdmin.PostAsJsonAsync($"/api/v1/produtos/{produto!.Id}/variantes", new
        {
            nome = "Único",
            sku = $"sku-var-{Guid.NewGuid():N}",
            preco,
            estoque,
        });
        resposta.EnsureSuccessStatusCode();

        return (await resposta.Content.ReadFromJsonAsync<ProdutoDto>())!;
    }

    public static async Task AdicionarAoCarrinhoAsync(this HttpClient cliente, Guid varianteId, int quantidade = 1) =>
        (await cliente.PostAsJsonAsync("/api/v1/carrinho/itens", new { varianteDeProdutoId = varianteId, quantidade })).EnsureSuccessStatusCode();

    private static async Task<HttpClient> LogarAsync(HttpClient cliente, string email)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });
        resposta.EnsureSuccessStatusCode();
        var token = await resposta.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return cliente;
    }
}
