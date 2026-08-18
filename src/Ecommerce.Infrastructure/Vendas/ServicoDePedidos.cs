using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Vendas;

public class ServicoDePedidos(ContextoBancoDados contexto, IServicoDePagamento servicoDePagamento) : IServicoDePedidos
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
        contexto.Add(pedido);

        var itensRemovidos = carrinho.Esvaziar();
        contexto.RemoveRange(itensRemovidos);

        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // VarianteDeProduto.Estoque é ConcurrencyToken — outro checkout debitou a mesma
            // variante entre a leitura e o SaveChanges. Falha alto e claro em vez de overselling.
            throw new ExcecaoDeDominio("O estoque de um ou mais itens mudou durante o checkout — atualize o carrinho e tente de novo.");
        }

        var referenciaPagamento = await servicoDePagamento.IniciarCobrancaAsync(pedido.Id, pedido.Total, ct);
        var pagamento = Pagamento.Criar(pedido.Id, referenciaPagamento, pedido.Total);
        contexto.Add(pagamento);
        await contexto.SaveChangesAsync(ct);

        return new CheckoutResultado(MapearParaDto(pedido), referenciaPagamento);
    }

    public async Task<PedidoDto?> ObterDoUsuarioAsync(Guid pedidoId, Guid usuarioId, CancellationToken ct = default)
    {
        var pedido = await contexto.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        return pedido is null || pedido.UsuarioId != usuarioId ? null : MapearParaDto(pedido);
    }

    public async Task<IReadOnlyCollection<PedidoResumoDto>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var pedidos = await contexto.Pedidos.AsNoTracking()
            .Include(p => p.Itens)
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEmUtc)
            .ToListAsync(ct);

        return pedidos.Select(p => new PedidoResumoDto(p.Id, p.Status, p.CriadoEmUtc, p.Total, p.Itens.Count)).ToList();
    }

    public async Task<PedidoDto> CancelarAsync(Guid pedidoId, Guid usuarioId, CancellationToken ct = default)
    {
        var pedido = await ObterAgregadoOuFalharAsync(pedidoId, ct);
        if (pedido.UsuarioId != usuarioId)
        {
            throw new ExcecaoDeDominio("Pedido não encontrado.");
        }

        pedido.Cancelar();
        await contexto.SaveChangesAsync(ct);
        return MapearParaDto(pedido);
    }

    public async Task<PedidoDto> MarcarComoEnviadoAsync(Guid pedidoId, string codigoRastreio, CancellationToken ct = default)
    {
        var pedido = await ObterAgregadoOuFalharAsync(pedidoId, ct);
        pedido.MarcarComoEnviado(codigoRastreio);
        await contexto.SaveChangesAsync(ct);
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

        var pedido = await contexto.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pagamento.PedidoId, ct)
            ?? throw new ExcecaoDeDominio("Pedido do pagamento não encontrado.");

        if (aprovado)
        {
            pagamento.Aprovar();

            // Idempotente: se o webhook reenviar o mesmo evento e o pedido já tiver saído de
            // Pendente, não tenta transicionar de novo (Pedido.ConfirmarPagamento lançaria, já
            // que só Pendente -> PagamentoConfirmado é uma transição válida).
            if (pedido.Status == StatusPedido.Pendente)
            {
                pedido.ConfirmarPagamento();
            }
        }
        else
        {
            pagamento.Recusar();

            if (pedido.Status == StatusPedido.Pendente)
            {
                pedido.RecusarPagamento();
            }
        }

        await contexto.SaveChangesAsync(ct);
    }

    private async Task<Pedido> ObterAgregadoOuFalharAsync(Guid pedidoId, CancellationToken ct) =>
        await contexto.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId, ct)
            ?? throw new ExcecaoDeDominio("Pedido não encontrado.");

    private static PedidoDto MapearParaDto(Pedido pedido) => new(
        pedido.Id,
        pedido.Status,
        pedido.CriadoEmUtc,
        pedido.CodigoRastreio,
        pedido.Total,
        pedido.Itens.Select(i => new ItemPedidoDto(i.VarianteDeProdutoId, i.NomeProduto, i.PrecoUnitario, i.Quantidade, i.Subtotal)).ToList());
}
