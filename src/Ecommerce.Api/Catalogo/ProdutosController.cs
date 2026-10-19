using System.Security.Claims;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Comum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Catalogo;

[ApiController]
[Route("api/v1/produtos")]
public class ProdutosController(IServicoProdutos servicoProdutos) : ControllerBase
{
    /// <summary>
    /// Busca paginada — endpoint público. Sempre restrita a produtos ativos com pelo menos uma
    /// variante ativa em estoque (não dá pra ver produto inativo por aqui, só admin gerencia isso).
    /// </summary>
    [HttpGet]
    [EnableRateLimiting(PoliticasDeRateLimiting.BuscaDeProdutos)]
    public async Task<ActionResult<ResultadoPaginado<ProdutoResumoDto>>> Buscar(
        [FromQuery] BuscarProdutosRequisicao requisicao,
        CancellationToken ct = default)
    {
        var consulta = new BuscarProdutosConsulta(
            requisicao.CategoriaId,
            requisicao.PrecoMinimo,
            requisicao.PrecoMaximo,
            requisicao.Termo,
            ApenasDisponiveis: true,
            requisicao.Ordenacao,
            requisicao.Pagina,
            requisicao.TamanhoPagina);

        return Ok(await servicoProdutos.BuscarAsync(consulta, ct));
    }

    /// <summary>
    /// Admin: busca sem o filtro de "disponível" (inclui produto inativo e sem variante em
    /// estoque) — a busca pública (<see cref="Buscar"/>) esconde exatamente isso, então o admin
    /// não teria como ver/gerenciar um produto recém-criado antes dele ganhar uma variante.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ResultadoPaginado<ProdutoResumoDto>>> BuscarAdmin(
        [FromQuery] Guid? categoriaId,
        [FromQuery] string? termo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var consulta = new BuscarProdutosConsulta(
            categoriaId, null, null, termo, ApenasDisponiveis: false, OrdenacaoProduto.MaisRecente, pagina, tamanhoPagina);

        return Ok(await servicoProdutos.BuscarAsync(consulta, ct));
    }

    /// <summary>Detalhe por id — público (não filtra por ativo: um link direto continua funcionando mesmo pra produto descontinuado).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProdutoDto>> ObterPorId(Guid id, CancellationToken ct)
    {
        var produto = await servicoProdutos.ObterPorIdAsync(id, ct);
        return produto is null ? NotFound() : Ok(produto);
    }

    /// <summary>Detalhe por slug — público (usado pelo frontend, que roteia produto por slug em vez de id).</summary>
    [HttpGet("por-slug/{slug}")]
    public async Task<ActionResult<ProdutoDto>> ObterPorSlug(string slug, CancellationToken ct)
    {
        var produto = await servicoProdutos.ObterPorSlugAsync(slug, ct);
        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> Criar(CriarProdutoRequisicao requisicao, CancellationToken ct)
    {
        var produto = await servicoProdutos.CriarAsync(
            new CriarProdutoComando(requisicao.Nome, requisicao.Descricao, requisicao.Sku, requisicao.Preco, requisicao.CategoriaId), ct);

        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> Atualizar(Guid id, AtualizarProdutoRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoProdutos.AtualizarAsync(new AtualizarProdutoComando(id, requisicao.Nome, requisicao.Descricao, requisicao.Preco), ct));

    [HttpPost("{id:guid}/inativar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> Inativar(Guid id, CancellationToken ct) =>
        Ok(await servicoProdutos.InativarAsync(id, ct));

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> Ativar(Guid id, CancellationToken ct) =>
        Ok(await servicoProdutos.AtivarAsync(id, ct));

    [HttpPost("{id:guid}/variantes")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> AdicionarVariante(Guid id, AdicionarVarianteRequisicao requisicao, CancellationToken ct)
    {
        var produto = await servicoProdutos.AdicionarVarianteAsync(
            new AdicionarVarianteComando(id, requisicao.Nome, requisicao.Sku, requisicao.Preco, requisicao.Estoque), ct);

        return Ok(produto);
    }

    /// <summary>Envia a imagem principal/adicional do produto — JPEG/PNG/WebP, até 5 MB.</summary>
    [HttpPost("{id:guid}/imagens")]
    [Authorize(Roles = Papeis.Admin)]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<ProdutoDto>> AdicionarImagem(Guid id, IFormFile arquivo, CancellationToken ct)
    {
        await using var conteudo = arquivo.OpenReadStream();
        var produto = await servicoProdutos.AdicionarImagemAsync(id, conteudo, arquivo.ContentType, ct);

        return Ok(produto);
    }

    [HttpDelete("{id:guid}/imagens/{imagemId:guid}")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> RemoverImagem(Guid id, Guid imagemId, CancellationToken ct) =>
        Ok(await servicoProdutos.RemoverImagemAsync(id, imagemId, ct));

    /// <summary>Nova ordem das imagens — a primeira da lista vira a principal (usada na listagem).</summary>
    [HttpPut("{id:guid}/imagens/ordem")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> ReordenarImagens(Guid id, ReordenarImagensRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoProdutos.ReordenarImagensAsync(id, requisicao.ImagemIds, ct));

    [HttpPut("{id:guid}/variantes/{varianteId:guid}/estoque")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> AjustarEstoque(Guid id, Guid varianteId, AjustarEstoqueRequisicao requisicao, CancellationToken ct) =>
        Ok(await servicoProdutos.AjustarEstoqueAsync(
            new AjustarEstoqueComando(id, varianteId, requisicao.NovoEstoque, ObterUsuarioId(), requisicao.Motivo), ct));

    /// <summary>Admin: histórico de ajustes manuais de estoque (auditoria) de uma variante.</summary>
    [HttpGet("{id:guid}/variantes/{varianteId:guid}/historico-estoque")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<AjusteDeEstoqueDto>>> HistoricoDeEstoque(Guid id, Guid varianteId, CancellationToken ct) =>
        Ok(await servicoProdutos.ObterHistoricoDeEstoqueAsync(varianteId, ct));

    [HttpPost("{id:guid}/variantes/{varianteId:guid}/inativar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> InativarVariante(Guid id, Guid varianteId, CancellationToken ct) =>
        Ok(await servicoProdutos.InativarVarianteAsync(new InativarVarianteComando(id, varianteId), ct));

    [HttpPost("{id:guid}/variantes/{varianteId:guid}/ativar")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<ActionResult<ProdutoDto>> AtivarVariante(Guid id, Guid varianteId, CancellationToken ct) =>
        Ok(await servicoProdutos.AtivarVarianteAsync(new AtivarVarianteComando(id, varianteId), ct));

    private Guid ObterUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var usuarioId) ? usuarioId : Guid.Empty;
    }
}
