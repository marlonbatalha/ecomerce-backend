using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Contas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Contas;

public class FavoritoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveGuardarUsuarioProdutoEData()
    {
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var favorito = Favorito.Criar(usuarioId, produtoId);

        favorito.Id.Should().NotBeEmpty();
        favorito.UsuarioId.Should().Be(usuarioId);
        favorito.ProdutoId.Should().Be(produtoId);
        favorito.CriadoEmUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Criar_SemUsuario_DeveLancarExcecaoDeDominio()
    {
        var acao = () => Favorito.Criar(Guid.Empty, Guid.NewGuid());

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_SemProduto_DeveLancarExcecaoDeDominio()
    {
        var acao = () => Favorito.Criar(Guid.NewGuid(), Guid.Empty);

        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
