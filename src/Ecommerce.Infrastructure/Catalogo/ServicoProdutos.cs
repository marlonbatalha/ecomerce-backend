using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Comum;
using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Catalogo;

public class ServicoProdutos(ContextoBancoDados contexto) : IServicoProdutos
{
    public async Task<ProdutoDto> CriarAsync(CriarProdutoComando comando, CancellationToken ct = default)
    {
        var categoriaExiste = await contexto.Categorias.AnyAsync(c => c.Id == comando.CategoriaId, ct);
        if (!categoriaExiste)
        {
            throw new ExcecaoDeDominio("Categoria informada não existe.");
        }

        await GarantirSkuDeProdutoUnicoAsync(comando.Sku, ct);

        var produto = Produto.Criar(comando.Nome, comando.Descricao, comando.Sku, comando.Preco, comando.CategoriaId);
        contexto.Produtos.Add(produto);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> AtualizarAsync(AtualizarProdutoComando comando, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(comando.ProdutoId, ct);
        produto.AtualizarDados(comando.Nome, comando.Descricao, comando.Preco);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> InativarAsync(Guid produtoId, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(produtoId, ct);
        produto.Inativar();
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> AtivarAsync(Guid produtoId, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(produtoId, ct);
        produto.Ativar();
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> AdicionarVarianteAsync(AdicionarVarianteComando comando, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(comando.ProdutoId, ct);
        await GarantirSkuDeVarianteUnicoAsync(comando.Sku, ct);

        var variante = produto.AdicionarVariante(comando.Nome, comando.Sku, comando.Preco, comando.Estoque);
        // O EF Core não detecta com segurança um item novo adicionado a uma coleção mapeada com
        // PropertyAccessMode.Field só por mutação do campo privado (visto na prática com o
        // provider InMemory) — adiciona explicitamente ao change tracker.
        contexto.Add(variante);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> AjustarEstoqueAsync(AjustarEstoqueComando comando, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(comando.ProdutoId, ct);
        var variante = produto.Variantes.FirstOrDefault(v => v.Id == comando.VarianteId)
            ?? throw new ExcecaoDeDominio("Variante não encontrada neste produto.");
        var estoqueAnterior = variante.Estoque;

        produto.AjustarEstoqueDaVariante(comando.VarianteId, comando.NovoEstoque);

        // Todo ajuste manual de estoque é auditado — ver Fase 4 do plano ("gestão de estoque com
        // motivo/auditoria"). Entidade nova e independente: Add explícito de qualquer forma.
        var ajuste = AjusteDeEstoque.Criar(comando.VarianteId, comando.UsuarioId, estoqueAnterior, comando.NovoEstoque, comando.Motivo);
        contexto.Add(ajuste);

        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> InativarVarianteAsync(InativarVarianteComando comando, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(comando.ProdutoId, ct);
        produto.InativarVariante(comando.VarianteId);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto> AtivarVarianteAsync(AtivarVarianteComando comando, CancellationToken ct = default)
    {
        var produto = await ObterAgregadoOuFalharAsync(comando.ProdutoId, ct);
        produto.AtivarVariante(comando.VarianteId);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(produto);
    }

    public async Task<ProdutoDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var produto = await contexto.Produtos
            .AsNoTracking()
            .Include(p => p.Variantes)
            .Include(p => p.Imagens)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return produto is null ? null : MapearParaDto(produto);
    }

    public async Task<ResultadoPaginado<ProdutoResumoDto>> BuscarAsync(BuscarProdutosConsulta consulta, CancellationToken ct = default)
    {
        var pagina = Math.Max(consulta.Pagina, 1);
        var tamanhoPagina = Math.Clamp(consulta.TamanhoPagina, 1, 100);

        var query = contexto.Produtos.AsNoTracking().Include(p => p.Imagens).AsQueryable();

        if (consulta.CategoriaId is { } categoriaId)
        {
            query = query.Where(p => p.CategoriaId == categoriaId);
        }

        if (consulta.PrecoMinimo is { } precoMinimo)
        {
            query = query.Where(p => p.Preco >= precoMinimo);
        }

        if (consulta.PrecoMaximo is { } precoMaximo)
        {
            query = query.Where(p => p.Preco <= precoMaximo);
        }

        if (!string.IsNullOrWhiteSpace(consulta.Termo))
        {
            query = query.Where(p => p.Nome.Contains(consulta.Termo));
        }

        if (consulta.ApenasDisponiveis)
        {
            query = query.Where(p => p.Ativo && p.Variantes.Any(v => v.Ativo && v.Estoque > 0));
        }

        query = consulta.Ordenacao switch
        {
            OrdenacaoProduto.PrecoAsc => query.OrderBy(p => p.Preco),
            OrdenacaoProduto.PrecoDesc => query.OrderByDescending(p => p.Preco),
            _ => query.OrderByDescending(p => p.CriadoEmUtc),
        };

        var totalItens = await query.CountAsync(ct);

        var produtos = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

        var itens = produtos.Select(MapearParaResumoDto).ToList();

        return new ResultadoPaginado<ProdutoResumoDto>(itens, pagina, tamanhoPagina, totalItens);
    }

    public async Task<IReadOnlyCollection<AjusteDeEstoqueDto>> ObterHistoricoDeEstoqueAsync(Guid varianteId, CancellationToken ct = default)
    {
        var ajustes = await contexto.Set<AjusteDeEstoque>()
            .AsNoTracking()
            .Where(a => a.VarianteDeProdutoId == varianteId)
            .OrderByDescending(a => a.CriadoEmUtc)
            .ToListAsync(ct);

        return ajustes
            .Select(a => new AjusteDeEstoqueDto(a.Id, a.EstoqueAnterior, a.EstoqueNovo, a.Motivo, a.UsuarioId, a.CriadoEmUtc))
            .ToList();
    }

    private async Task<Produto> ObterAgregadoOuFalharAsync(Guid id, CancellationToken ct) =>
        await contexto.Produtos
            .Include(p => p.Variantes)
            .Include(p => p.Imagens)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new ExcecaoDeDominio("Produto não encontrado.");

    private async Task GarantirSkuDeProdutoUnicoAsync(string sku, CancellationToken ct)
    {
        var skuNormalizado = sku.Trim().ToUpperInvariant();
        if (await contexto.Produtos.AnyAsync(p => p.Sku == skuNormalizado, ct))
        {
            throw new ExcecaoDeDominio($"Já existe um produto com o SKU '{skuNormalizado}'.");
        }
    }

    private async Task GarantirSkuDeVarianteUnicoAsync(string sku, CancellationToken ct)
    {
        var skuNormalizado = sku.Trim().ToUpperInvariant();
        if (await contexto.Set<VarianteDeProduto>().AnyAsync(v => v.Sku == skuNormalizado, ct))
        {
            throw new ExcecaoDeDominio($"Já existe uma variante com o SKU '{skuNormalizado}'.");
        }
    }

    private static ProdutoDto MapearParaDto(Produto produto) => new(
        produto.Id,
        produto.Nome,
        produto.Descricao,
        produto.Sku,
        produto.Preco,
        produto.CategoriaId,
        produto.Ativo,
        produto.Variantes.Select(v => new VarianteDto(v.Id, v.Nome, v.Sku, v.Preco, v.Estoque, v.Ativo)).ToList(),
        produto.Imagens.OrderBy(i => i.Ordem).Select(i => new ImagemDto(i.Id, i.Url, i.Ordem)).ToList());

    private static ProdutoResumoDto MapearParaResumoDto(Produto produto) => new(
        produto.Id,
        produto.Nome,
        produto.Sku,
        produto.Preco,
        produto.CategoriaId,
        produto.Ativo,
        produto.Imagens.OrderBy(i => i.Ordem).Select(i => i.Url).FirstOrDefault());
}
