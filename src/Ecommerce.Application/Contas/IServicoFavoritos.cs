namespace Ecommerce.Application.Contas;

/// <summary>
/// Lista de favoritos do cliente. Adicionar e remover são idempotentes (adicionar o que já está na
/// lista ou remover o que não está não é erro): o botão de favoritar pode ser clicado duas vezes,
/// ou em duas abas, sem a segunda chamada falhar.
/// </summary>
public interface IServicoFavoritos
{
    /// <summary>Limite por usuário: a lista é pra guardar produtos, não pra copiar o catálogo.</summary>
    public const int MaximoPorUsuario = 200;

    Task<IReadOnlyCollection<FavoritoDto>> ListarAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Só os Ids dos produtos favoritados, pro coração das páginas sem montar a lista toda.</summary>
    Task<IReadOnlyCollection<Guid>> ListarIdsDeProdutosAsync(Guid usuarioId, CancellationToken ct = default);

    Task AdicionarAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default);

    Task RemoverAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default);
}
