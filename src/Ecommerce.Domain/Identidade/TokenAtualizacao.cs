using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Identidade;

/// <summary>
/// Refresh token emitido para um usuário. Persistido pelo hash do valor (nunca o token puro),
/// com expiração e suporte a revogação — permite logout remoto e detecção de reuso (ver Fase 1
/// do plano do projeto: um token já revogado sendo apresentado de novo é sinal de token roubado).
/// </summary>
public class TokenAtualizacao : Entidade
{
    public Guid UsuarioId { get; private set; }

    public string HashToken { get; private set; } = null!;

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime ExpiraEmUtc { get; private set; }

    public DateTime? RevogadoEmUtc { get; private set; }

    /// <summary>
    /// Hash do token que substituiu este, quando revogado por renovação (encadeamento usado pra
    /// detecção de reuso). Null quando a revogação não veio de uma renovação (ex.: logout).
    /// </summary>
    public string? SubstituidoPorHashToken { get; private set; }

    public bool EstaRevogado => RevogadoEmUtc is not null;

    public bool EstaExpirado => DateTime.UtcNow >= ExpiraEmUtc;

    public bool EstaAtivo => !EstaRevogado && !EstaExpirado;

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private TokenAtualizacao()
    {
    }

    private TokenAtualizacao(Guid id, Guid usuarioId, string hashToken, DateTime criadoEmUtc, DateTime expiraEmUtc)
        : base(id)
    {
        UsuarioId = usuarioId;
        HashToken = hashToken;
        CriadoEmUtc = criadoEmUtc;
        ExpiraEmUtc = expiraEmUtc;
    }

    public static TokenAtualizacao Criar(Guid usuarioId, string hashToken, TimeSpan duracao)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O token de atualização precisa estar associado a um usuário.");
        }

        if (string.IsNullOrWhiteSpace(hashToken))
        {
            throw new ExcecaoDeDominio("O hash do token de atualização é obrigatório.");
        }

        if (duracao <= TimeSpan.Zero)
        {
            throw new ExcecaoDeDominio("A duração do token de atualização precisa ser positiva.");
        }

        var agora = DateTime.UtcNow;
        return new TokenAtualizacao(Guid.NewGuid(), usuarioId, hashToken, agora, agora.Add(duracao));
    }

    /// <summary>
    /// Revoga o token — opcionalmente registrando o hash do token que o substituiu (renovação),
    /// formando a cadeia usada pra detecção de reuso.
    /// </summary>
    public void Revogar(string? substitutoHashToken = null)
    {
        if (EstaRevogado)
        {
            throw new ExcecaoDeDominio("O token de atualização já foi revogado.");
        }

        RevogadoEmUtc = DateTime.UtcNow;
        SubstituidoPorHashToken = substitutoHashToken;
    }
}
