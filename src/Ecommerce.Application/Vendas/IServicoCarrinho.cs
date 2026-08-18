namespace Ecommerce.Application.Vendas;

/// <summary>
/// Casos de uso de carrinho. Implementado em Infrastructure — a Api resolve o carrinho atual
/// (usuário autenticado ou sessão anônima via cookie) e passa o <c>CarrinhoId</c> concreto pros
/// demais métodos (ver CarrinhoController).
/// </summary>
public interface IServicoCarrinho
{
    Task<CarrinhoDto> ObterOuCriarParaUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task<CarrinhoDto> ObterOuCriarAnonimoAsync(string idSessao, CancellationToken ct = default);

    Task<CarrinhoDto> AdicionarItemAsync(AdicionarItemCarrinhoComando comando, CancellationToken ct = default);

    Task<CarrinhoDto> AtualizarQuantidadeAsync(AtualizarQuantidadeCarrinhoComando comando, CancellationToken ct = default);

    Task<CarrinhoDto> RemoverItemAsync(RemoverItemCarrinhoComando comando, CancellationToken ct = default);

    /// <summary>Funde o carrinho anônimo no carrinho do usuário recém-logado e descarta o anônimo — ver seção "Regras" da Fase 3.</summary>
    Task MesclarAnonimoNoUsuarioAsync(string idSessaoAnonimo, Guid usuarioId, CancellationToken ct = default);
}
