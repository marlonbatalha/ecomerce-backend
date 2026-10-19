using System.Net;
using System.Net.Http.Json;
using Ecommerce.Api;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Comum;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.IntegrationTests;

/// <summary>
/// Sobe a API com limite baixo na busca de produtos e uma chave de frontend configurada — o
/// limite padrão de Development (1000/min) não dá pra estourar num teste.
/// </summary>
public class FabricaDeAplicacaoComRateLimit : FabricaDeAplicacaoTeste
{
    public const string ChaveDoFrontend = "chave-frontend-teste";
    public const int LimiteDeBusca = 3;

    protected override IDictionary<string, string?> Configuracoes
    {
        get
        {
            var configuracoes = base.Configuracoes;
            configuracoes["Frontend:ChaveDeConfianca"] = ChaveDoFrontend;
            configuracoes[$"RateLimiting:{PoliticasDeRateLimiting.BuscaDeProdutos}"] = LimiteDeBusca.ToString();
            return configuracoes;
        }
    }
}

public class RateLimitingTests(FabricaDeAplicacaoComRateLimit fabrica) : IClassFixture<FabricaDeAplicacaoComRateLimit>
{
    private const string UrlDeBusca = "/api/v1/produtos?termo=camiseta";

    private HttpClient CriarCliente() => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
    });

    /// <summary>Com a chave e sem visitante: não consome a partição sem chave usada por outro teste.</summary>
    private HttpClient CriarClienteDoFrontend()
    {
        var cliente = CriarCliente();
        cliente.DefaultRequestHeaders.Add(IdentificadorDeCliente.CabecalhoChave, FabricaDeAplicacaoComRateLimit.ChaveDoFrontend);
        return cliente;
    }

    private static HttpRequestMessage Busca(string? chave = null, string? ipDoVisitante = null)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Get, UrlDeBusca);
        if (chave is not null)
        {
            requisicao.Headers.Add(IdentificadorDeCliente.CabecalhoChave, chave);
        }

        if (ipDoVisitante is not null)
        {
            requisicao.Headers.Add(IdentificadorDeCliente.CabecalhoIp, ipDoVisitante);
        }

        return requisicao;
    }

    private static async Task<List<HttpStatusCode>> EnviarAsync(HttpClient cliente, int quantidade, Func<HttpRequestMessage> criar)
    {
        var status = new List<HttpStatusCode>();
        for (var i = 0; i < quantidade; i++)
        {
            using var resposta = await cliente.SendAsync(criar());
            status.Add(resposta.StatusCode);
        }

        return status;
    }

    [Fact]
    public async Task Busca_ComChaveDoFrontend_DeveLimitarCadaVisitanteSeparadamente()
    {
        using var cliente = CriarCliente();
        const int limite = FabricaDeAplicacaoComRateLimit.LimiteDeBusca;

        var visitanteA = await EnviarAsync(cliente, limite + 1, () => Busca(FabricaDeAplicacaoComRateLimit.ChaveDoFrontend, "203.0.113.10"));
        var visitanteB = await EnviarAsync(cliente, 1, () => Busca(FabricaDeAplicacaoComRateLimit.ChaveDoFrontend, "203.0.113.11"));

        visitanteA.Take(limite).Should().AllBeEquivalentTo(HttpStatusCode.OK);
        visitanteA.Last().Should().Be(HttpStatusCode.TooManyRequests);
        visitanteB.Should().Equal(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Busca_ComChaveDoFrontendSemVisitante_NaoDeveLimitar()
    {
        // ISR/build do Next: servidor confiável, sem visitante por trás.
        using var cliente = CriarCliente();

        var status = await EnviarAsync(cliente, FabricaDeAplicacaoComRateLimit.LimiteDeBusca * 2, () => Busca(FabricaDeAplicacaoComRateLimit.ChaveDoFrontend));

        status.Should().AllBeEquivalentTo(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Busca_ComChaveErrada_DeveIgnorarIpInformadoPeloCliente()
    {
        // Sem a chave certa, trocar o X-Cliente-IP a cada requisição não pode abrir uma janela nova.
        // TestServer não tem IP remoto: todas caem na mesma partição ("desconhecido"), exclusiva
        // deste teste — os outros testes desta classe sempre mandam a chave certa.
        using var cliente = CriarCliente();
        var contador = 0;

        var status = await EnviarAsync(
            cliente,
            FabricaDeAplicacaoComRateLimit.LimiteDeBusca + 1,
            () => Busca("chave-errada", $"198.51.100.{++contador}"));

        status.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [InlineData("a")]
    [InlineData(" b ")]
    public async Task Busca_ComTermoCurtoDemais_DeveRetornar400(string termo)
    {
        using var cliente = CriarClienteDoFrontend();

        var resposta = await cliente.GetAsync($"/api/v1/produtos?termo={Uri.EscapeDataString(termo)}");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Busca_ComTermoLongoDemais_DeveRetornar400()
    {
        using var cliente = CriarClienteDoFrontend();

        var resposta = await cliente.GetAsync($"/api/v1/produtos?termo={new string('x', 101)}");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Busca_SemParametros_DeveManterPaginacaoPadrao()
    {
        using var cliente = CriarClienteDoFrontend();

        var resposta = await cliente.GetAsync("/api/v1/produtos");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await resposta.Content.ReadFromJsonAsync<ResultadoPaginado<ProdutoResumoDto>>();
        pagina!.Pagina.Should().Be(1);
        pagina.TamanhoPagina.Should().Be(20);
    }
}
