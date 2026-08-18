using System.Security.Claims;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Comum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<ResultadoPaginado<ProdutoResumoDto>>> Buscar(
        [FromQuery] Guid? categoriaId,
        [FromQuery] decimal? precoMinimo,
        [FromQuery] decimal? precoMaximo,
        [FromQuery] string? termo,
        [FromQuery] OrdenacaoProduto ordenacao = OrdenacaoProduto.MaisRecente,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var consulta = new BuscarProdutosConsulta(
            categoriaId, precoMinimo, precoMaximo, termo, ApenasDisponiveis: true, ordenacao, pagina, tamanhoPagina);

        return Ok(await servicoProdutos.BuscarAsync(consulta, ct));
    }

    /// <summary>Detalhe por id — público (não filtra por ativo: um link direto continua funcionando mesmo pra produto descontinuado).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProdutoDto>> ObterPorId(Guid id, CancellationToken ct)
    {
        var produto = await servicoProdutos.ObterPorIdAsync(id, ct);
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
