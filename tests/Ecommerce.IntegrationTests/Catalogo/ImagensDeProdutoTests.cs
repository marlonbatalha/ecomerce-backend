using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Catalogo;
using FluentAssertions;

namespace Ecommerce.IntegrationTests.Catalogo;

public class ImagensDeProdutoTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private static MultipartFormDataContent CriarArquivo(string tipoConteudo = "image/png")
    {
        var arquivo = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue(tipoConteudo);
        return new MultipartFormDataContent { { arquivo, "arquivo", "foto.png" } };
    }

    private static async Task<ProdutoDto> EnviarImagemAsync(HttpClient admin, Guid produtoId)
    {
        var resposta = await admin.PostAsync($"/api/v1/produtos/{produtoId}/imagens", CriarArquivo());
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resposta.Content.ReadFromJsonAsync<ProdutoDto>())!;
    }

    [Fact]
    public async Task Enviar_DeveSalvarNoStorageEAparecerNoProduto()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();

        var atualizado = await EnviarImagemAsync(admin, produto.Id);

        var imagem = atualizado.Imagens.Should().ContainSingle().Subject;
        imagem.Ordem.Should().Be(0);
        fabrica.Armazenamento.Arquivos.Should().ContainKey(imagem.Url);
    }

    [Fact]
    public async Task Enviar_ComTipoNaoSuportado_DeveRetornarBadRequest()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();

        var resposta = await admin.PostAsync($"/api/v1/produtos/{produto.Id}/imagens", CriarArquivo("image/gif"));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Remover_DeveApagarDoStorageEPromoverAProxima()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        await EnviarImagemAsync(admin, produto.Id);
        var comDuas = await EnviarImagemAsync(admin, produto.Id);
        var principal = comDuas.Imagens.Single(i => i.Ordem == 0);
        var segunda = comDuas.Imagens.Single(i => i.Ordem == 1);

        var resposta = await admin.DeleteAsync($"/api/v1/produtos/{produto.Id}/imagens/{principal.Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await resposta.Content.ReadFromJsonAsync<ProdutoDto>();
        atualizado!.Imagens.Should().ContainSingle().Which.Should().BeEquivalentTo(segunda with { Ordem = 0 });
        fabrica.Armazenamento.Arquivos.Should().NotContainKey(principal.Url);
    }

    [Fact]
    public async Task Reordenar_DeveTrocarAPrincipalUsadaNaListagem()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        await EnviarImagemAsync(admin, produto.Id);
        var comDuas = await EnviarImagemAsync(admin, produto.Id);
        var novaOrdem = comDuas.Imagens.OrderByDescending(i => i.Ordem).Select(i => i.Id).ToList();

        var resposta = await admin.PutAsJsonAsync($"/api/v1/produtos/{produto.Id}/imagens/ordem", new { imagemIds = novaOrdem });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await resposta.Content.ReadFromJsonAsync<ProdutoDto>();
        atualizado!.Imagens.Select(i => i.Id).Should().Equal(novaOrdem);

        // A busca projeta só a primeira imagem (subconsulta por Ordem) — tem que seguir a nova ordem.
        var novaPrincipal = comDuas.Imagens.Single(i => i.Id == novaOrdem[0]).Url;
        var busca = await admin.GetFromJsonAsync<Ecommerce.Application.Comum.ResultadoPaginado<ProdutoResumoDto>>(
            $"/api/v1/produtos?termo={Uri.EscapeDataString(produto.Nome)}");
        busca!.Itens.Should().ContainSingle().Which.ImagemPrincipalUrl.Should().Be(novaPrincipal);
    }

    [Fact]
    public async Task Reordenar_ComListaIncompleta_DeveRetornarBadRequest()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        await EnviarImagemAsync(admin, produto.Id);
        var comDuas = await EnviarImagemAsync(admin, produto.Id);

        var resposta = await admin.PutAsJsonAsync(
            $"/api/v1/produtos/{produto.Id}/imagens/ordem", new { imagemIds = new[] { comDuas.Imagens.First().Id } });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Remover_ComUsuarioComum_DeveRetornarForbidden()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        var comImagem = await EnviarImagemAsync(admin, produto.Id);
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();

        var resposta = await cliente.DeleteAsync($"/api/v1/produtos/{produto.Id}/imagens/{comImagem.Imagens.Single().Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
