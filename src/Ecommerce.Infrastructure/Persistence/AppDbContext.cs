using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Persistence;

/// <summary>
/// DbContext principal da aplicação. Entidades e configurações (IEntityTypeConfiguration)
/// serão adicionadas por agregado conforme o domínio for implementado (ver Fase 2+ do
/// docs/plano-ecommerce.md — Catálogo, Carrinho/Pedidos, etc.).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
