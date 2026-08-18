using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using FluentAssertions;

namespace Ecommerce.UnitTests.Vendas;

public class CarrinhoTests
{
    [Fact]
    public void AdicionarItem_PrimeiraVez_DeveCriarNovoItemERetornarInstancia()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());
        var varianteId = Guid.NewGuid();

        var novoItem = carrinho.AdicionarItem(varianteId, 2, estoqueDisponivel: 10);

        novoItem.Should().NotBeNull();
        carrinho.Itens.Should().ContainSingle(i => i.VarianteDeProdutoId == varianteId && i.Quantidade == 2);
    }

    [Fact]
    public void AdicionarItem_VarianteJaNoCarrinho_DeveSomarQuantidadeERetornarNull()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());
        var varianteId = Guid.NewGuid();
        carrinho.AdicionarItem(varianteId, 2, estoqueDisponivel: 10);

        var resultado = carrinho.AdicionarItem(varianteId, 3, estoqueDisponivel: 10);

        resultado.Should().BeNull();
        carrinho.Itens.Should().ContainSingle(i => i.Quantidade == 5);
    }

    [Fact]
    public void AdicionarItem_AcimaDoEstoque_DeveLancarExcecaoDeDominio()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());

        var act = () => carrinho.AdicionarItem(Guid.NewGuid(), 11, estoqueDisponivel: 10);

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void AdicionarItem_SomaAcimaDoEstoque_DeveLancarExcecaoDeDominioSemAlterarQuantidade()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());
        var varianteId = Guid.NewGuid();
        carrinho.AdicionarItem(varianteId, 8, estoqueDisponivel: 10);

        var act = () => carrinho.AdicionarItem(varianteId, 5, estoqueDisponivel: 10);

        act.Should().Throw<ExcecaoDeDominio>();
        carrinho.Itens.Single().Quantidade.Should().Be(8);
    }

    [Fact]
    public void RemoverItem_Existente_DeveRemoverERetornarItem()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());
        var varianteId = Guid.NewGuid();
        carrinho.AdicionarItem(varianteId, 1, estoqueDisponivel: 10);

        var itemRemovido = carrinho.RemoverItem(varianteId);

        itemRemovido.VarianteDeProdutoId.Should().Be(varianteId);
        carrinho.Itens.Should().BeEmpty();
    }

    [Fact]
    public void RemoverItem_Inexistente_DeveLancarExcecaoDeDominio()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());

        var act = () => carrinho.RemoverItem(Guid.NewGuid());

        act.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Esvaziar_DeveRemoverTodosOsItensERetornarOsRemovidos()
    {
        var carrinho = Carrinho.CriarParaUsuario(Guid.NewGuid());
        carrinho.AdicionarItem(Guid.NewGuid(), 1, estoqueDisponivel: 10);
        carrinho.AdicionarItem(Guid.NewGuid(), 2, estoqueDisponivel: 10);

        var removidos = carrinho.Esvaziar();

        removidos.Should().HaveCount(2);
        carrinho.Itens.Should().BeEmpty();
    }

    [Fact]
    public void MesclarCom_DeveSomarItensComunsEAdicionarNovos()
    {
        var varianteComum = Guid.NewGuid();
        var varianteSoNoAnonimo = Guid.NewGuid();

        var carrinhoUsuario = Carrinho.CriarParaUsuario(Guid.NewGuid());
        carrinhoUsuario.AdicionarItem(varianteComum, 1, estoqueDisponivel: 10);

        var carrinhoAnonimo = Carrinho.CriarAnonimo("sessao-1");
        carrinhoAnonimo.AdicionarItem(varianteComum, 2, estoqueDisponivel: 10);
        carrinhoAnonimo.AdicionarItem(varianteSoNoAnonimo, 1, estoqueDisponivel: 10);

        var estoques = new Dictionary<Guid, int> { [varianteComum] = 10, [varianteSoNoAnonimo] = 10 };
        var itensNovos = carrinhoUsuario.MesclarCom(carrinhoAnonimo, estoques);

        carrinhoUsuario.Itens.Should().ContainSingle(i => i.VarianteDeProdutoId == varianteComum && i.Quantidade == 3);
        carrinhoUsuario.Itens.Should().ContainSingle(i => i.VarianteDeProdutoId == varianteSoNoAnonimo && i.Quantidade == 1);
        itensNovos.Should().ContainSingle(i => i.VarianteDeProdutoId == varianteSoNoAnonimo);
    }

    [Fact]
    public void CriarAnonimo_ComIdSessaoVazio_DeveLancarExcecaoDeDominio()
    {
        var act = () => Carrinho.CriarAnonimo("");

        act.Should().Throw<ExcecaoDeDominio>();
    }
}
