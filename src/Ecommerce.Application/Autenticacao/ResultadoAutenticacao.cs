namespace Ecommerce.Application.Autenticacao;

/// <summary>
/// Resultado de uma operação de autenticação (registro, login ou renovação). Em caso de sucesso,
/// carrega o access token e o refresh token puros — cabe à camada de apresentação (Api) decidir
/// como entregá-los (o refresh token, em cookie httpOnly; nunca no corpo da resposta).
/// </summary>
public record ResultadoAutenticacao
{
    public bool Sucesso { get; private init; }

    public string? Erro { get; private init; }

    public string? AccessToken { get; private init; }

    public DateTime? AccessTokenExpiraEmUtc { get; private init; }

    public string? RefreshToken { get; private init; }

    public DateTime? RefreshTokenExpiraEmUtc { get; private init; }

    /// <summary>Usado, por exemplo, pra mesclar o carrinho anônimo no carrinho do usuário após o login (ver Fase 3 do plano).</summary>
    public Guid? UsuarioId { get; private init; }

    public static ResultadoAutenticacao ComFalha(string erro) => new() { Sucesso = false, Erro = erro };

    public static ResultadoAutenticacao ComSucesso(
        Guid usuarioId,
        string accessToken,
        DateTime accessTokenExpiraEmUtc,
        string refreshToken,
        DateTime refreshTokenExpiraEmUtc) => new()
        {
            Sucesso = true,
            UsuarioId = usuarioId,
            AccessToken = accessToken,
            AccessTokenExpiraEmUtc = accessTokenExpiraEmUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiraEmUtc = refreshTokenExpiraEmUtc,
        };
}
