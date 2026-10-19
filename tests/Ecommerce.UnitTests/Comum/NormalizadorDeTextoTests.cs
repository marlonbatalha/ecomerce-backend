using Ecommerce.Domain.Comum;
using FluentAssertions;

namespace Ecommerce.UnitTests.Comum;

public class NormalizadorDeTextoTests
{
    [Theory]
    [InlineData("Camiseta Básica", "CAMISETA BASICA")]
    [InlineData("  calça   jeans\tslim ", "CALCA JEANS SLIM")]
    [InlineData("Ação ÇÃO ü", "ACAO CAO U")]
    public void ParaBusca_DeveRemoverAcentosColapsarEspacosEDeixarMaiusculo(string entrada, string esperado) =>
        NormalizadorDeTexto.ParaBusca(entrada).Should().Be(esperado);

    [Fact]
    public void RemoverAcentos_DevePreservarCaixa() =>
        NormalizadorDeTexto.RemoverAcentos("Pólo Açaí").Should().Be("Polo Acai");
}
