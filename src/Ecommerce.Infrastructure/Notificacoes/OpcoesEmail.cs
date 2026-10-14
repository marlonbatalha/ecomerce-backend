namespace Ecommerce.Infrastructure.Notificacoes;

/// <summary>Vinculada à seção "Email" da configuração.</summary>
public class OpcoesEmail
{
    public const string SecaoConfiguracao = "Email";

    public const string ProvedorLog = "Log";

    public const string ProvedorSmtp = "Smtp";

    /// <summary>
    /// <see cref="ProvedorLog"/> (padrão — só escreve no log, pra dev sem servidor SMTP) ou
    /// <see cref="ProvedorSmtp"/> (qualquer serviço SMTP: OCI Email Delivery, SES, Mailgun, ...).
    /// </summary>
    public string Provedor { get; set; } = ProvedorLog;

    public string Remetente { get; set; } = "nao-responda@loja.local";

    public string NomeRemetente { get; set; } = "Loja";

    public OpcoesSmtp Smtp { get; set; } = new();
}

public class OpcoesSmtp
{
    public string Host { get; set; } = string.Empty;

    /// <summary>587 (STARTTLS) é o padrão da maioria dos provedores.</summary>
    public int Porta { get; set; } = 587;

    public string Usuario { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}
