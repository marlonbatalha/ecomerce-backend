using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Ecommerce.Infrastructure.Persistencia;

/// <summary>
/// Configuração do provider Oracle compartilhada entre runtime (<c>InjecaoDeDependencia</c>) e
/// design-time (<see cref="ContextoBancoDadosFactory"/>) — garante que as migrations geradas e o
/// banco real de fato usem o mesmo dialeto SQL.
/// </summary>
public static class ConfiguracaoOracle
{
    public static void Aplicar(DbContextOptionsBuilder builder, string connectionString)
    {
        builder.UseOracle(connectionString, oracle =>
        {
            // Sem isso, o provider Oracle EF Core 10.x assume Oracle Database 23ai por padrão
            // (ex.: gera coluna BOOLEAN nativa pro Identity). O ambiente local
            // (docker/docker-compose.yml, Oracle XE 21c) não suporta isso, e a versão real do
            // Autonomous Database ainda não foi decidida (ver plano, seção 8) — força 19c, que é
            // compatível com 19c/21c/23c.
            oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
        });
    }
}
