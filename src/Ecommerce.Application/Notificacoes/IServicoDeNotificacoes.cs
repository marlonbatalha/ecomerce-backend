namespace Ecommerce.Application.Notificacoes;

/// <summary>
/// E-mails transacionais. Implementação em Infrastructure monta o conteúdo e só **enfileira** o
/// envio (processado em segundo plano) — nenhum método aqui bloqueia a requisição esperando SMTP,
/// e falha de e-mail nunca derruba o fluxo de negócio que a disparou (é logada, não lançada).
/// </summary>
public interface IServicoDeNotificacoes
{
    Task BoasVindasAsync(string email, string nome, CancellationToken ct = default);

    Task PedidoRecebidoAsync(Guid pedidoId, CancellationToken ct = default);

    Task PagamentoConfirmadoAsync(Guid pedidoId, CancellationToken ct = default);

    Task PedidoEnviadoAsync(Guid pedidoId, CancellationToken ct = default);

    Task PedidoCanceladoAsync(Guid pedidoId, CancellationToken ct = default);
}
