using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Contas;
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

    /// <param name="comCpf">Pix e boleto exigem CPF no perfil; false simula quem ainda não cadastrou.</param>
    public static async Task<(HttpClient Cliente, string Email)> CriarClienteLogadoAsync(this FabricaDeAplicacaoTeste fabrica, bool comCpf = true)
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        var cliente = fabrica.CriarCliente();
        (await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente Teste", email, senha = Senha })).EnsureSuccessStatusCode();
        await LogarAsync(cliente, email);
        if (comCpf)
        {
            await cliente.CadastrarCpfAsync();
        }

        return (cliente, email);
    }

    /// <summary>Grava um CPF válido e único no perfil (o checkout recusa sem ele).</summary>
    public static async Task<string> CadastrarCpfAsync(this HttpClient cliente, string nome = "Cliente Teste")
    {
        var cpf = GerarCpf();
        (await cliente.PutAsJsonAsync("/api/v1/conta/perfil", new { nome, cpf })).EnsureSuccessStatusCode();
        return cpf;
    }

    /// <summary>CPF aleatório com dígitos verificadores corretos (o índice é único entre as contas).</summary>
    public static string GerarCpf()
    {
        var digitos = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
        for (var tamanho = 9; tamanho <= 10; tamanho++)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++)
            {
                soma += digitos[i] * (tamanho + 1 - i);
            }

            var resto = soma % 11;
            digitos.Add(resto < 2 ? 0 : 11 - resto);
        }

        var cpf = string.Concat(digitos);
        return cpf.Distinct().Count() == 1 ? GerarCpf() : cpf;
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

    /// <summary>Valor do PAC no <c>ServicoDeFreteFake</c> — entra no total de todo pedido finalizado por <see cref="FinalizarCompraAsync"/>.</summary>
    public const decimal FretePac = 19.90m;

    public static async Task<Guid> CriarEnderecoAsync(this HttpClient cliente)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/v1/conta/enderecos", new
        {
            rotulo = "Casa",
            logradouro = "Av. Paulista",
            numero = "1000",
            complemento = (string?)null,
            bairro = "Bela Vista",
            cidade = "São Paulo",
            estado = "SP",
            cep = "01310-100",
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<EnderecoDto>())!.Id;
    }

    /// <summary>Cadastra um endereço e finaliza a compra do carrinho atual com PAC (Pix, se não disser outro).</summary>
    public static async Task<HttpResponseMessage> FinalizarCompraAsync(
        this HttpClient cliente, string? codigoCupom = null, string metodoDePagamento = "Pix")
    {
        var enderecoId = await cliente.CriarEnderecoAsync();
        return await cliente.PostAsJsonAsync("/api/v1/pedidos/checkout", new { enderecoId, metodoDeEnvio = "PAC", metodoDePagamento, codigoCupom });
    }

    private static async Task<HttpClient> LogarAsync(HttpClient cliente, string email)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });
        resposta.EnsureSuccessStatusCode();
        var token = await resposta.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return cliente;
    }
}
