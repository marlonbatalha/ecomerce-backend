using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Vendas;

public class ServicoCarrinho(ContextoBancoDados contexto) : IServicoCarrinho
{
    public async Task<CarrinhoDto> ObterOuCriarParaUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var carrinho = await contexto.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);
        if (carrinho is null)
        {
            carrinho = Carrinho.CriarParaUsuario(usuarioId);
            contexto.Add(carrinho);
            await contexto.SaveChangesAsync(ct);
        }

        return await MontarDtoAsync(carrinho, ct);
    }

    public async Task<CarrinhoDto> ObterOuCriarAnonimoAsync(string idSessao, CancellationToken ct = default)
    {
        var carrinho = await contexto.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.IdSessao == idSessao, ct);
        if (carrinho is null)
        {
            carrinho = Carrinho.CriarAnonimo(idSessao);
            contexto.Add(carrinho);
            await contexto.SaveChangesAsync(ct);
        }

        return await MontarDtoAsync(carrinho, ct);
    }

    public async Task<CarrinhoDto> AdicionarItemAsync(AdicionarItemCarrinhoComando comando, CancellationToken ct = default)
    {
        var carrinho = await ObterAgregadoOuFalharAsync(comando.CarrinhoId, ct);
        var variante = await ObterVarianteOuFalharAsync(comando.VarianteDeProdutoId, ct);

        var novoItem = carrinho.AdicionarItem(comando.VarianteDeProdutoId, comando.Quantidade, variante.Estoque);
        if (novoItem is not null)
        {
            // Ver commit anterior: EF não detecta com segurança item novo numa coleção
            // field-mapped só por mutação do campo privado — precisa Add explícito.
            contexto.Add(novoItem);
        }

        await contexto.SaveChangesAsync(ct);
        return await MontarDtoAsync(carrinho, ct);
    }

    public async Task<CarrinhoDto> AtualizarQuantidadeAsync(AtualizarQuantidadeCarrinhoComando comando, CancellationToken ct = default)
    {
        var carrinho = await ObterAgregadoOuFalharAsync(comando.CarrinhoId, ct);
        var variante = await ObterVarianteOuFalharAsync(comando.VarianteDeProdutoId, ct);

        carrinho.AtualizarQuantidade(comando.VarianteDeProdutoId, comando.Quantidade, variante.Estoque);
        await contexto.SaveChangesAsync(ct);
        return await MontarDtoAsync(carrinho, ct);
    }

    public async Task<CarrinhoDto> RemoverItemAsync(RemoverItemCarrinhoComando comando, CancellationToken ct = default)
    {
        var carrinho = await ObterAgregadoOuFalharAsync(comando.CarrinhoId, ct);
        var itemRemovido = carrinho.RemoverItem(comando.VarianteDeProdutoId);
        contexto.Remove(itemRemovido);

        await contexto.SaveChangesAsync(ct);
        return await MontarDtoAsync(carrinho, ct);
    }

    public async Task MesclarAnonimoNoUsuarioAsync(string idSessaoAnonimo, Guid usuarioId, CancellationToken ct = default)
    {
        var carrinhoAnonimo = await contexto.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.IdSessao == idSessaoAnonimo, ct);
        if (carrinhoAnonimo is null)
        {
            return;
        }

        if (carrinhoAnonimo.Itens.Count == 0)
        {
            contexto.Remove(carrinhoAnonimo);
            await contexto.SaveChangesAsync(ct);
            return;
        }

        var carrinhoUsuario = await contexto.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);
        var carrinhoUsuarioEhNovo = carrinhoUsuario is null;
        carrinhoUsuario ??= Carrinho.CriarParaUsuario(usuarioId);

        var varianteIds = carrinhoAnonimo.Itens.Select(i => i.VarianteDeProdutoId).ToList();
        var estoques = await contexto.Set<VarianteDeProduto>()
            .Where(v => varianteIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Estoque, ct);

        var itensNovos = carrinhoUsuario.MesclarCom(carrinhoAnonimo, estoques);

        if (carrinhoUsuarioEhNovo)
        {
            contexto.Add(carrinhoUsuario);
        }

        foreach (var item in itensNovos)
        {
            contexto.Add(item);
        }

        contexto.Remove(carrinhoAnonimo);
        await contexto.SaveChangesAsync(ct);
    }

    private async Task<Carrinho> ObterAgregadoOuFalharAsync(Guid carrinhoId, CancellationToken ct) =>
        await contexto.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == carrinhoId, ct)
            ?? throw new ExcecaoDeDominio("Carrinho não encontrado.");

    private async Task<VarianteDeProduto> ObterVarianteOuFalharAsync(Guid varianteId, CancellationToken ct) =>
        await contexto.Set<VarianteDeProduto>().FirstOrDefaultAsync(v => v.Id == varianteId, ct)
            ?? throw new ExcecaoDeDominio("Variante de produto não encontrada.");

    private async Task<CarrinhoDto> MontarDtoAsync(Carrinho carrinho, CancellationToken ct)
    {
        if (carrinho.Itens.Count == 0)
        {
            return new CarrinhoDto(carrinho.Id, [], 0m);
        }

        var varianteIds = carrinho.Itens.Select(i => i.VarianteDeProdutoId).ToList();
        var variantes = await contexto.Set<VarianteDeProduto>()
            .Where(v => varianteIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var produtoIds = variantes.Values.Select(v => v.ProdutoId).Distinct().ToList();
        var produtos = await contexto.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Nome,
                p.Slug,
                ImagemUrl = p.Imagens.OrderBy(i => i.Ordem).Select(i => i.Url).FirstOrDefault(),
                VariantesAtivas = p.Variantes.Count(v => v.Ativo),
            })
            .ToDictionaryAsync(p => p.Id, ct);

        var itens = new List<ItemCarrinhoDto>();
        foreach (var item in carrinho.Itens)
        {
            if (!variantes.TryGetValue(item.VarianteDeProdutoId, out var variante))
            {
                // Defensivo: produto/variante nunca é apagado de verdade (só inativado) hoje, mas
                // não custa não quebrar a montagem do carrinho se isso mudar no futuro.
                continue;
            }

            var produto = produtos.GetValueOrDefault(variante.ProdutoId);
            itens.Add(new ItemCarrinhoDto(
                item.VarianteDeProdutoId,
                produto?.Nome ?? "(produto indisponível)",
                variante.Nome,
                variante.Preco,
                item.Quantidade,
                variante.Preco * item.Quantidade,
                produto?.Slug ?? string.Empty,
                produto?.ImagemUrl,
                variante.Ativo ? variante.Estoque : 0,
                produto?.VariantesAtivas > 1));
        }

        return new CarrinhoDto(carrinho.Id, itens, itens.Sum(i => i.Subtotal));
    }
}
