using Ecommerce.Application.Vendas;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Vendas;

public class ServicoCupons(ContextoBancoDados contexto) : IServicoCupons
{
    public async Task<IReadOnlyCollection<CupomDto>> ListarAsync(CancellationToken ct = default)
    {
        var cupons = await contexto.Cupons.AsNoTracking().OrderByDescending(c => c.CriadoEmUtc).ToListAsync(ct);
        return cupons.Select(MapearParaDto).ToList();
    }

    public async Task<CupomDto> CriarAsync(CriarCupomComando comando, CancellationToken ct = default)
    {
        var cupom = Cupom.Criar(comando.Codigo, comando.Descricao, comando.Tipo, comando.Valor,
            comando.ValorMinimoDoPedido, comando.ValidoAteUtc, comando.LimiteDeUsos);

        if (await contexto.Cupons.AnyAsync(c => c.Codigo == cupom.Codigo, ct))
        {
            throw new ExcecaoDeDominio($"Já existe um cupom com o código '{cupom.Codigo}'.");
        }

        contexto.Cupons.Add(cupom);
        await contexto.SaveChangesAsync(ct);

        return MapearParaDto(cupom);
    }

    public async Task<CupomDto> AtivarAsync(Guid cupomId, CancellationToken ct = default)
    {
        var cupom = await ObterOuFalharAsync(cupomId, ct);
        cupom.Ativar();
        await contexto.SaveChangesAsync(ct);
        return MapearParaDto(cupom);
    }

    public async Task<CupomDto> InativarAsync(Guid cupomId, CancellationToken ct = default)
    {
        var cupom = await ObterOuFalharAsync(cupomId, ct);
        cupom.Inativar();
        await contexto.SaveChangesAsync(ct);
        return MapearParaDto(cupom);
    }

    public async Task<ValidacaoDeCupomDto> ValidarAsync(string codigo, decimal subtotal, CancellationToken ct = default)
    {
        var codigoNormalizado = Cupom.NormalizarCodigo(codigo ?? string.Empty);
        var cupom = await contexto.Cupons.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigoNormalizado, ct)
            ?? throw new ExcecaoDeDominio("Cupom não encontrado.");

        var desconto = cupom.CalcularDesconto(subtotal, DateTime.UtcNow);
        return new ValidacaoDeCupomDto(cupom.Codigo, cupom.Descricao, desconto);
    }

    private async Task<Cupom> ObterOuFalharAsync(Guid cupomId, CancellationToken ct) =>
        await contexto.Cupons.FirstOrDefaultAsync(c => c.Id == cupomId, ct)
            ?? throw new ExcecaoDeDominio("Cupom não encontrado.");

    private static CupomDto MapearParaDto(Cupom cupom) => new(
        cupom.Id, cupom.Codigo, cupom.Descricao, cupom.Tipo, cupom.Valor, cupom.ValorMinimoDoPedido,
        cupom.ValidoAteUtc, cupom.LimiteDeUsos, cupom.QuantidadeDeUsos, cupom.Ativo, cupom.CriadoEmUtc);
}
