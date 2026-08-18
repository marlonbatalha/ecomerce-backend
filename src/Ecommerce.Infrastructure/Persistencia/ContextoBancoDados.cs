using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Identidade;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Persistencia;

/// <summary>
/// Contexto principal da aplicação (EF Core). Estende <see cref="IdentityDbContext{TUser,TRole,TKey}"/>
/// pra incluir as tabelas do ASP.NET Core Identity (usuários, roles, claims) junto com o resto do
/// domínio. Entidades e configurações (IEntityTypeConfiguration) são adicionadas por agregado
/// conforme o domínio for implementado (ver Fase 2+ do docs/plano-ecommerce.md — Catálogo,
/// Carrinho/Pedidos, etc.).
/// </summary>
public class ContextoBancoDados(DbContextOptions<ContextoBancoDados> opcoes)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(opcoes)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<TokenAtualizacao> TokensAtualizacao => Set<TokenAtualizacao>();

    protected override void OnModelCreating(ModelBuilder construtorDeModelo)
    {
        base.OnModelCreating(construtorDeModelo);

        construtorDeModelo.ApplyConfigurationsFromAssembly(typeof(ContextoBancoDados).Assembly);
    }
}
