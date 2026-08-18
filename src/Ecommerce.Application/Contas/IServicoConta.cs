namespace Ecommerce.Application.Contas;

/// <summary>
/// Perfil do próprio usuário logado (dados cadastrais, troca de senha). Falhas (senha atual
/// incorreta, nova senha fraca) lançam <see cref="Ecommerce.Domain.Comum.ExcecaoDeDominio"/>.
/// </summary>
public interface IServicoConta
{
    Task<PerfilDto?> ObterPerfilAsync(Guid usuarioId, CancellationToken ct = default);

    Task<PerfilDto> AtualizarPerfilAsync(AtualizarPerfilComando comando, CancellationToken ct = default);

    Task TrocarSenhaAsync(TrocarSenhaComando comando, CancellationToken ct = default);
}
