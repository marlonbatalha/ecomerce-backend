using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Comum;
using Ecommerce.Domain.Contas;
using Ecommerce.Domain.Estoque;
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

    public DbSet<AjusteDeEstoque> AjustesDeEstoque => Set<AjusteDeEstoque>();

    public DbSet<Carrinho> Carrinhos => Set<Carrinho>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    public DbSet<Cupom> Cupons => Set<Cupom>();

    public DbSet<Endereco> Enderecos => Set<Endereco>();

    public DbSet<Favorito> Favoritos => Set<Favorito>();

    protected override void OnModelCreating(ModelBuilder construtorDeModelo)
    {
        base.OnModelCreating(construtorDeModelo);

        // O ASP.NET Core Identity mapeia suas próprias tabelas e colunas (AspNetUsers.UserName,
        // AspNetUserRoles.RoleId, ...) em inglês por padrão — sobrescreve tabela E coluna aqui pra
        // seguir o mesmo padrão do resto do schema (PascalCase em português, ver ConfiguracaoCategoria
        // e demais em Persistencia/Configuracoes). Prefixo "IDN_" (Identidade) segue a mesma convenção
        // de todas as tabelas do schema — ver nota de organização por contexto (CAT_/CTA_/EST_/IDN_/VND_)
        // no plano do projeto. Só a coluna do banco muda (HasColumnName) — a propriedade C# continua
        // com o nome original (UserName, Email, ...) porque pertence às classes base do Identity
        // (IdentityUser<TKey> etc.), que a API do próprio Identity (UserManager, SignInManager, ...)
        // espera encontrar; "Id" fica como "Id" em todas, igual ao resto do schema.
        construtorDeModelo.Entity<Usuario>(b =>
        {
            b.ToTable("IDN_Usuarios");
            b.Property(u => u.UserName).HasColumnName("NomeDeUsuario");
            b.Property(u => u.NormalizedUserName).HasColumnName("NomeDeUsuarioNormalizado");
            b.Property(u => u.NormalizedEmail).HasColumnName("EmailNormalizado");
            b.Property(u => u.EmailConfirmed).HasColumnName("EmailConfirmado");
            b.Property(u => u.PasswordHash).HasColumnName("HashDaSenha");
            b.Property(u => u.SecurityStamp).HasColumnName("CarimboDeSeguranca");
            b.Property(u => u.ConcurrencyStamp).HasColumnName("CarimboDeConcorrencia");
            b.Property(u => u.PhoneNumber).HasColumnName("Telefone");
            b.Property(u => u.PhoneNumberConfirmed).HasColumnName("TelefoneConfirmado");
            b.Property(u => u.TwoFactorEnabled).HasColumnName("AutenticacaoDoisFatoresAtiva");
            b.Property(u => u.LockoutEnd).HasColumnName("BloqueadoAte");
            b.Property(u => u.LockoutEnabled).HasColumnName("BloqueioAtivo");
            b.Property(u => u.AccessFailedCount).HasColumnName("TentativasDeAcessoFalhas");

            b.Property(u => u.Cpf).HasMaxLength(Cpf.Tamanho);
            // Índice único no Oracle ignora linhas com a coluna nula: várias contas sem CPF convivem.
            // Sem filtro (o EF põe um "IS NOT NULL" por convenção, e o Oracle não aceita índice com WHERE).
            b.HasIndex(u => u.Cpf).IsUnique().HasFilter(null);
        });

        construtorDeModelo.Entity<IdentityRole<Guid>>(b =>
        {
            b.ToTable("IDN_Papeis");
            b.Property(r => r.Name).HasColumnName("Nome");
            b.Property(r => r.NormalizedName).HasColumnName("NomeNormalizado");
            b.Property(r => r.ConcurrencyStamp).HasColumnName("CarimboDeConcorrencia");
        });

        construtorDeModelo.Entity<IdentityUserRole<Guid>>(b =>
        {
            b.ToTable("IDN_UsuariosPapeis");
            b.Property(ur => ur.UserId).HasColumnName("UsuarioId");
            b.Property(ur => ur.RoleId).HasColumnName("PapelId");
        });

        construtorDeModelo.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("IDN_ClaimsDeUsuario");
            b.Property(c => c.UserId).HasColumnName("UsuarioId");
            b.Property(c => c.ClaimType).HasColumnName("TipoDeClaim");
            b.Property(c => c.ClaimValue).HasColumnName("ValorDaClaim");
        });

        construtorDeModelo.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("IDN_ClaimsDePapel");
            b.Property(c => c.RoleId).HasColumnName("PapelId");
            b.Property(c => c.ClaimType).HasColumnName("TipoDeClaim");
            b.Property(c => c.ClaimValue).HasColumnName("ValorDaClaim");
        });

        construtorDeModelo.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("IDN_LoginsDeUsuario");
            b.Property(l => l.LoginProvider).HasColumnName("ProvedorDeLogin");
            b.Property(l => l.ProviderKey).HasColumnName("ChaveDoProvedor");
            b.Property(l => l.ProviderDisplayName).HasColumnName("NomeDeExibicaoDoProvedor");
            b.Property(l => l.UserId).HasColumnName("UsuarioId");
        });

        construtorDeModelo.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("IDN_TokensDeUsuario");
            b.Property(t => t.UserId).HasColumnName("UsuarioId");
            b.Property(t => t.LoginProvider).HasColumnName("ProvedorDeLogin");
            b.Property(t => t.Name).HasColumnName("Nome");
            b.Property(t => t.Value).HasColumnName("Valor");
        });

        construtorDeModelo.ApplyConfigurationsFromAssembly(typeof(ContextoBancoDados).Assembly);
    }
}
