using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Contas;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Contas;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Contas;

public class ServicoFavoritos(ContextoBancoDados contexto) : IServicoFavoritos
{
    public async Task<IReadOnlyCollection<FavoritoDto>> ListarAsync(Guid usuarioId, CancellationToken ct = default)
    {
        // Projeção direto no DTO, como na busca do catálogo: não carrega a Descrição (NCLOB) nem
        // todas as imagens/variantes de cada produto.
        return await contexto.Favoritos
            .AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId)
            .Join(contexto.Produtos, f => f.ProdutoId, p => p.Id, (f, p) => new { Favorito = f, Produto = p })
            .OrderByDescending(x => x.Favorito.CriadoEmUtc)
            .Select(x => new FavoritoDto(
                new ProdutoResumoDto(
                    x.Produto.Id,
                    x.Produto.Nome,
                    x.Produto.Slug,
                    x.Produto.Sku,
                    x.Produto.Preco,
                    x.Produto.CategoriaId,
                    x.Produto.Ativo,
                    x.Produto.Imagens.OrderBy(i => i.Ordem).Select(i => i.Url).FirstOrDefault()),
                x.Produto.Ativo && x.Produto.Variantes.Any(v => v.Ativo && v.Estoque > 0),
                x.Favorito.CriadoEmUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<Guid>> ListarIdsDeProdutosAsync(Guid usuarioId, CancellationToken ct = default) =>
        await contexto.Favoritos
            .AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId)
            .Select(f => f.ProdutoId)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default)
    {
        if (await JaFavoritadoAsync(usuarioId, produtoId, ct))
        {
            return;
        }

        // Produto inativo também não pode ser favoritado: pra quem navega na loja ele não existe.
        if (!await contexto.Produtos.AnyAsync(p => p.Id == produtoId && p.Ativo, ct))
        {
            throw new ExcecaoDeDominio("Produto não encontrado.");
        }

        if (await contexto.Favoritos.CountAsync(f => f.UsuarioId == usuarioId, ct) >= IServicoFavoritos.MaximoPorUsuario)
        {
            throw new ExcecaoDeDominio(
                $"A lista de favoritos chegou ao limite de {IServicoFavoritos.MaximoPorUsuario} produtos. Remova algum para adicionar outro.");
        }

        var favorito = Favorito.Criar(usuarioId, produtoId);
        contexto.Favoritos.Add(favorito);

        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            contexto.Entry(favorito).State = EntityState.Detached;

            // Duas requisições ao mesmo tempo passaram pela checagem acima e a segunda bateu no
            // índice único: o produto está na lista, que é o que foi pedido. Qualquer outra falha sobe.
            if (!await JaFavoritadoAsync(usuarioId, produtoId, ct))
            {
                throw;
            }
        }
    }

    public async Task RemoverAsync(Guid usuarioId, Guid produtoId, CancellationToken ct = default)
    {
        var favorito = await contexto.Favoritos.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.ProdutoId == produtoId, ct);
        if (favorito is null)
        {
            return;
        }

        contexto.Favoritos.Remove(favorito);
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Outra requisição removeu antes: o produto já não está na lista, que é o que foi pedido.
        }
    }

    private Task<bool> JaFavoritadoAsync(Guid usuarioId, Guid produtoId, CancellationToken ct) =>
        contexto.Favoritos.AsNoTracking().AnyAsync(f => f.UsuarioId == usuarioId && f.ProdutoId == produtoId, ct);
}
