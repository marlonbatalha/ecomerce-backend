using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ecommerce.Infrastructure.Notificacoes;

/// <summary>Transporte do e-mail — SMTP de verdade ou log (dev), conforme <c>Email:Provedor</c>.</summary>
public interface IEnviadorDeEmail
{
    Task EnviarAsync(MensagemDeEmail mensagem, CancellationToken ct = default);
}

/// <summary>Dev: não envia nada, só registra no log o que teria sido enviado.</summary>
public class EnviadorDeEmailLog(ILogger<EnviadorDeEmailLog> logger) : IEnviadorDeEmail
{
    public Task EnviarAsync(MensagemDeEmail mensagem, CancellationToken ct = default)
    {
        logger.LogInformation("E-mail (provedor Log, não enviado) para {Destinatario}: {Assunto}", mensagem.Destinatario, mensagem.Assunto);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Envio via SMTP com MailKit (o <c>System.Net.Mail.SmtpClient</c> é obsoleto pra uso novo). Uma
/// conexão por e-mail — volume transacional é baixo; se crescer, reaproveitar a conexão no
/// <see cref="ProcessadorDeFilaDeEmails"/>.
/// </summary>
public class EnviadorDeEmailSmtp(IOptions<OpcoesEmail> opcoes) : IEnviadorDeEmail
{
    public async Task EnviarAsync(MensagemDeEmail mensagem, CancellationToken ct = default)
    {
        var configuracao = opcoes.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(configuracao.NomeRemetente, configuracao.Remetente));
        mime.To.Add(new MailboxAddress(mensagem.NomeDestinatario, mensagem.Destinatario));
        mime.Subject = mensagem.Assunto;
        mime.Body = new BodyBuilder { HtmlBody = mensagem.CorpoHtml }.ToMessageBody();

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(configuracao.Smtp.Host, configuracao.Smtp.Porta, SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrEmpty(configuracao.Smtp.Usuario))
        {
            await cliente.AuthenticateAsync(configuracao.Smtp.Usuario, configuracao.Smtp.Senha, ct);
        }

        await cliente.SendAsync(mime, ct);
        await cliente.DisconnectAsync(true, ct);
    }
}
