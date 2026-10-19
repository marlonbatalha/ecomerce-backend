using Ecommerce.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoProduto : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("CAT_Produtos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome)
            .HasMaxLength(Produto.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(p => p.NomeParaBusca)
            .HasMaxLength(Produto.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(Produto.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(p => p.Descricao)
            .HasMaxLength(Produto.TamanhoMaximoDescricao)
            .ComStringVaziaComoNulo();

        builder.Property(p => p.Sku)
            .HasMaxLength(Produto.TamanhoMaximoSku)
            .IsRequired();

        builder.Property(p => p.Preco).HasPrecision(18, 2);

        builder.HasIndex(p => p.Sku).IsUnique();

        builder.HasIndex(p => p.Slug).IsUnique();

        // Índices da busca pública (ServicoProdutos.BuscarAsync): (CategoriaId, Preco) atende o
        // filtro por categoria com/sem faixa de preço e também serve de índice da FK (coluna
        // líder); (Ativo, CriadoEmUtc) atende a ordenação padrão "mais recente" sem full scan.
        // NomeParaBusca fica sem índice de propósito: a busca é LIKE '%termo%', que B-tree não
        // acelera — o caminho pra O(log n) ali é Oracle Text (ver plano).
        builder.HasIndex(p => new { p.CategoriaId, p.Preco });

        builder.HasIndex(p => new { p.Ativo, p.CriadoEmUtc });

        // Sem navigation property pra Categoria (agregados diferentes) — FK só de banco.
        // Restrict: não deixa apagar uma categoria que ainda tem produto (mesma lógica do
        // auto-relacionamento de Categoria — remoção em cascata tem que ser explícita).
        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Variantes/Imagens pertencem ao agregado Produto — cascade aqui é o comportamento
        // correto (apagar o produto apaga suas variantes/imagens junto).
        builder.HasMany(p => p.Variantes)
            .WithOne()
            .HasForeignKey(v => v.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Imagens)
            .WithOne()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Variantes/Imagens são expostas só como IReadOnlyCollection (sem setter público) — o EF
        // precisa ler/escrever direto no campo privado que apoia cada coleção.
        builder.Metadata.FindNavigation(nameof(Produto.Variantes))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Produto.Imagens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
