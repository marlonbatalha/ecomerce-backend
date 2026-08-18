using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Identidade;
using FluentAssertions;

namespace Ecommerce.UnitTests.Identidade;

public class TokenAtualizacaoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveFicarAtivo()
    {
        var token = TokenAtualizacao.Criar(Guid.NewGuid(), "hash-qualquer", TimeSpan.FromDays(7));

        token.EstaAtivo.Should().BeTrue();
        token.EstaRevogado.Should().BeFalse();
        token.EstaExpirado.Should().BeFalse();
    }

    [Fact]
    public void Criar_ComUsuarioVazio_DeveLancarExcecaoDeDominio()
    {
        var act = () => TokenAtualizacao.Criar(Guid.Empty, "hash-qualquer", TimeSpan.FromDays(7));

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComHashInvalido_DeveLancarExcecaoDeDominio(string? hashInvalido)
    {
        var act = () => TokenAtualizacao.Criar(Guid.NewGuid(), hashInvalido!, TimeSpan.FromDays(7));

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComDuracaoNaoPositiva_DeveLancarExcecaoDeDominio()
    {
        var act = () => TokenAtualizacao.Criar(Guid.NewGuid(), "hash-qualquer", TimeSpan.Zero);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComDuracaoJaVencida_DeveFicarExpirado()
    {
        var token = TokenAtualizacao.Criar(Guid.NewGuid(), "hash-qualquer", TimeSpan.FromMilliseconds(1));

        Thread.Sleep(10);

        token.EstaExpirado.Should().BeTrue();
        token.EstaAtivo.Should().BeFalse();
    }

    [Fact]
    public void Revogar_DeveMarcarComoRevogadoEGuardarSubstituto()
    {
        var token = TokenAtualizacao.Criar(Guid.NewGuid(), "hash-original", TimeSpan.FromDays(7));

        token.Revogar("hash-novo");

        token.EstaRevogado.Should().BeTrue();
        token.EstaAtivo.Should().BeFalse();
        token.SubstituidoPorHashToken.Should().Be("hash-novo");
        token.RevogadoEmUtc.Should().NotBeNull();
    }

    [Fact]
    public void Revogar_JaRevogado_DeveLancarExcecaoDeDominio()
    {
        var token = TokenAtualizacao.Criar(Guid.NewGuid(), "hash-original", TimeSpan.FromDays(7));
        token.Revogar();

        var act = () => token.Revogar();

        act.Should().Throw<ExcecaoDeDominio>();
    }
}
