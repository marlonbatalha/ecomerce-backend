using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Catalogo;

[ApiController]
[Route("api/v1/categorias")]
public class CategoriasController(IServicoCategorias servicoCategorias) : ControllerBase
{
    /// <summary>Árvore completa de categorias — endpoint público.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoriaDto>>> Listar(CancellationToken ct) =>
        Ok(await servicoCategorias.ListarArvoreAsync(ct));

    /// <summary>Detalhe de uma categoria pelo slug — endpoint público.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<CategoriaDto>> ObterPorSlug(string slug, CancellationToken ct)
    {
        var categoria = await servicoCategorias.ObterPorSlugAsync(slug, ct);
        return categoria is null ? NotFound() : Ok(categoria);
    }

    [HttpPost]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<CategoriaDto>> Criar(CriarCategoriaRequisicao requisicao, CancellationToken ct)
    {
        var categoria = await servicoCategorias.CriarAsync(
            new CriarCategoriaComando(requisicao.Nome, requisicao.CategoriaPaiId), ct);

        return CreatedAtAction(nameof(ObterPorSlug), new { slug = categoria.Slug }, categoria);
    }

    [HttpPut("{id:guid}/renomear")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<CategoriaDto>> Renomear(Guid id, RenomearCategoriaRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoCategorias.RenomearAsync(new RenomearCategoriaComando(id, requisicao.NovoNome), ct));

    [HttpPut("{id:guid}/mover")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<CategoriaDto>> Mover(Guid id, MoverCategoriaRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoCategorias.MoverAsync(new MoverCategoriaComando(id, requisicao.NovaCategoriaPaiId), ct));
}
