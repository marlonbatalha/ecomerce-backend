using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Vendas;

public class PagamentoTests
{
    private static Pagamento CriarPagamentoValido() => Pagamento.Criar(Guid.NewGuid(), "ref-externa-123", 100m);

    [Fact]
    public void Criar_ComDadosValidos_DeveFicarPendente()
    {
        var pagamento = CriarPagamentoValido();

        pagamento.Status.Should().Be(StatusPagamento.Pendente);
    }

    [Fact]
    public void Criar_ComValorNaoPositivo_DeveLancarExcecaoDeDominio()
    {
        var act = () => Pagamento.Criar(Guid.NewGuid(), "ref", 0m);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Aprovar_Pendente_DeveFicarAprovado()
    {
        var pagamento = CriarPagamentoValido();

        pagamento.Aprovar();

        pagamento.Status.Should().Be(StatusPagamento.Aprovado);
    }

    [Fact]
    public void Aprovar_JaAprovado_DeveSerIdempotente()
    {
        var pagamento = CriarPagamentoValido();
        pagamento.Aprovar();

        var act = () => pagamento.Aprovar();

        act.Should().NotThrow();
        pagamento.Status.Should().Be(StatusPagamento.Aprovado);
    }

    [Fact]
    public void Aprovar_JaRecusado_DeveLancarExcecaoDeDominio()
    {
        var pagamento = CriarPagamentoValido();
        pagamento.Recusar();

        var act = () => pagamento.Aprovar();

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusar_JaRecusado_DeveSerIdempotente()
    {
        var pagamento = CriarPagamentoValido();
        pagamento.Recusar();

        var act = () => pagamento.Recusar();

        act.Should().NotThrow();
    }

    [Fact]
    public void Estornar_Aprovado_DeveFicarEstornado()
    {
        var pagamento = CriarPagamentoValido();
        pagamento.Aprovar();

        pagamento.Estornar();

        pagamento.Status.Should().Be(StatusPagamento.Estornado);
    }

    [Fact]
    public void Estornar_SemAprovar_DeveLancarExcecaoDeDominio()
    {
        var pagamento = CriarPagamentoValido();

        var act = () => pagamento.Estornar();

        act.Should().Throw<ExcecaoDeDominio>();
    }
}
