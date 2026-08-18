using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Contas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Contas;

public class EnderecoTests
{
    private static Endereco CriarEnderecoValido() => Endereco.Criar(
        Guid.NewGuid(), "Casa", "Rua das Flores", "123", "Apto 45", "Centro", "São Paulo", "sp", "01310-100");

    [Fact]
    public void Criar_ComDadosValidos_DeveNormalizarEstadoECep()
    {
        var endereco = CriarEnderecoValido();

        endereco.Estado.Should().Be("SP");
        endereco.Cep.Should().Be("01310-100");
        endereco.Padrao.Should().BeFalse();
    }

    [Fact]
    public void Criar_ComCepSemFormatacao_DeveNormalizar()
    {
        var endereco = Endereco.Criar(Guid.NewGuid(), "Casa", "Rua X", "1", null, "Bairro", "Cidade", "RJ", "20040020");

        endereco.Cep.Should().Be("20040-020");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345-6789")]
    [InlineData("")]
    public void Criar_ComCepInvalido_DeveLancarExcecaoDeDominio(string cepInvalido)
    {
        var act = () => Endereco.Criar(Guid.NewGuid(), "Casa", "Rua X", "1", null, "Bairro", "Cidade", "RJ", cepInvalido);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("S")]
    [InlineData("SPP")]
    [InlineData("")]
    public void Criar_ComEstadoInvalido_DeveLancarExcecaoDeDominio(string estadoInvalido)
    {
        var act = () => Endereco.Criar(Guid.NewGuid(), "Casa", "Rua X", "1", null, "Bairro", "Cidade", estadoInvalido, "01310-100");

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComUsuarioVazio_DeveLancarExcecaoDeDominio()
    {
        var act = () => Endereco.Criar(Guid.Empty, "Casa", "Rua X", "1", null, "Bairro", "Cidade", "RJ", "01310-100");

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComLogradouroVazio_DeveLancarExcecaoDeDominio(string logradouroInvalido)
    {
        var act = () => Endereco.Criar(Guid.NewGuid(), "Casa", logradouroInvalido, "1", null, "Bairro", "Cidade", "RJ", "01310-100");

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_SemComplemento_DeveFicarNulo()
    {
        var endereco = Endereco.Criar(Guid.NewGuid(), "Casa", "Rua X", "1", "", "Bairro", "Cidade", "RJ", "01310-100");

        endereco.Complemento.Should().BeNull();
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveAtualizarTodosOsCampos()
    {
        var endereco = CriarEnderecoValido();

        endereco.Atualizar("Trabalho", "Av. Paulista", "1000", "Sala 10", "Bela Vista", "São Paulo", "sp", "01310-000");

        endereco.Rotulo.Should().Be("Trabalho");
        endereco.Logradouro.Should().Be("Av. Paulista");
        endereco.Complemento.Should().Be("Sala 10");
    }
}
