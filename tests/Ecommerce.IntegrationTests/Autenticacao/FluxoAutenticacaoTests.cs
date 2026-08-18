using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Api.Autenticacao;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.IntegrationTests.Autenticacao;

public class FluxoAutenticacaoTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private HttpClient CriarCliente() => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        // BaseAddress https: o cookie do refresh token é Secure — sem isso o HttpClient
        // descarta o cookie e a renovação nunca recebe o valor de volta.
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    [Fact]
    public async Task FluxoCompleto_Registrar_Login_EndpointProtegido_Renovar_DeveFuncionar()
    {
        using var cliente = CriarCliente();
        var email = $"teste-{Guid.NewGuid():N}@exemplo.com";
        var senha = "SenhaForte123!";

        var respostaRegistro = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Usuário de Teste", email, senha });
        respostaRegistro.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenRegistro = await respostaRegistro.Content.ReadFromJsonAsync<TokenRespostaDto>();
        tokenRegistro.Should().NotBeNull();
        tokenRegistro!.AccessToken.Should().NotBeNullOrWhiteSpace();

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRegistro.AccessToken);
        var respostaEu = await cliente.GetAsync("/api/v1/auth/eu");
        respostaEu.StatusCode.Should().Be(HttpStatusCode.OK);

        cliente.DefaultRequestHeaders.Authorization = null;
        var respostaLogin = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        respostaLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenLogin = await respostaLogin.Content.ReadFromJsonAsync<TokenRespostaDto>();

        // Renova via refresh token — enviado automaticamente pelo cookie httpOnly setado no login.
        var respostaRenovar = await cliente.PostAsync("/api/v1/auth/atualizar-token", null);
        respostaRenovar.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenRenovado = await respostaRenovar.Content.ReadFromJsonAsync<TokenRespostaDto>();
        tokenRenovado.Should().NotBeNull();
        tokenRenovado!.AccessToken.Should().NotBe(tokenLogin!.AccessToken);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRenovado.AccessToken);
        var respostaEuDeNovo = await cliente.GetAsync("/api/v1/auth/eu");
        respostaEuDeNovo.StatusCode.Should().Be(HttpStatusCode.OK);

        // Logout revoga o refresh token — uma segunda renovação com o mesmo cookie deve falhar.
        var respostaLogout = await cliente.PostAsync("/api/v1/auth/logout", null);
        respostaLogout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Registrar_ComSenhaFraca_DeveRetornarBadRequest()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new
        {
            nome = "Usuário",
            email = $"fraco-{Guid.NewGuid():N}@exemplo.com",
            senha = "123",
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "inexistente@exemplo.com",
            senha = "QualquerSenha123!",
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Eu_SemToken_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/v1/auth/eu");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
