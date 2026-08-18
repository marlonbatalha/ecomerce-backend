using Ecommerce.Application.Comum;
using Ecommerce.Application.Contas;
using Ecommerce.Infrastructure.Identidade;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Contas;

public class ServicoClientes(ContextoBancoDados contexto, UserManager<Usuario> gerenciadorUsuarios) : IServicoClientes
{
    public async Task<ResultadoPaginado<ClienteResumoDto>> BuscarAsync(string? termo, int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var paginaValida = Math.Max(pagina, 1);
        var tamanhoValido = Math.Clamp(tamanhoPagina, 1, 100);

        var query = contexto.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.Trim().ToUpperInvariant();
            query = query.Where(u => u.Nome.ToUpper().Contains(termoNormalizado) || (u.Email != null && u.Email.ToUpper().Contains(termoNormalizado)));
        }

        var totalItens = await query.CountAsync(ct);
        var usuarios = await query
            .OrderBy(u => u.Nome)
            .Skip((paginaValida - 1) * tamanhoValido)
            .Take(tamanhoValido)
            .ToListAsync(ct);

        var itens = new List<ClienteResumoDto>();
        foreach (var usuario in usuarios)
        {
            var papeis = await gerenciadorUsuarios.GetRolesAsync(usuario);
            itens.Add(new ClienteResumoDto(usuario.Id, usuario.Nome, usuario.Email ?? string.Empty, papeis.ToList()));
        }

        return new ResultadoPaginado<ClienteResumoDto>(itens, paginaValida, tamanhoValido, totalItens);
    }

    public async Task<ClienteResumoDto?> ObterPorIdAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await gerenciadorUsuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return null;
        }

        var papeis = await gerenciadorUsuarios.GetRolesAsync(usuario);
        return new ClienteResumoDto(usuario.Id, usuario.Nome, usuario.Email ?? string.Empty, papeis.ToList());
    }
}
