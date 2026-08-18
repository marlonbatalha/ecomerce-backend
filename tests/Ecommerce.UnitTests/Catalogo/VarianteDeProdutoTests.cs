using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using FluentAssertions;

namespace Ecommerce.UnitTests.Catalogo;

public class VarianteDeProdutoTests
{
    // VarianteDeProduto.Criar é internal (só Produto pode criar — ver comentário na classe),
    // então os testes passam pelo agregado raiz pra obter uma instância, como qualquer
    // consumidor externo ao assembly Domain faria.
    private static VarianteDeProduto CriarVarianteValida() =>
        Produto.Criar("Produto", "desc", "sku-produto", 50m, Guid.NewGuid())
            .AdicionarVariante("P / Azul", "sku-variante", 45m, 3);

    [Fact]
    public void AjustarEstoque_ComValorValido_DeveAtualizar()
    {
        var variante = CriarVarianteValida();

        variante.AjustarEstoque(20);

        variante.Estoque.Should().Be(20);
    }

    [Fact]
    public void AjustarEstoque_ComValorNegativo_DeveLancarExcecaoDeDominio()
    {
        var variante = CriarVarianteValida();

        var act = () => variante.AjustarEstoque(-1);

        act.Should().Throw<ExcecaoDeDominio>();
        variante.Estoque.Should().Be(3);
    }

    [Fact]
    public void AtualizarPreco_ComValorValido_DeveAtualizar()
    {
        var variante = CriarVarianteValida();

        variante.AtualizarPreco(59.90m);

        variante.Preco.Should().Be(59.90m);
    }

    [Fact]
    public void AtualizarPreco_ComValorNegativo_DeveLancarExcecaoDeDominio()
    {
        var variante = CriarVarianteValida();

        var act = () => variante.AtualizarPreco(-0.01m);

        act.Should().Throw<ExcecaoDeDominio>();
    }
}
