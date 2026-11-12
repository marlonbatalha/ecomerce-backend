using Ecommerce.Domain.Comum;
using FluentAssertions;

namespace Ecommerce.UnitTests.Comum;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData(" 111.444.777-35 ", "11144477735")]
    public void Normalizar_CpfValido_DeveDevolverSoOsDigitos(string entrada, string esperado)
    {
        Cpf.Normalizar(entrada).Should().Be(esperado);
    }

    [Theory]
    [InlineData("529.982.247-24")] // dígito verificador errado
    [InlineData("111.111.111-11")] // sequência repetida passa na conta, mas não existe
    [InlineData("1234567890")] // 10 dígitos
    [InlineData("")]
    [InlineData(null)]
    public void Normalizar_CpfInvalido_DeveLancarExcecaoDeDominio(string? entrada)
    {
        var act = () => Cpf.Normalizar(entrada);

        act.Should().Throw<ExcecaoDeDominio>();
        Cpf.EhValido(entrada).Should().BeFalse();
    }
}
