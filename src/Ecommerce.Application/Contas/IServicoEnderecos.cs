namespace Ecommerce.Application.Contas;

/// <summary>
/// Casos de uso de endereço. Todo método que recebe <c>enderecoId</c> também recebe
/// <c>usuarioId</c> e checa posse — evita IDOR (um cliente não pode ver/editar endereço de outro).
/// </summary>
public interface IServicoEnderecos
{
    Task<EnderecoDto> CriarAsync(CriarEnderecoComando comando, CancellationToken ct = default);

    Task<EnderecoDto> AtualizarAsync(AtualizarEnderecoComando comando, CancellationToken ct = default);

    Task RemoverAsync(Guid enderecoId, Guid usuarioId, CancellationToken ct = default);

    /// <summary>Marca como padrão e desmarca qualquer outro endereço padrão do mesmo usuário.</summary>
    Task<EnderecoDto> MarcarComoPadraoAsync(Guid enderecoId, Guid usuarioId, CancellationToken ct = default);

    Task<IReadOnlyCollection<EnderecoDto>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
