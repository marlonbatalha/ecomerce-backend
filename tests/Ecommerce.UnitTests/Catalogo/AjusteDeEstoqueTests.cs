using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Estoque;
using FluentAssertions;

namespace Ecommerce.UnitTests.Catalogo;

public class AjusteDeEstoqueTests
{
    [Fact]
    public void Criar_ComDadosValidos_DevePreencherTodosOsCampos()
    {
        var varianteId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var ajuste = AjusteDeEstoque.Criar(varianteId, usuarioId, 5, 20, "Reposição de fornecedor");

        ajuste.VarianteDeProdutoId.Should().Be(varianteId);
        ajuste.UsuarioId.Should().Be(usuarioId);
        ajuste.EstoqueAnterior.Should().Be(5);
        ajuste.EstoqueNovo.Should().Be(20);
        ajuste.Motivo.Should().Be("Reposição de fornecedor");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComMotivoInvalido_DeveLancarExcecaoDeDominio(string? motivoInvalido)
    {
        var act = () => AjusteDeEstoque.Criar(Guid.NewGuid(), Guid.NewGuid(), 5, 10, motivoInvalido!);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComEstoqueNegativo_DeveLancarExcecaoDeDominio()
    {
        var act = () => AjusteDeEstoque.Criar(Guid.NewGuid(), Guid.NewGuid(), -1, 10, "Motivo");

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComVarianteOuUsuarioVazio_DeveLancarExcecaoDeDominio()
    {
        var act1 = () => AjusteDeEstoque.Criar(Guid.Empty, Guid.NewGuid(), 5, 10, "Motivo");
        var act2 = () => AjusteDeEstoque.Criar(Guid.NewGuid(), Guid.Empty, 5, 10, "Motivo");

        act1.Should().Throw<ExcecaoDeDominio>();
        act2.Should().Throw<ExcecaoDeDominio>();
    }
}
