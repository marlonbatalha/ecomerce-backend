namespace Ecommerce.Application.Autenticacao;

/// <summary>
/// Orquestra registro, login e renovação de sessão. Implementado em Infrastructure (depende do
/// ASP.NET Core Identity) — a Api conhece só esta interface, nunca o Identity diretamente.
/// </summary>
public interface IServicoAutenticacao
{
    Task<ResultadoAutenticacao> RegistrarAsync(RegistrarUsuarioComando comando, CancellationToken ct = default);

    Task<ResultadoAutenticacao> LoginAsync(LoginComando comando, CancellationToken ct = default);

    /// <summary>Troca um refresh token válido por um novo par access+refresh (rotação de token).</summary>
    Task<ResultadoAutenticacao> AtualizarTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revoga um refresh token (logout). Idempotente — não falha se já estiver revogado/inexistente.</summary>
    Task RevogarTokenAsync(string refreshToken, CancellationToken ct = default);
}
