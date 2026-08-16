using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Common;
using FluentAssertions;

namespace Ecommerce.UnitTests.Catalog;

public class CategoryTests
{
    [Fact]
    public void Create_ComNomeValido_DevePreencherNomeESlug()
    {
        var category = Category.Create("Eletrônicos & Informática");

        category.Name.Should().Be("Eletrônicos & Informática");
        category.Slug.Should().Be("eletronicos-informatica");
        category.ParentCategoryId.Should().BeNull();
        category.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComNomeInvalido_DeveLancarDomainException(string? nomeInvalido)
    {
        var act = () => Category.Create(nomeInvalido!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ComNomeMaiorQueLimite_DeveLancarDomainException()
    {
        var nomeMuitoLongo = new string('a', 101);

        var act = () => Category.Create(nomeMuitoLongo);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rename_DeveAtualizarNomeESlug()
    {
        var category = Category.Create("Roupas");

        category.Rename("Roupas e Acessórios");

        category.Name.Should().Be("Roupas e Acessórios");
        category.Slug.Should().Be("roupas-e-acessorios");
    }

    [Fact]
    public void MoveTo_ParaOutraCategoria_DeveAtualizarParent()
    {
        var raiz = Category.Create("Casa");
        var filha = Category.Create("Cozinha");

        filha.MoveTo(raiz);

        filha.ParentCategory.Should().Be(raiz);
        filha.ParentCategoryId.Should().Be(raiz.Id);
    }

    [Fact]
    public void MoveTo_ParaSiMesma_DeveLancarDomainException()
    {
        var category = Category.Create("Livros");

        var act = () => category.MoveTo(category);

        act.Should().Throw<DomainException>()
            .WithMessage("*não pode ser pai de si mesma*");
    }

    [Fact]
    public void MoveTo_Null_DeveVoltarParaRaiz()
    {
        var raiz = Category.Create("Casa");
        var filha = Category.Create("Cozinha");
        filha.MoveTo(raiz);

        filha.MoveTo(null);

        filha.ParentCategory.Should().BeNull();
        filha.ParentCategoryId.Should().BeNull();
    }
}
