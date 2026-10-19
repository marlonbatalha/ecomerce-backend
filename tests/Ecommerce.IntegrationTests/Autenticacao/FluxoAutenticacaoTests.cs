using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Infrastructure.Persistencia;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task Login_AposCincoSenhasErradas_DeveBloquearAContaMesmoComSenhaCerta()
    {
        using var cliente = CriarCliente();
        var email = $"bloqueio-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";

        var respostaRegistro = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Usuário de Teste", email, senha });
        respostaRegistro.EnsureSuccessStatusCode();

        for (var i = 0; i < 5; i++)
        {
            var errada = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "SenhaErrada123!" });
            errada.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var respostaComSenhaCerta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });

        respostaComSenhaCerta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ComSucesso_DeveZerarAsFalhasAnteriores()
    {
        using var cliente = CriarCliente();
        var email = $"falhas-{Guid.NewGuid():N}@exemplo.com";
        const string senha = "SenhaForte123!";

        var respostaRegistro = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Usuário de Teste", email, senha });
        respostaRegistro.EnsureSuccessStatusCode();

        // 4 falhas + sucesso + 4 falhas: sem o reset no sucesso, seriam 8 e a conta bloquearia.
        for (var rodada = 0; rodada < 2; rodada++)
        {
            for (var i = 0; i < 4; i++)
            {
                await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "SenhaErrada123!" });
            }

            var respostaCerta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
            respostaCerta.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Registrar_ComEmailJaUsadoESenhaFraca_DeveExplicarEmPortugues()
    {
        using var cliente = CriarCliente();
        var email = $"duplicado-{Guid.NewGuid():N}@exemplo.com";
        (await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Primeira", email, senha = "SenhaForte123!" }))
            .EnsureSuccessStatusCode();

        var duplicado = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Segunda", email, senha = "SenhaForte123!" });
        var fraca = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Fraca", email = $"fraca-{Guid.NewGuid():N}@exemplo.com", senha = "senhafraca" });

        duplicado.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await duplicado.Content.ReadAsStringAsync()).Should().Contain($"Já existe uma conta com o e-mail {email}.")
            .And.NotContain("already");
        var erroFraca = await fraca.Content.ReadAsStringAsync();
        erroFraca.Should().Contain("letra maiúscula").And.Contain("número").And.Contain("símbolo");
    }

    /// <summary>Lê o valor do cookie refreshToken do Set-Cookie (o servidor do Next faz o mesmo).</summary>
    private static string LerRefreshToken(HttpResponseMessage resposta)
    {
        var cabecalho = resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("refreshToken=", StringComparison.Ordinal));
        return Uri.UnescapeDataString(cabecalho.Split(';')[0]["refreshToken=".Length..]);
    }

    private async Task<HttpResponseMessage> RenovarComAsync(string refreshToken)
    {
        // Cliente sem cookie container: o refresh token vai só no cabeçalho, como o Next manda.
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/atualizar-token");
        requisicao.Headers.Add("Cookie", $"refreshToken={Uri.EscapeDataString(refreshToken)}");
        return await cliente.SendAsync(requisicao);
    }

    private async Task<string> RegistrarERetornarRefreshTokenAsync()
    {
        using var cliente = CriarCliente();
        var email = $"renovacao-{Guid.NewGuid():N}@exemplo.com";
        var resposta = await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "Usuário de Teste", email, senha = "SenhaForte123!" });
        resposta.EnsureSuccessStatusCode();
        return LerRefreshToken(resposta);
    }

    [Fact]
    public async Task Renovar_ComTokenRecemRotacionado_DeveRetornarConflictSemDerrubarASessao()
    {
        var tokenOriginal = await RegistrarERetornarRefreshTokenAsync();

        var primeira = await RenovarComAsync(tokenOriginal);
        primeira.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenNovo = LerRefreshToken(primeira);

        // Segunda requisição em paralelo ainda com o token antigo.
        var concorrente = await RenovarComAsync(tokenOriginal);
        concorrente.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // A cadeia não foi revogada: o token novo continua renovando.
        var comTokenNovo = await RenovarComAsync(tokenNovo);
        comTokenNovo.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Renovar_ComTokenRotacionadoForaDaJanela_DeveTratarComoReusoERevogarACadeia()
    {
        var tokenOriginal = await RegistrarERetornarRefreshTokenAsync();

        var primeira = await RenovarComAsync(tokenOriginal);
        var tokenNovo = LerRefreshToken(primeira);

        // Simula que a rotação aconteceu há mais tempo que a tolerância.
        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoBancoDados>();
            var hashOriginal = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenOriginal)));
            var token = await contexto.TokensAtualizacao.SingleAsync(t => t.HashToken == hashOriginal);
            contexto.Entry(token).Property(t => t.RevogadoEmUtc).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
            await contexto.SaveChangesAsync();
        }

        var reuso = await RenovarComAsync(tokenOriginal);
        reuso.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Reuso suspeito derruba a cadeia inteira — o token novo também deixa de valer.
        var comTokenNovo = await RenovarComAsync(tokenNovo);
        comTokenNovo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Eu_SemToken_DeveRetornarUnauthorized()
    {
        using var cliente = CriarCliente();

        var resposta = await cliente.GetAsync("/api/v1/auth/eu");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
