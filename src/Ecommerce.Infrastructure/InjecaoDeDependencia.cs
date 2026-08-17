using Ecommerce.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;

public static class InjecaoDeDependencia
{
    /// <summary>
    /// Registra os serviços da camada de Infrastructure (persistência, integrações externas).
    /// A connection string vem de configuração — em produção/staging, via variável de ambiente
    /// (ex.: ConnectionStrings__Default) apontando para o Oracle Wallet, nunca commitada.
    /// </summary>
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var stringConexao = configuracao.GetConnectionString("Default");

        servicos.AddDbContext<ContextoBancoDados>(opcoes =>
        {
            if (string.IsNullOrWhiteSpace(stringConexao))
            {
                // Sem connection string configurada (ex.: primeiro build local): registra o
                // DbContext mesmo assim para não quebrar o DI, mas qualquer uso real falhará
                // até ConnectionStrings:Default ser definido (appsettings.Development.json,
                // variável de ambiente ou secret).
                return;
            }

            opcoes.UseOracle(stringConexao);
        });

        return servicos;
    }
}
