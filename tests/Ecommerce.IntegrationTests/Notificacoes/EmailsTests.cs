using System.Net.Http.Json;
using Ecommerce.Application.Vendas;
using FluentAssertions;

namespace Ecommerce.IntegrationTests.Notificacoes;

public class EmailsTests(FabricaDeAplicacaoTeste fabrica) : IClassFixture<FabricaDeAplicacaoTeste>
{
    [Fact]
    public async Task Registrar_DeveEnviarBoasVindas()
    {
        var (_, email) = await fabrica.CriarClienteLogadoAsync();

        var mensagem = await fabrica.Emails.AguardarAsync(m => m.Destinatario == email);

        mensagem.Assunto.Should().Contain("Bem-vindo");
        mensagem.CorpoHtml.Should().Contain("Cliente Teste");
    }

    [Fact]
    public async Task Checkout_EEnvio_DevemEnviarEmailsDoPedido()
    {
        using var admin = await fabrica.CriarClienteAdminAsync();
        var produto = await admin.CriarProdutoComVarianteAsync(preco: 42m);
        var (cliente, email) = await fabrica.CriarClienteLogadoAsync();
        await cliente.AdicionarAoCarrinhoAsync(produto.Variantes.Single().Id);

        var checkout = await (await cliente.FinalizarCompraAsync()).Content.ReadFromJsonAsync<CheckoutResultado>();

        var recebido = await fabrica.Emails.AguardarAsync(m => m.Destinatario == email && m.Assunto.StartsWith("Recebemos seu pedido"));
        recebido.CorpoHtml.Should().Contain("R$").And.Contain(checkout!.Pedido.Id.ToString());
    }

    [Fact]
    public async Task Nome_ComHtml_DeveSerEscapadoNoEmail()
    {
        var email = $"xss-{Guid.NewGuid():N}@exemplo.com";
        using var cliente = fabrica.CriarCliente();
        await cliente.PostAsJsonAsync("/api/v1/auth/registrar", new { nome = "<script>x</script>", email, senha = AuxiliaresDeTeste.Senha });

        var mensagem = await fabrica.Emails.AguardarAsync(m => m.Destinatario == email);

        mensagem.CorpoHtml.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
    }
}
