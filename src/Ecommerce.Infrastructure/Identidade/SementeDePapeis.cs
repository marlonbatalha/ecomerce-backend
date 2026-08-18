using Ecommerce.Application.Autenticacao;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure.Identidade;

/// <summary>
/// Garante que as roles do Identity (<see cref="Papeis.Todos"/>) existam no banco. Chamada uma
/// vez na inicialização da aplicação (ver Program.cs) — sem isso, <c>AddToRoleAsync</c> falha
/// pra usuários novos porque a role ainda não existe.
/// </summary>
public static class SementeDePapeis
{
    public static async Task GarantirPapeisAsync(IServiceProvider servicos)
    {
        using var escopo = servicos.CreateScope();
        var gerenciadorPapeis = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var papel in Papeis.Todos)
        {
            if (!await gerenciadorPapeis.RoleExistsAsync(papel))
            {
                await gerenciadorPapeis.CreateAsync(new IdentityRole<Guid>(papel));
            }
        }
    }
}
