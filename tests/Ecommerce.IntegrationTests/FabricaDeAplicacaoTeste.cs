using System.Linq;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
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
}
