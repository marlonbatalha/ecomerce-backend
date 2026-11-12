using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Contas;

/// <summary>
/// Produto que um usuário marcou como favorito (lista de desejos). Só liga usuário e produto e
/// guarda quando foi marcado; "um produto aparece uma vez por usuário" é garantido pelo índice
/// único (UsuarioId, ProdutoId) e pelo serviço, que trata adicionar de novo como sucesso.
/// </summary>
public class Favorito : Entidade
{
    public Guid UsuarioId { get; private set; }

    public Guid ProdutoId { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    private Favorito()
    {
    }

    private Favorito(Guid id, Guid usuarioId, Guid produtoId)
        : base(id)
    {
        UsuarioId = usuarioId;
        ProdutoId = produtoId;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static Favorito Criar(Guid usuarioId, Guid produtoId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O favorito precisa estar associado a um usuário.");
        }

        if (produtoId == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O favorito precisa estar associado a um produto.");
        }

        return new Favorito(Guid.NewGuid(), usuarioId, produtoId);
    }
}
