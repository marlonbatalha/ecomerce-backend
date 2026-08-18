using System.Linq;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Infrastructure.Identidade;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests;

/// <summary>
/// Troca o <see cref="ContextoBancoDados"/> pra usar o provider EF Core InMemory em vez do
/// Oracle configurado em <c>InjecaoDeDependencia</c> — permite testar o pipeline HTTP completo
/// (Program.cs, DI, controllers, Identity/JWT) sem depender de Docker/Oracle disponível
/// (ver [[docker-indisponivel-ambiente-dev]] — a migration real contra Oracle é validada à parte).
/// </summary>
public class FabricaDeAplicacaoTeste : WebApplicationFactory<Program>
{
    private readonly string _nomeBancoEmMemoria = $"testes-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicos =>
        {
            var descritor = servicos.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ContextoBancoDados>));
            if (descritor is not null)
            {
                servicos.Remove(descritor);
            }

            servicos.AddDbContext<ContextoBancoDados>(opcoes => opcoes.UseInMemoryDatabase(_nomeBancoEmMemoria));
        });
    }

    /// <summary>
    /// Cria um usuário com a role Admin direto via Identity (sem passar pelo endpoint público de
    /// registro, que sempre atribui "cliente") — útil pra testar endpoints admin-only. O teste
    /// ainda precisa logar via <c>/api/v1/auth/login</c> pra obter um JWT válido com a claim de role.
    /// </summary>
    public async Task CriarUsuarioAdminAsync(string email, string senha)
    {
        using var escopo = Services.CreateScope();
        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = new Usuario { UserName = email, Email = email, Nome = "Admin de Teste" };
        var resultado = await gerenciadorUsuarios.CreateAsync(usuario, senha);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        await gerenciadorUsuarios.AddToRoleAsync(usuario, Papeis.Admin);
    }
}
