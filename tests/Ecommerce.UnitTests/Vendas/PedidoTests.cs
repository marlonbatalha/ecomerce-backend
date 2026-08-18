using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Vendas;

public class PedidoTests
{
    private static Pedido CriarPedidoValido() => Pedido.Criar(
        Guid.NewGuid(),
        [new ItemPedidoRascunho(Guid.NewGuid(), "Camiseta P", 79.90m, 2)]);

    [Fact]
    public void Criar_ComItens_DevePreencherTotalEStatusPendente()
    {
        var pedido = CriarPedidoValido();

        pedido.Status.Should().Be(StatusPedido.Pendente);
        pedido.Total.Should().Be(159.80m);
        pedido.Itens.Should().ContainSingle();
    }

    [Fact]
    public void Criar_SemItens_DeveLancarExcecaoDeDominio()
    {
        var act = () => Pedido.Criar(Guid.NewGuid(), []);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Criar_ComUsuarioVazio_DeveLancarExcecaoDeDominio()
    {
        var act = () => Pedido.Criar(Guid.Empty, [new ItemPedidoRascunho(Guid.NewGuid(), "Produto", 10m, 1)]);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Criar_ComQuantidadeInvalida_DeveLancarExcecaoDeDominio(int quantidadeInvalida)
    {
        var act = () => Pedido.Criar(Guid.NewGuid(), [new ItemPedidoRascunho(Guid.NewGuid(), "Produto", 10m, quantidadeInvalida)]);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void FluxoFeliz_PendenteAtePendenteConfirmadoEnviadoEntregue_DeveFuncionar()
    {
        var pedido = CriarPedidoValido();

        pedido.ConfirmarPagamento();
        pedido.Status.Should().Be(StatusPedido.PagamentoConfirmado);

        pedido.MarcarComoEnviado("BR123456789");
        pedido.Status.Should().Be(StatusPedido.Enviado);
        pedido.CodigoRastreio.Should().Be("BR123456789");

        pedido.MarcarComoEntregue();
        pedido.Status.Should().Be(StatusPedido.Entregue);
    }

    [Fact]
    public void MarcarComoEnviado_SemPagamentoConfirmado_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();

        var act = () => pedido.MarcarComoEnviado("BR123456789");

        act.Should().Throw<ExcecaoDeDominio>().WithMessage("*Pendente*Enviado*");
    }

    [Fact]
    public void MarcarComoEnviado_SemCodigoRastreio_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.ConfirmarPagamento();

        var act = () => pedido.MarcarComoEnviado("");

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void MarcarComoEntregue_SemEnvio_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.ConfirmarPagamento();

        var act = () => pedido.MarcarComoEntregue();

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Cancelar_DepoisDeEntregue_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.ConfirmarPagamento();
        pedido.MarcarComoEnviado("BR123456789");
        pedido.MarcarComoEntregue();

        var act = () => pedido.Cancelar();

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Reembolsar_DepoisDeEntregue_DeveFuncionar()
    {
        var pedido = CriarPedidoValido();
        pedido.ConfirmarPagamento();
        pedido.MarcarComoEnviado("BR123456789");
        pedido.MarcarComoEntregue();

        pedido.Reembolsar();

        pedido.Status.Should().Be(StatusPedido.Reembolsado);
    }

    [Fact]
    public void RecusarPagamento_DeTransicaoValida_DeveFuncionar()
    {
        var pedido = CriarPedidoValido();

        pedido.RecusarPagamento();

        pedido.Status.Should().Be(StatusPedido.PagamentoRecusado);
    }

    [Fact]
    public void ConfirmarPagamento_DepoisDeRecusado_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.RecusarPagamento();

        var act = () => pedido.ConfirmarPagamento();

        act.Should().Throw<ExcecaoDeDominio>();
    }
}
