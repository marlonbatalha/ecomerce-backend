using Ecommerce.Application.Comum;
using Ecommerce.Application.Notificacoes;
using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Vendas;

public class ServicoDePedidos(
    ContextoBancoDados contexto,
    IServicoDePagamento servicoDePagamento,
    IServicoDeNotificacoes notificacoes,
    ILogger<ServicoDePedidos> logger) : IServicoDePedidos
{
    public async Task<CheckoutResultado> FinalizarCompraAsync(FinalizarCompraComando comando, CancellationToken ct = default)
    {
        var carrinho = await contexto.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == comando.CarrinhoId, ct)
            ?? throw new ExcecaoDeDominio("Carrinho não encontrado.");

        if (carrinho.UsuarioId != comando.UsuarioId)
        {
            throw new ExcecaoDeDominio("Carrinho não encontrado.");
        }

        if (carrinho.Itens.Count == 0)
        {
            throw new ExcecaoDeDominio("O carrinho está vazio.");
        }

        var varianteIds = carrinho.Itens.Select(i => i.VarianteDeProdutoId).ToList();
        var variantes = await contexto.Set<VarianteDeProduto>()
            .Where(v => varianteIds.Contains(v.Id))
            .ToListAsync(ct);

        var produtoIds = variantes.Select(v => v.ProdutoId).Distinct().ToList();
        var produtos = await contexto.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var rascunhos = new List<ItemPedidoRascunho>();
        foreach (var item in carrinho.Itens)
        {
            var variante = variantes.FirstOrDefault(v => v.Id == item.VarianteDeProdutoId)
                ?? throw new ExcecaoDeDominio("Uma das variantes do carrinho não existe mais.");

            if (!variante.Ativo)
            {
                throw new ExcecaoDeDominio($"A variante '{variante.Nome}' não está mais disponível.");
            }

            // Estoque é revalidado aqui (não confia só na checagem de quando o item entrou no
            // carrinho — pode ter mudado desde então).
            if (item.Quantidade > variante.Estoque)
            {
                throw new ExcecaoDeDominio($"Estoque insuficiente para '{variante.Nome}'.");
            }

            var nomeProduto = produtos.TryGetValue(variante.ProdutoId, out var produto) ? produto.Nome : "(produto indisponível)";
            rascunhos.Add(new ItemPedidoRascunho(variante.Id, nomeProduto, variante.Preco, item.Quantidade));

            variante.AjustarEstoque(variante.Estoque - item.Quantidade);
        }

        var pedido = Pedido.Criar(comando.UsuarioId, rascunhos);

        if (!string.IsNullOrWhiteSpace(comando.CodigoCupom))
        {
            var codigo = Cupom.NormalizarCodigo(comando.CodigoCupom);
            var cupom = await contexto.Cupons.FirstOrDefaultAsync(c => c.Codigo == codigo, ct)
                ?? throw new ExcecaoDeDominio("Cupom não encontrado.");

            var desconto = cupom.CalcularDesconto(pedido.Subtotal, DateTime.UtcNow);
            pedido.AplicarDesconto(cupom.Codigo, desconto);
            cupom.RegistrarUso();
        }

        contexto.Add(pedido);

        // Cobrança é criada no gateway ANTES de gravar qualquer coisa: se o gateway falhar, nada
        // foi persistido (estoque, cupom e carrinho intactos). O inverso — gateway ok e o
        // SaveChanges falhar — deixa só uma cobrança órfã no gateway que ninguém recebeu o link.
        string? referenciaPagamento = null;
        string? urlPagamento = null;
        if (pedido.Total > 0)
        {
            var email = await contexto.Users.Where(u => u.Id == comando.UsuarioId).Select(u => u.Email).FirstOrDefaultAsync(ct);
            var cobranca = await servicoDePagamento.IniciarCobrancaAsync(
                new SolicitacaoDeCobranca(pedido.Id, pedido.Total, $"Pedido #{FormatarNumero(pedido.Id)}", email), ct);

            referenciaPagamento = cobranca.ReferenciaExterna;
            urlPagamento = cobranca.UrlPagamento;
            contexto.Add(Pagamento.Criar(pedido.Id, cobranca.ReferenciaExterna, pedido.Total, cobranca.UrlPagamento));
        }
        else
        {
            // Cupom cobriu 100% — não há o que cobrar.
            pedido.ConfirmarPagamento();
        }

        var itensRemovidos = carrinho.Esvaziar();
        contexto.RemoveRange(itensRemovidos);

        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // VarianteDeProduto.Estoque e Cupom.QuantidadeDeUsos são ConcurrencyToken — outro
            // checkout mexeu neles entre a leitura e o SaveChanges. Falha alto e claro em vez de
            // overselling (ou de estourar o limite do cupom).
            throw new ExcecaoDeDominio("O estoque ou o cupom mudou durante o checkout — atualize o carrinho e tente de novo.");
        }

        await notificacoes.PedidoRecebidoAsync(pedido.Id, ct);
        if (pedido.Status == StatusPedido.PagamentoConfirmado)
        {
            await notificacoes.PagamentoConfirmadoAsync(pedido.Id, ct);
        }

        return new CheckoutResultado(MapearParaDto(pedido, urlPagamento), referenciaPagamento, urlPagamento);
    }

    public async Task<PedidoDto?> ObterDoUsuarioAsync(Guid pedidoId, Guid usuarioId, CancellationToken ct = default)
    {
        var pedido = await contexto.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        return pedido is null || pedido.UsuarioId != usuarioId ? null : MapearParaDto(pedido, await ObterUrlPagamentoAsync(pedido, ct));
    }

    public async Task<IReadOnlyCollection<PedidoResumoDto>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var pedidos = await contexto.Pedidos.AsNoTracking()
            .Include(p => p.Itens)
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEmUtc)
            .ToListAsync(ct);

        return pedidos.Select(MapearParaResumoDto).ToList();
    }

    public async Task<ResultadoPaginado<PedidoResumoDto>> ListarTodosAsync(StatusPedido? status, int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var paginaValida = Math.Max(pagina, 1);
        var tamanhoValido = Math.Clamp(tamanhoPagina, 1, 100);

        var query = contexto.Pedidos.AsNoTracking().Include(p => p.Itens).AsQueryable();
        if (status is { } statusFiltro)
        {
            query = query.Where(p => p.Status == statusFiltro);
        }

        var totalItens = await query.CountAsync(ct);
        var pedidos = await query
            .OrderByDescending(p => p.CriadoEmUtc)
            .Skip((paginaValida - 1) * tamanhoValido)
            .Take(tamanhoValido)
            .ToListAsync(ct);

        return new ResultadoPaginado<PedidoResumoDto>(pedidos.Select(MapearParaResumoDto).ToList(), paginaValida, tamanhoValido, totalItens);
    }

    public async Task<PedidoDto?> ObterPorIdAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await contexto.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        return pedido is null ? null : MapearParaDto(pedido, await ObterUrlPagamentoAsync(pedido, ct));
    }

    public async Task<PedidoDto> CancelarAsync(Guid pedidoId, Guid usuarioId, CancellationToken ct = default)
    {
        var pedido = await ObterAgregadoOuFalharAsync(pedidoId, ct);
        if (pedido.UsuarioId != usuarioId)
        {
            throw new ExcecaoDeDominio("Pedido não encontrado.");
        }

        pedido.Cancelar();
        await DevolverEstoqueAsync(pedido, ct);
        await contexto.SaveChangesAsync(ct);

        await notificacoes.PedidoCanceladoAsync(pedido.Id, ct);
        return MapearParaDto(pedido);
    }

    public async Task<PedidoDto> MarcarComoEnviadoAsync(Guid pedidoId, string codigoRastreio, CancellationToken ct = default)
    {
        var pedido = await ObterAgregadoOuFalharAsync(pedidoId, ct);
        pedido.MarcarComoEnviado(codigoRastreio);
        await contexto.SaveChangesAsync(ct);

        await notificacoes.PedidoEnviadoAsync(pedido.Id, ct);
        return MapearParaDto(pedido);
    }

    public async Task<PedidoDto> MarcarComoEntregueAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await ObterAgregadoOuFalharAsync(pedidoId, ct);
        pedido.MarcarComoEntregue();
        await contexto.SaveChangesAsync(ct);
        return MapearParaDto(pedido);
    }

    public async Task ProcessarConfirmacaoDePagamentoAsync(string referenciaExterna, bool aprovado, CancellationToken ct = default)
    {
        var pagamento = await contexto.Pagamentos.FirstOrDefaultAsync(p => p.ReferenciaExterna == referenciaExterna, ct)
            ?? throw new ExcecaoDeDominio("Pagamento não encontrado para essa referência.");

        await AplicarResultadoAsync(pagamento, aprovado ? ResultadoDePagamento.Aprovado : ResultadoDePagamento.Recusado, ct);
    }

    public async Task ProcessarPagamentoDoGatewayAsync(PagamentoConsultado pagamentoConsultado, CancellationToken ct = default)
    {
        var pagamento = await contexto.Pagamentos
            .Where(p => p.PedidoId == pagamentoConsultado.PedidoId)
            .OrderByDescending(p => p.CriadoEmUtc)
            .FirstOrDefaultAsync(ct);

        if (pagamento is null)
        {
            // external_reference de um pedido que não existe aqui (ex.: conta do gateway
            // compartilhada com outro ambiente) — nada a fazer, e não é erro do gateway.
            logger.LogWarning("Notificação de pagamento para pedido desconhecido {PedidoId}", pagamentoConsultado.PedidoId);
            return;
        }

        pagamento.RegistrarTransacao(pagamentoConsultado.IdTransacao);
        await AplicarResultadoAsync(pagamento, pagamentoConsultado.Resultado, ct);
    }

    /// <summary>
    /// Idempotente: o gateway pode reenviar o mesmo evento, então cada ramo só transiciona o pedido
    /// se ele ainda estiver no status de origem esperado.
    /// </summary>
    private async Task AplicarResultadoAsync(Pagamento pagamento, ResultadoDePagamento resultado, CancellationToken ct)
    {
        var pedido = await ObterAgregadoOuFalharAsync(pagamento.PedidoId, ct);
        var pagamentoConfirmadoAgora = false;

        switch (resultado)
        {
            case ResultadoDePagamento.Aprovado:
                pagamento.Aprovar();
                if (pedido.Status == StatusPedido.Pendente)
                {
                    pedido.ConfirmarPagamento();
                    pagamentoConfirmadoAgora = true;
                }

                break;

            case ResultadoDePagamento.Recusado:
                pagamento.Recusar();
                if (pedido.Status == StatusPedido.Pendente)
                {
                    pedido.RecusarPagamento();
                    await DevolverEstoqueAsync(pedido, ct);
                }

                break;

            case ResultadoDePagamento.Estornado:
                if (pagamento.Status is StatusPagamento.Aprovado or StatusPagamento.Estornado)
                {
                    pagamento.Estornar();
                }

                if (pedido.Status is StatusPedido.PagamentoConfirmado or StatusPedido.Enviado or StatusPedido.Entregue)
                {
                    pedido.Reembolsar();
                }

                break;

            case ResultadoDePagamento.Pendente:
                break;
        }

        await contexto.SaveChangesAsync(ct);

        if (pagamentoConfirmadoAgora)
        {
            await notificacoes.PagamentoConfirmadoAsync(pedido.Id, ct);
        }
    }

    /// <summary>Pedido que não vai mais ser pago/enviado libera o estoque reservado no checkout.</summary>
    private async Task DevolverEstoqueAsync(Pedido pedido, CancellationToken ct)
    {
        var varianteIds = pedido.Itens.Select(i => i.VarianteDeProdutoId).ToList();
        var variantes = await contexto.Set<VarianteDeProduto>().Where(v => varianteIds.Contains(v.Id)).ToListAsync(ct);

        foreach (var item in pedido.Itens)
        {
            var variante = variantes.FirstOrDefault(v => v.Id == item.VarianteDeProdutoId);
            variante?.AjustarEstoque(variante.Estoque + item.Quantidade);
        }
    }

    private async Task<string?> ObterUrlPagamentoAsync(Pedido pedido, CancellationToken ct)
    {
        if (pedido.Status != StatusPedido.Pendente)
        {
            return null;
        }

        return await contexto.Pagamentos.AsNoTracking()
            .Where(p => p.PedidoId == pedido.Id)
            .OrderByDescending(p => p.CriadoEmUtc)
            .Select(p => p.UrlPagamento)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<Pedido> ObterAgregadoOuFalharAsync(Guid pedidoId, CancellationToken ct) =>
        await contexto.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId, ct)
            ?? throw new ExcecaoDeDominio("Pedido não encontrado.");

    /// <summary>Número curto exibido ao cliente (8 primeiros hex do Guid, em maiúsculas).</summary>
    internal static string FormatarNumero(Guid pedidoId) => pedidoId.ToString("N")[..8].ToUpperInvariant();

    private static PedidoResumoDto MapearParaResumoDto(Pedido pedido) =>
        new(pedido.Id, pedido.UsuarioId, pedido.Status, pedido.CriadoEmUtc, pedido.Total, pedido.Itens.Count);

    private static PedidoDto MapearParaDto(Pedido pedido, string? urlPagamento = null) => new(
        pedido.Id,
        pedido.Status,
        pedido.CriadoEmUtc,
        pedido.CodigoRastreio,
        pedido.Subtotal,
        pedido.Desconto,
        pedido.CodigoCupom,
        pedido.Total,
        pedido.Itens.Select(i => new ItemPedidoDto(i.VarianteDeProdutoId, i.NomeProduto, i.PrecoUnitario, i.Quantidade, i.Subtotal)).ToList(),
        pedido.Status == StatusPedido.Pendente ? urlPagamento : null);
}
