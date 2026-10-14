using System.Globalization;
using System.Net;
using System.Text;
using Ecommerce.Application.Notificacoes;
using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Persistencia;
using Ecommerce.Infrastructure.Vendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Notificacoes;

/// <summary>
/// Monta os e-mails transacionais e enfileira na <see cref="FilaDeEmails"/>. Toda falha aqui
/// (pedido sumido, usuário sem e-mail, ...) é logada e engolida — e-mail é efeito colateral, não
/// pode desfazer um checkout/pagamento que já foi gravado.
/// </summary>
public class ServicoDeNotificacoesPorEmail(
    ContextoBancoDados contexto,
    FilaDeEmails fila,
    IOptions<OpcoesAplicacao> opcoesAplicacao,
    IOptions<OpcoesEmail> opcoesEmail,
    ILogger<ServicoDeNotificacoesPorEmail> logger) : IServicoDeNotificacoes
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public Task BoasVindasAsync(string email, string nome, CancellationToken ct = default)
    {
        var corpo = $"""
            <p>Olá, {Html(nome)}!</p>
            <p>Sua conta foi criada com sucesso. Que bom ter você por aqui.</p>
            {Botao("Começar a comprar", UrlFrontend("/produtos"))}
            """;

        Enfileirar(email, nome, "Bem-vindo(a)!", "Conta criada", corpo);
        return Task.CompletedTask;
    }

    public Task PedidoRecebidoAsync(Guid pedidoId, CancellationToken ct = default) =>
        NotificarPedidoAsync(pedidoId, ct, (pedido, nome) => (
            $"Recebemos seu pedido #{ServicoDePedidos.FormatarNumero(pedido.Id)}",
            "Pedido recebido",
            $"""
            <p>Olá, {Html(nome)}! Recebemos seu pedido e ele já está reservado pra você.</p>
            {TabelaDeItens(pedido)}
            <p>Assim que o pagamento for confirmado, avisamos por aqui.</p>
            {Botao("Acompanhar pedido", UrlFrontend($"/conta/pedidos/{pedido.Id}"))}
            """));

    public Task PagamentoConfirmadoAsync(Guid pedidoId, CancellationToken ct = default) =>
        NotificarPedidoAsync(pedidoId, ct, (pedido, nome) => (
            $"Pagamento confirmado — pedido #{ServicoDePedidos.FormatarNumero(pedido.Id)}",
            "Pagamento confirmado",
            $"""
            <p>Olá, {Html(nome)}! O pagamento de {Moeda(pedido.Total)} foi confirmado.</p>
            <p>Agora é com a gente: vamos separar e enviar seus produtos.</p>
            {Botao("Ver pedido", UrlFrontend($"/conta/pedidos/{pedido.Id}"))}
            """));

    public Task PedidoEnviadoAsync(Guid pedidoId, CancellationToken ct = default) =>
        NotificarPedidoAsync(pedidoId, ct, (pedido, nome) => (
            $"Seu pedido #{ServicoDePedidos.FormatarNumero(pedido.Id)} foi enviado",
            "Pedido a caminho",
            $"""
            <p>Olá, {Html(nome)}! Seu pedido saiu para entrega.</p>
            <p>Código de rastreio: <strong>{Html(pedido.CodigoRastreio ?? "-")}</strong></p>
            {Botao("Ver pedido", UrlFrontend($"/conta/pedidos/{pedido.Id}"))}
            """));

    public Task PedidoCanceladoAsync(Guid pedidoId, CancellationToken ct = default) =>
        NotificarPedidoAsync(pedidoId, ct, (pedido, nome) => (
            $"Pedido #{ServicoDePedidos.FormatarNumero(pedido.Id)} cancelado",
            "Pedido cancelado",
            $"""
            <p>Olá, {Html(nome)}. Seu pedido foi cancelado.</p>
            <p>Se você já tinha pago, o estorno é feito pelo mesmo meio de pagamento.</p>
            {Botao("Continuar comprando", UrlFrontend("/produtos"))}
            """));

    private async Task NotificarPedidoAsync(
        Guid pedidoId, CancellationToken ct, Func<Pedido, string, (string Assunto, string Titulo, string Corpo)> montar)
    {
        try
        {
            var pedido = await contexto.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
            if (pedido is null)
            {
                return;
            }

            var usuario = await contexto.Users.AsNoTracking()
                .Where(u => u.Id == pedido.UsuarioId)
                .Select(u => new { u.Email, u.Nome })
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(usuario?.Email))
            {
                return;
            }

            var (assunto, titulo, corpo) = montar(pedido, usuario.Nome);
            Enfileirar(usuario.Email, usuario.Nome, assunto, titulo, corpo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao preparar e-mail do pedido {PedidoId}", pedidoId);
        }
    }

    private void Enfileirar(string email, string nome, string assunto, string titulo, string corpo) =>
        fila.Enfileirar(new MensagemDeEmail(email, nome, assunto, Layout(titulo, corpo)));

    private string Layout(string titulo, string corpo)
    {
        var loja = Html(opcoesEmail.Value.NomeRemetente);
        return $"""
            <!doctype html>
            <html lang="pt-BR">
            <body style="margin:0;padding:24px;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#18181b">
              <table role="presentation" width="100%" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;padding:32px">
                <tr><td>
                  <p style="margin:0 0 4px;font-size:13px;color:#71717a">{loja}</p>
                  <h1 style="margin:0 0 20px;font-size:22px">{Html(titulo)}</h1>
                  <div style="font-size:15px;line-height:1.6">{corpo}</div>
                </td></tr>
              </table>
              <p style="text-align:center;font-size:12px;color:#a1a1aa;margin-top:16px">Este é um e-mail automático, não responda.</p>
            </body>
            </html>
            """;
    }

    private static string TabelaDeItens(Pedido pedido)
    {
        var linhas = new StringBuilder();
        foreach (var item in pedido.Itens)
        {
            linhas.Append($"""<tr><td style="padding:6px 0">{item.Quantidade}× {Html(item.NomeProduto)}</td><td style="padding:6px 0;text-align:right">{Moeda(item.Subtotal)}</td></tr>""");
        }

        if (pedido.Desconto > 0)
        {
            linhas.Append($"""<tr><td style="padding:6px 0;color:#16a34a">Cupom {Html(pedido.CodigoCupom ?? string.Empty)}</td><td style="padding:6px 0;text-align:right;color:#16a34a">-{Moeda(pedido.Desconto)}</td></tr>""");
        }

        linhas.Append($"""<tr><td style="padding:10px 0 0;border-top:1px solid #e4e4e7"><strong>Total</strong></td><td style="padding:10px 0 0;border-top:1px solid #e4e4e7;text-align:right"><strong>{Moeda(pedido.Total)}</strong></td></tr>""");

        return $"""<table role="presentation" width="100%" style="border-collapse:collapse;margin:16px 0">{linhas}</table>""";
    }

    private static string Botao(string texto, string url) =>
        $"""<p style="margin:24px 0 0"><a href="{Html(url)}" style="display:inline-block;background:#18181b;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:600">{Html(texto)}</a></p>""";

    private string UrlFrontend(string caminho) => $"{opcoesAplicacao.Value.UrlFrontend.TrimEnd('/')}{caminho}";

    private static string Moeda(decimal valor) => valor.ToString("C", CulturaBrasileira);

    private static string Html(string texto) => WebUtility.HtmlEncode(texto);
}
