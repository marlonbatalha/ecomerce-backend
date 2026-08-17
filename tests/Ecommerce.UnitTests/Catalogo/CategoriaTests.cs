using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using FluentAssertions;

namespace Ecommerce.UnitTests.Catalogo;

public class CategoriaTests
{
    [Fact]
    public void Criar_ComNomeValido_DevePreencherNomeESlug()
    {
        var categoria = Categoria.Criar("Eletrônicos & Informática");

        categoria.Nome.Should().Be("Eletrônicos & Informática");
        categoria.Slug.Should().Be("eletronicos-informatica");
        categoria.CategoriaPaiId.Should().BeNull();
        categoria.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComNomeInvalido_DeveLancarExcecaoDeDominio(string? nomeInvalido)
    {
        var act = () => Categoria.Criar(nomeInvalido!);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComNomeMaiorQueLimite_DeveLancarExcecaoDeDominio()
    {
        var nomeMuitoLongo = new string('a', 101);

        var act = () => Categoria.Criar(nomeMuitoLongo);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Renomear_DeveAtualizarNomeESlug()
    {
        var categoria = Categoria.Criar("Roupas");

        categoria.Renomear("Roupas e Acessórios");

        categoria.Nome.Should().Be("Roupas e Acessórios");
        categoria.Slug.Should().Be("roupas-e-acessorios");
    }

    [Fact]
    public void MoverPara_ParaOutraCategoria_DeveAtualizarCategoriaPai()
    {
        var raiz = Categoria.Criar("Casa");
        var filha = Categoria.Criar("Cozinha");

        filha.MoverPara(raiz);

        filha.CategoriaPai.Should().Be(raiz);
        filha.CategoriaPaiId.Should().Be(raiz.Id);
    }

    [Fact]
    public void MoverPara_ParaSiMesma_DeveLancarExcecaoDeDominio()
    {
        var categoria = Categoria.Criar("Livros");

        var act = () => categoria.MoverPara(categoria);

        act.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("*não pode ser pai de si mesma*");
    }

    [Fact]
    public void MoverPara_Null_DeveVoltarParaRaiz()
    {
        var raiz = Categoria.Criar("Casa");
        var filha = Categoria.Criar("Cozinha");
        filha.MoverPara(raiz);

        filha.MoverPara(null);

        filha.CategoriaPai.Should().BeNull();
        filha.CategoriaPaiId.Should().BeNull();
    }
}
