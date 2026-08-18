using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Identidade;
using Ecommerce.Domain.Vendas;
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

    // VarianteDeProduto/ImagemDeProduto não têm DbSet próprio de propósito — pertencem ao
    // agregado Produto (ver ConfiguracaoProduto) e são descobertas pelo EF via as navigations
    // Produto.Variantes/Produto.Imagens; acesso direto quando necessário (ex.: checar SKU
    // duplicado) é via Set<T>().
    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Carrinho> Carrinhos => Set<Carrinho>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    protected override void OnModelCreating(ModelBuilder construtorDeModelo)
    {
        base.OnModelCreating(construtorDeModelo);

        construtorDeModelo.ApplyConfigurationsFromAssembly(typeof(ContextoBancoDados).Assembly);
    }
}
