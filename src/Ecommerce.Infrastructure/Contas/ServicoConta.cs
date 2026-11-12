using Ecommerce.Application.Contas;
using Ecommerce.Domain.Comum;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Contas;

public class ServicoConta(UserManager<Usuario> gerenciadorUsuarios) : IServicoConta
{
    public async Task<PerfilDto?> ObterPerfilAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await gerenciadorUsuarios.FindByIdAsync(usuarioId.ToString());
        return usuario is null ? null : MapearParaDto(usuario);
    }

    public async Task<PerfilDto> AtualizarPerfilAsync(AtualizarPerfilComando comando, CancellationToken ct = default)
    {
        var usuario = await gerenciadorUsuarios.FindByIdAsync(comando.UsuarioId.ToString())
            ?? throw new ExcecaoDeDominio("Usuário não encontrado.");

        if (string.IsNullOrWhiteSpace(comando.Nome))
        {
            throw new ExcecaoDeDominio("O nome é obrigatório.");
        }

        usuario.Nome = comando.Nome.Trim();

        if (comando.Cpf is not null)
        {
            var cpf = Cpf.Normalizar(comando.Cpf);
            if (cpf != usuario.Cpf)
            {
                var emUso = await gerenciadorUsuarios.Users.AnyAsync(u => u.Cpf == cpf && u.Id != usuario.Id, ct);
                if (emUso)
                {
                    throw new ExcecaoDeDominio("Este CPF já está cadastrado em outra conta.");
                }

                usuario.Cpf = cpf;
            }
        }

        var resultado = await gerenciadorUsuarios.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            throw new ExcecaoDeDominio(DescreverErros(resultado));
        }

        return MapearParaDto(usuario);
    }

    public async Task TrocarSenhaAsync(TrocarSenhaComando comando, CancellationToken ct = default)
    {
        var usuario = await gerenciadorUsuarios.FindByIdAsync(comando.UsuarioId.ToString())
            ?? throw new ExcecaoDeDominio("Usuário não encontrado.");

        var resultado = await gerenciadorUsuarios.ChangePasswordAsync(usuario, comando.SenhaAtual, comando.NovaSenha);
        if (!resultado.Succeeded)
        {
            throw new ExcecaoDeDominio(DescreverErros(resultado));
        }
    }

    private static PerfilDto MapearParaDto(Usuario usuario) => new(usuario.Id, usuario.Nome, usuario.Email ?? string.Empty, usuario.Cpf);

    private static string DescreverErros(IdentityResult resultado) =>
        string.Join(" ", resultado.Errors.Select(e => e.Description));
}
