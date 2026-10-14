using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using FluentAssertions;

namespace Ecommerce.UnitTests.Catalogo;

public class ProdutoTests
{
    private static Produto CriarProdutoValido() =>
        Produto.Criar("Camiseta Básica", "100% algodão", "camiseta-basica", 79.90m, Guid.NewGuid());

    [Fact]
    public void Criar_ComDadosValidos_DevePreencherCamposENormalizarSku()
    {
        var produto = CriarProdutoValido();

        produto.Nome.Should().Be("Camiseta Básica");
        produto.Sku.Should().Be("CAMISETA-BASICA");
        produto.Preco.Should().Be(79.90m);
        produto.Ativo.Should().BeTrue();
        produto.Variantes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComNomeInvalido_DeveLancarExcecaoDeDominio(string? nomeInvalido)
    {
        var act = () => Produto.Criar(nomeInvalido!, "desc", "sku-1", 10m, Guid.NewGuid());

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComSkuInvalido_DeveLancarExcecaoDeDominio(string skuInvalido)
    {
        var act = () => Produto.Criar("Produto", "desc", skuInvalido, 10m, Guid.NewGuid());

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComPrecoNegativo_DeveLancarExcecaoDeDominio()
    {
        var act = () => Produto.Criar("Produto", "desc", "sku-1", -0.01m, Guid.NewGuid());

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void AdicionarVariante_ComDadosValidos_DeveAdicionarAtiva()
    {
        var produto = CriarProdutoValido();

        var variante = produto.AdicionarVariante("P / Branca", "camiseta-basica-p-branca", 79.90m, 10);

        produto.Variantes.Should().ContainSingle();
        variante.Ativo.Should().BeTrue();
        variante.Estoque.Should().Be(10);
    }

    [Fact]
    public void AdicionarVariante_ComEstoqueNegativo_DeveLancarExcecaoDeDominio()
    {
        var produto = CriarProdutoValido();

        var act = () => produto.AdicionarVariante("P / Branca", "sku-variante", 79.90m, -1);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void InativarVariante_UltimaAtiva_DeveLancarExcecaoDeDominio()
    {
        var produto = CriarProdutoValido();
        var variante = produto.AdicionarVariante("Único", "sku-unico", 79.90m, 5);

        var act = () => produto.InativarVariante(variante.Id);

        act.Should().Throw<ExcecaoDeDominio>().WithMessage("*ao menos uma variante ativa*");
    }

    [Fact]
    public void InativarVariante_ComOutraVarianteAtiva_DeveFuncionar()
    {
        var produto = CriarProdutoValido();
        var variante1 = produto.AdicionarVariante("P", "sku-p", 79.90m, 5);
        produto.AdicionarVariante("M", "sku-m", 79.90m, 5);

        produto.InativarVariante(variante1.Id);

        produto.Variantes.Single(v => v.Id == variante1.Id).Ativo.Should().BeFalse();
    }

    [Fact]
    public void InativarVariante_Inexistente_DeveLancarExcecaoDeDominio()
    {
        var produto = CriarProdutoValido();

        var act = () => produto.InativarVariante(Guid.NewGuid());

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void AdicionarImagem_DeveIncrementarOrdemAutomaticamente()
    {
        var produto = CriarProdutoValido();

        var primeira = produto.AdicionarImagem("https://exemplo.com/1.jpg");
        var segunda = produto.AdicionarImagem("https://exemplo.com/2.jpg");

        primeira.Ordem.Should().Be(0);
        segunda.Ordem.Should().Be(1);
        produto.Imagens.Should().HaveCount(2);
    }

    [Fact]
    public void AtualizarDados_ComPrecoNegativo_DeveLancarExcecaoDeDominioSemAlterarEstado()
    {
        var produto = CriarProdutoValido();

        var act = () => produto.AtualizarDados("Novo nome", "Nova desc", -1m);

        act.Should().Throw<ExcecaoDeDominio>();
        produto.Nome.Should().Be("Camiseta Básica");
    }

    [Fact]
    public void RemoverImagem_Principal_DeveRecompactarOrdem()
    {
        var produto = CriarProdutoValido();
        var primeira = produto.AdicionarImagem("https://cdn/1.jpg");
        var segunda = produto.AdicionarImagem("https://cdn/2.jpg");
        var terceira = produto.AdicionarImagem("https://cdn/3.jpg");

        var removida = produto.RemoverImagem(primeira.Id);

        removida.Should().BeSameAs(primeira);
        produto.Imagens.Should().HaveCount(2);
        segunda.Ordem.Should().Be(0);
        terceira.Ordem.Should().Be(1);
    }

    [Fact]
    public void RemoverImagem_Inexistente_DeveLancarExcecaoDeDominio()
    {
        var produto = CriarProdutoValido();

        var act = () => produto.RemoverImagem(Guid.NewGuid());

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void ReordenarImagens_DeveAplicarNovaOrdem()
    {
        var produto = CriarProdutoValido();
        var primeira = produto.AdicionarImagem("https://cdn/1.jpg");
        var segunda = produto.AdicionarImagem("https://cdn/2.jpg");

        produto.ReordenarImagens([segunda.Id, primeira.Id]);

        segunda.Ordem.Should().Be(0);
        primeira.Ordem.Should().Be(1);
    }

    [Fact]
    public void ReordenarImagens_ComListaIncompletaOuRepetida_DeveLancarExcecaoDeDominio()
    {
        var produto = CriarProdutoValido();
        var primeira = produto.AdicionarImagem("https://cdn/1.jpg");
        produto.AdicionarImagem("https://cdn/2.jpg");

        var incompleta = () => produto.ReordenarImagens([primeira.Id]);
        var repetida = () => produto.ReordenarImagens([primeira.Id, primeira.Id]);

        incompleta.Should().Throw<ExcecaoDeDominio>();
        repetida.Should().Throw<ExcecaoDeDominio>();
    }
}
