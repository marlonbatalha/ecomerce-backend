using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Vendas;

public class CupomTests
{
    private static readonly DateTime Agora = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_DeveNormalizarCodigoEmMaiusculas()
    {
        var cupom = Cupom.Criar("  bemvindo10 ", "Boas-vindas", TipoDeDesconto.Percentual, 10m);

        cupom.Codigo.Should().Be("BEMVINDO10");
        cupom.Ativo.Should().BeTrue();
        cupom.QuantidadeDeUsos.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("com espaco")]
    [InlineData("acentuação")]
    public void Criar_ComCodigoInvalido_DeveLancarExcecaoDeDominio(string codigo)
    {
        var act = () => Cupom.Criar(codigo, "", TipoDeDesconto.ValorFixo, 10m);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Criar_PercentualForaDaFaixa_DeveLancarExcecaoDeDominio(decimal valor)
    {
        var act = () => Cupom.Criar("X", "", TipoDeDesconto.Percentual, valor);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void CalcularDesconto_Percentual_DeveArredondarACentavos()
    {
        var cupom = Cupom.Criar("DEZ", "", TipoDeDesconto.Percentual, 10m);

        cupom.CalcularDesconto(159.85m, Agora).Should().Be(15.99m);
    }

    [Fact]
    public void CalcularDesconto_ValorFixoMaiorQueSubtotal_DeveLimitarAoSubtotal()
    {
        var cupom = Cupom.Criar("CINQUENTA", "", TipoDeDesconto.ValorFixo, 50m);

        cupom.CalcularDesconto(30m, Agora).Should().Be(30m);
    }

    [Fact]
    public void CalcularDesconto_AbaixoDoMinimo_DeveLancarExcecaoDeDominio()
    {
        var cupom = Cupom.Criar("MIN100", "", TipoDeDesconto.ValorFixo, 20m, valorMinimoDoPedido: 100m);

        var act = () => cupom.CalcularDesconto(99.99m, Agora);

        act.Should().Throw<ExcecaoDeDominio>().WithMessage("*mínimo*");
    }

    [Fact]
    public void CalcularDesconto_Expirado_DeveLancarExcecaoDeDominio()
    {
        var cupom = Cupom.Criar("VENCIDO", "", TipoDeDesconto.ValorFixo, 20m, validoAteUtc: Agora.AddDays(-1));

        var act = () => cupom.CalcularDesconto(100m, Agora);

        act.Should().Throw<ExcecaoDeDominio>().WithMessage("*expirou*");
    }

    [Fact]
    public void CalcularDesconto_ComLimiteAtingido_DeveLancarExcecaoDeDominio()
    {
        var cupom = Cupom.Criar("UNICO", "", TipoDeDesconto.ValorFixo, 20m, limiteDeUsos: 1);
        cupom.RegistrarUso();

        var act = () => cupom.CalcularDesconto(100m, Agora);

        act.Should().Throw<ExcecaoDeDominio>().WithMessage("*limite*");
    }

    [Fact]
    public void CalcularDesconto_Inativo_DeveLancarExcecaoDeDominio()
    {
        var cupom = Cupom.Criar("PAUSADO", "", TipoDeDesconto.ValorFixo, 20m);
        cupom.Inativar();

        var act = () => cupom.CalcularDesconto(100m, Agora);

        act.Should().Throw<ExcecaoDeDominio>();
    }
}
