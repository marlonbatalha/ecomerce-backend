using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Notificacoes;

/// <summary>
/// Consome a <see cref="FilaDeEmails"/> e envia, com até 3 tentativas (espera de 2s e 4s entre
/// elas). Falha definitiva é logada e o e-mail descartado — nunca derruba o host.
/// </summary>
public class ProcessadorDeFilaDeEmails(
    FilaDeEmails fila,
    IServiceScopeFactory fabricaDeEscopos,
    ILogger<ProcessadorDeFilaDeEmails> logger) : BackgroundService
{
    private const int MaximoDeTentativas = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var mensagem in fila.LerTodasAsync(stoppingToken))
            {
                await EnviarComRetentativasAsync(mensagem, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host desligando.
        }
    }

    private async Task EnviarComRetentativasAsync(MensagemDeEmail mensagem, CancellationToken ct)
    {
        for (var tentativa = 1; tentativa <= MaximoDeTentativas; tentativa++)
        {
            try
            {
                using var escopo = fabricaDeEscopos.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<IEnviadorDeEmail>().EnviarAsync(mensagem, ct);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (tentativa == MaximoDeTentativas)
                {
                    logger.LogError(ex, "Falha definitiva ao enviar e-mail {Assunto} para {Destinatario}", mensagem.Assunto, mensagem.Destinatario);
                    return;
                }

                logger.LogWarning(ex, "Falha ao enviar e-mail (tentativa {Tentativa}), tentando de novo", tentativa);
                await Task.Delay(TimeSpan.FromSeconds(2 * tentativa), ct);
            }
        }
    }
}
