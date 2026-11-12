using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Vendas;

public class PagamentoTests
{
    private static Pagamento CriarPagamentoValido() => Pagamento.Criar(Guid.NewGuid(), MetodoDePagamento.Pix, "ref-externa-123", 100m);

    [Fact]
    public void Criar_ComDadosValidos_DeveFicarPendente()
    {
        var pagamento = CriarPagamentoValido();

        pagamento.Status.Should().Be(StatusPagamento.Pendente);
    }

    [Fact]
    public void Criar_ComValorNaoPositivo_DeveLancarExcecaoDeDominio()
    {
        var act = () => Pagamento.Criar(Guid.NewGuid(), MetodoDePagamento.Pix, "ref", 0m);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComInstrucoes_DeveGuardarMetodoEInstrucoes()
    {
        var expira = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);

        var pagamento = Pagamento.Criar(
            Guid.NewGuid(), MetodoDePagamento.Boleto, "123", 50m,
            new InstrucoesDaCobranca(UrlPagamento: "https://mp/boleto", LinhaDigitavel: "0019 0000", ExpiraEmUtc: expira));

        pagamento.Metodo.Should().Be(MetodoDePagamento.Boleto);
        pagamento.UrlPagamento.Should().Be("https://mp/boleto");
        pagamento.LinhaDigitavel.Should().Be("0019 0000");
        pagamento.CodigoPix.Should().BeNull();
        pagamento.ExpiraEmUtc.Should().Be(expira);
    }

    [Fact]
    public void Criar_ComCodigoPixLongoDemais_DeveLancarExcecaoDeDominio()
    {
        var act = () => Pagamento.Criar(
            Guid.NewGuid(), MetodoDePagamento.Pix, "123", 50m,
            new InstrucoesDaCobranca(CodigoPix: new string('0', Pagamento.TamanhoMaximoCodigoPix + 1)));

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
