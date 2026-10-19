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

    private static EnderecoDeEntrega EnderecoValido() =>
        EnderecoDeEntrega.Criar(" Av. Paulista ", "1000", "  ", "Bela Vista", "São Paulo", "sp", "01310-100");

    [Fact]
    public void DefinirEntrega_DeveSomarOFreteAoTotalMasNaoAoSubtotal()
    {
        var pedido = CriarPedidoValido();

        pedido.DefinirEntrega(EnderecoValido(), "PAC", 19.90m, 8);

        pedido.Subtotal.Should().Be(159.80m);
        pedido.Total.Should().Be(179.70m);
        pedido.MetodoDeEnvio.Should().Be("PAC");
        pedido.PrazoEntregaDias.Should().Be(8);
    }

    [Fact]
    public void DefinirEntrega_ComDesconto_DescontaSoOsProdutos()
    {
        var pedido = CriarPedidoValido();
        pedido.AplicarDesconto("TUDO", 159.80m);

        pedido.DefinirEntrega(EnderecoValido(), "PAC", 19.90m, 8);

        pedido.Total.Should().Be(19.90m);
    }

    [Fact]
    public void DefinirEntrega_DuasVezes_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.DefinirEntrega(EnderecoValido(), "PAC", 19.90m, 8);

        var act = () => pedido.DefinirEntrega(EnderecoValido(), "SEDEX", 34.90m, 3);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void DefinirEntrega_ComFreteNegativo_DeveLancarExcecaoDeDominio()
    {
        var act = () => CriarPedidoValido().DefinirEntrega(EnderecoValido(), "PAC", -1m, 8);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void DefinirEntrega_ComPedidoJaPago_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.ConfirmarPagamento();

        var act = () => pedido.DefinirEntrega(EnderecoValido(), "PAC", 19.90m, 8);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void EnderecoDeEntrega_DeveNormalizarEspacosEUf()
    {
        var endereco = EnderecoValido();

        endereco.Logradouro.Should().Be("Av. Paulista");
        endereco.Complemento.Should().BeNull();
        endereco.Estado.Should().Be("SP");
    }

    [Fact]
    public void EnderecoDeEntrega_Incompleto_DeveLancarExcecaoDeDominio()
    {
        var act = () => EnderecoDeEntrega.Criar("Rua", "", null, "Bairro", "Cidade", "SP", "01310-100");

        act.Should().Throw<ExcecaoDeDominio>();
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
    public void AplicarDesconto_DeveReduzirTotalMantendoSubtotal()
    {
        var pedido = CriarPedidoValido();

        pedido.AplicarDesconto("DEZ", 15.98m);

        pedido.Subtotal.Should().Be(159.80m);
        pedido.Desconto.Should().Be(15.98m);
        pedido.Total.Should().Be(143.82m);
        pedido.CodigoCupom.Should().Be("DEZ");
    }

    [Fact]
    public void AplicarDesconto_DuasVezes_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();
        pedido.AplicarDesconto("DEZ", 10m);

        var act = () => pedido.AplicarDesconto("OUTRO", 10m);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void AplicarDesconto_MaiorQueSubtotal_DeveLancarExcecaoDeDominio()
    {
        var pedido = CriarPedidoValido();

        var act = () => pedido.AplicarDesconto("DEMAIS", 200m);

        act.Should().Throw<ExcecaoDeDominio>();
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
