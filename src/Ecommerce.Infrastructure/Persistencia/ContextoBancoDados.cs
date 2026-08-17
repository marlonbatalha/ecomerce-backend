using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Persistencia;

/// <summary>
/// Contexto principal da aplicação (EF Core). Entidades e configurações (IEntityTypeConfiguration)
/// serão adicionadas por agregado conforme o domínio for implementado (ver Fase 2+ do
/// docs/plano-ecommerce.md — Catálogo, Carrinho/Pedidos, etc.).
/// </summary>
public class ContextoBancoDados(DbContextOptions<ContextoBancoDados> opcoes) : DbContext(opcoes)
{
    protected override void OnModelCreating(ModelBuilder construtorDeModelo)
    {
        base.OnModelCreating(construtorDeModelo);

        construtorDeModelo.ApplyConfigurationsFromAssembly(typeof(ContextoBancoDados).Assembly);
    }
}
