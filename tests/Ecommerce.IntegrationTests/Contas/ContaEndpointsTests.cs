using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Application.Comum;
using Ecommerce.Application.Contas;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.IntegrationTests.Contas;

public class ContaEndpointsTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private HttpClient CriarCliente() => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    private async Task<HttpClient> CriarClienteLogadoAsync(string? nome = null)
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";

        var cliente = CriarCliente();
        await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = nome ?? "Cliente de Teste", email, senha });
        var respostaLogin = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        return cliente;
    }

    private static object EnderecoValido(string rotulo = "Casa") => new
    {
        rotulo,
        logradouro = "Rua das Flores",
        numero = "123",
        complemento = "Apto 45",
        bairro = "Centro",
        cidade = "São Paulo",
        estado = "SP",
        cep = "01310-100",
    };

    [Fact]
    public async Task Criar_PrimeiroEndereco_DeveNascerComoPadrao()
    {
        using var cliente = await CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/conta/enderecos", EnderecoValido());

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var endereco = await resposta.Content.ReadFromJsonAsync<EnderecoDto>();
        endereco!.Padrao.Should().BeTrue();
    }

    [Fact]
    public async Task MarcarComoPadrao_DeveDesmarcarOutroEnderecoPadrao()
    {
        using var cliente = await CriarClienteLogadoAsync();

        var respostaPrimeiro = await cliente.PostAsJsonAsync("/api/v1/conta/enderecos", EnderecoValido("Casa"));
        var primeiro = await respostaPrimeiro.Content.ReadFromJsonAsync<EnderecoDto>();

        var respostaSegundo = await cliente.PostAsJsonAsync("/api/v1/conta/enderecos", EnderecoValido("Trabalho"));
        var segundo = await respostaSegundo.Content.ReadFromJsonAsync<EnderecoDto>();
        segundo!.Padrao.Should().BeFalse();

        await cliente.PostAsync($"/api/v1/conta/enderecos/{segundo.Id}/padrao", null);

        var respostaLista = await cliente.GetAsync("/api/v1/conta/enderecos");
        var lista = await respostaLista.Content.ReadFromJsonAsync<List<EnderecoDto>>();

        lista!.Single(e => e.Id == segundo.Id).Padrao.Should().BeTrue();
        lista!.Single(e => e.Id == primeiro!.Id).Padrao.Should().BeFalse();
    }

    [Fact]
    public async Task Remover_EnderecoDeOutroUsuario_DeveRetornarBadRequest()
    {
        using var clienteA = await CriarClienteLogadoAsync();
        var respostaCriar = await clienteA.PostAsJsonAsync("/api/v1/conta/enderecos", EnderecoValido());
        var endereco = await respostaCriar.Content.ReadFromJsonAsync<EnderecoDto>();

        using var clienteB = await CriarClienteLogadoAsync();
        var respostaRemover = await clienteB.DeleteAsync($"/api/v1/conta/enderecos/{endereco!.Id}");

        // IDOR: cliente B não pode mexer no endereço do cliente A — tratado como "não encontrado".
        respostaRemover.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Enderecos_SemAutenticacao_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/v1/conta/enderecos");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Perfil_ObterEAtualizar_DeveFuncionar()
    {
        using var cliente = await CriarClienteLogadoAsync("Nome Original");

        var respostaObter = await cliente.GetAsync("/api/v1/conta/perfil");
        var perfil = await respostaObter.Content.ReadFromJsonAsync<PerfilDto>();
        perfil!.Nome.Should().Be("Nome Original");

        var respostaAtualizar = await cliente.PutAsJsonAsync("/api/v1/conta/perfil", new { nome = "Nome Novo" });
        respostaAtualizar.StatusCode.Should().Be(HttpStatusCode.OK);
        var perfilAtualizado = await respostaAtualizar.Content.ReadFromJsonAsync<PerfilDto>();
        perfilAtualizado!.Nome.Should().Be("Nome Novo");
    }

    [Fact]
    public async Task TrocarSenha_ComSenhaAtualCorreta_DevePermitirLoginComANovaSenha()
    {
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        const string senhaOriginal = "SenhaForte123!";
        const string senhaNova = "OutraSenhaForte456!";

        var cliente = CriarCliente();
        await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Cliente", email, senha = senhaOriginal });
        var respostaLogin = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = senhaOriginal });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var respostaTrocar = await cliente.PostAsJsonAsync("/api/v1/conta/trocar-senha", new { senhaAtual = senhaOriginal, novaSenha = senhaNova });
        respostaTrocar.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var novoCliente = CriarCliente();
        var respostaLoginNovo = await novoCliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = senhaNova });
        respostaLoginNovo.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrocarSenha_ComSenhaAtualErrada_DeveRetornarBadRequest()
    {
        using var cliente = await CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/conta/trocar-senha", new { senhaAtual = "SenhaErrada999!", novaSenha = "NovaSenhaForte123!" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Clientes_ComUsuarioComum_DeveRetornarForbidden()
    {
        using var cliente = await CriarClienteLogadoAsync();

        var resposta = await cliente.GetAsync("/api/v1/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Clientes_ComAdmin_DeveEncontrarPorNome()
    {
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";
        await fabrica.CriarUsuarioAdminAsync(email, senha);

        using var clienteAdmin = CriarCliente();
        var respostaLogin = await clienteAdmin.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        var token = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();
        clienteAdmin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var nomeUnico = $"ClienteBuscavel{Guid.NewGuid():N}";
        using var novoCliente = await CriarClienteLogadoAsync(nomeUnico);

        var respostaBusca = await clienteAdmin.GetAsync($"/api/v1/clientes?termo={nomeUnico}");
        respostaBusca.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagina = await respostaBusca.Content.ReadFromJsonAsync<ResultadoPaginado<ClienteResumoDto>>();
        pagina!.Itens.Should().ContainSingle(c => c.Nome == nomeUnico);
    }
}
