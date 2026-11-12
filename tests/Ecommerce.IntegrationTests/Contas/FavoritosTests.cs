using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.Contas;
using FluentAssertions;

namespace Ecommerce.IntegrationTests.Contas;

public class FavoritosTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    private const string Rota = "/api/v1/conta/favoritos";

    [Fact]
    public async Task Adicionar_DeveAparecerNaListaENosIds()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 59.90m);
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();

        var resposta = await cliente.PutAsync($"{Rota}/{produto.Id}", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lista = await cliente.GetFromJsonAsync<List<FavoritoDto>>(Rota);
        var favorito = lista.Should().ContainSingle().Subject;
        favorito.Produto.Id.Should().Be(produto.Id);
        favorito.Produto.Slug.Should().Be(produto.Slug);
        favorito.Produto.Preco.Should().Be(59.90m);
        favorito.Disponivel.Should().BeTrue();

        var ids = await cliente.GetFromJsonAsync<List<Guid>>($"{Rota}/ids");
        ids.Should().Equal(produto.Id);
    }

    [Fact]
    public async Task Adicionar_DuasVezes_DeveManterUmSoItem()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();

        (await cliente.PutAsync($"{Rota}/{produto.Id}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await cliente.PutAsync($"{Rota}/{produto.Id}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ids = await cliente.GetFromJsonAsync<List<Guid>>($"{Rota}/ids");
        ids.Should().ContainSingle();
    }

    [Fact]
    public async Task Remover_DeveTirarDaLista_ERepetirNaoEhErro()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.PutAsync($"{Rota}/{produto.Id}", null);

        (await cliente.DeleteAsync($"{Rota}/{produto.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await cliente.DeleteAsync($"{Rota}/{produto.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lista = await cliente.GetFromJsonAsync<List<FavoritoDto>>(Rota);
        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task Adicionar_ProdutoInexistente_DeveRetornarBadRequest()
    {
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();

        var resposta = await cliente.PutAsync($"{Rota}/{Guid.NewGuid()}", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Adicionar_ProdutoInativo_DeveRetornarBadRequest()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        (await admin.PostAsync($"/api/v1/produtos/{produto.Id}/inativar", null)).EnsureSuccessStatusCode();
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();

        var resposta = await cliente.PutAsync($"{Rota}/{produto.Id}", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProdutoInativadoDepoisDeFavoritado_DeveContinuarNaListaComoIndisponivel()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        var (cliente, _) = await fabrica.CriarClienteLogadoAsync();
        await cliente.PutAsync($"{Rota}/{produto.Id}", null);

        (await admin.PostAsync($"/api/v1/produtos/{produto.Id}/inativar", null)).EnsureSuccessStatusCode();

        var lista = await cliente.GetFromJsonAsync<List<FavoritoDto>>(Rota);
        var favorito = lista.Should().ContainSingle().Subject;
        favorito.Disponivel.Should().BeFalse();
        favorito.Produto.Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task Lista_DeveSerSoDoUsuarioLogado()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync();
        var (clienteA, _) = await fabrica.CriarClienteLogadoAsync();
        var (clienteB, _) = await fabrica.CriarClienteLogadoAsync();
        await clienteA.PutAsync($"{Rota}/{produto.Id}", null);

        // B remover o produto não mexe na lista de A.
        await clienteB.DeleteAsync($"{Rota}/{produto.Id}");

        (await clienteB.GetFromJsonAsync<List<Guid>>($"{Rota}/ids")).Should().BeEmpty();
        (await clienteA.GetFromJsonAsync<List<Guid>>($"{Rota}/ids")).Should().Equal(produto.Id);
    }

    [Fact]
    public async Task SemLogin_DeveRetornarUnauthorized()
    {
        using var cliente = fabrica.CriarCliente();

        var resposta = await cliente.GetAsync(Rota);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
