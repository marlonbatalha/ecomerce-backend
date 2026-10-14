using System.Threading.Channels;

namespace Ecommerce.Infrastructure.Notificacoes;

public record MensagemDeEmail(string Destinatario, string NomeDestinatario, string Assunto, string CorpoHtml);

/// <summary>
/// Fila em memória (singleton) entre quem dispara o e-mail (requisição HTTP) e o
/// <see cref="ProcessadorDeFilaDeEmails"/>, que envia em segundo plano. Em memória = e-mails ainda
/// na fila se perdem num restart — aceitável pra e-mail transacional nesta fase; se virar
/// requisito, o próximo passo é uma tabela de outbox no Oracle.
/// </summary>
public class FilaDeEmails
{
    private readonly Channel<MensagemDeEmail> _canal = Channel.CreateBounded<MensagemDeEmail>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Enfileirar(MensagemDeEmail mensagem) => _canal.Writer.TryWrite(mensagem);

    public IAsyncEnumerable<MensagemDeEmail> LerTodasAsync(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}
