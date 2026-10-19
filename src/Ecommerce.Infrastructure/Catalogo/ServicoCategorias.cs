using Ecommerce.Application.Catalogo;
using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Catalogo;

public class ServicoCategorias(ContextoBancoDados contexto) : IServicoCategorias
{
    public async Task<CategoriaDto> CriarAsync(CriarCategoriaComando comando, CancellationToken ct = default)
    {
        Guid? categoriaPaiId = null;
        if (comando.CategoriaPaiId is { } id)
        {
            categoriaPaiId = (await ObterOuFalharAsync(id, ct)).Id;
        }

        var categoria = Categoria.Criar(comando.Nome, categoriaPaiId);
        contexto.Categorias.Add(categoria);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(categoria);
    }

    public async Task<CategoriaDto> RenomearAsync(RenomearCategoriaComando comando, CancellationToken ct = default)
    {
        var categoria = await ObterOuFalharAsync(comando.CategoriaId, ct);
        categoria.Renomear(comando.NovoNome);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(categoria);
    }

    public async Task<CategoriaDto> MoverAsync(MoverCategoriaComando comando, CancellationToken ct = default)
    {
        var categoria = await ObterOuFalharAsync(comando.CategoriaId, ct);

        Categoria? novaCategoriaPai = null;
        if (comando.NovaCategoriaPaiId is { } novaCategoriaPaiId)
        {
            novaCategoriaPai = await ObterOuFalharAsync(novaCategoriaPaiId, ct);
        }

        categoria.MoverPara(novaCategoriaPai);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(categoria);
    }

    public async Task<IReadOnlyCollection<CategoriaDto>> ListarArvoreAsync(CancellationToken ct = default)
    {
        var todas = await contexto.Categorias.AsNoTracking().ToListAsync(ct);

        // Agrupa uma vez por pai (O(n)) em vez de varrer a lista inteira a cada nó (O(n²)).
        var filhasPorPai = todas.ToLookup(c => c.CategoriaPaiId);

        return filhasPorPai[null]
            .Select(raiz => MapearParaDtoComFilhos(raiz, filhasPorPai))
            .ToList();
    }

    public async Task<CategoriaDto?> ObterPorSlugAsync(string slug, CancellationToken ct = default)
    {
        var categoria = await contexto.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, ct);
        return categoria is null ? null : MapearParaDto(categoria);
    }

    private async Task<Categoria> ObterOuFalharAsync(Guid id, CancellationToken ct) =>
        await contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ExcecaoDeDominio("Categoria não encontrada.");

    private static CategoriaDto MapearParaDto(Categoria categoria) =>
        new(categoria.Id, categoria.Nome, categoria.Slug, categoria.CategoriaPaiId, []);

    private static CategoriaDto MapearParaDtoComFilhos(Categoria categoria, ILookup<Guid?, Categoria> filhasPorPai)
    {
        var filhas = filhasPorPai[categoria.Id]
            .Select(filha => MapearParaDtoComFilhos(filha, filhasPorPai))
            .ToList();

        return new CategoriaDto(categoria.Id, categoria.Nome, categoria.Slug, categoria.CategoriaPaiId, filhas);
    }
}
