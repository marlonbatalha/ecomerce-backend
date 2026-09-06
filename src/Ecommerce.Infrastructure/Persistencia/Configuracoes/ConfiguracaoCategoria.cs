using Ecommerce.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento EF Core de <see cref="Categoria"/>. Regras de negócio (nome obrigatório, tamanho
/// máximo, hierarquia) ficam na entidade — aqui só o necessário para persistir corretamente.
/// </summary>
public class ConfiguracaoCategoria : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("CAT_Categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .HasMaxLength(Categoria.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(Categoria.TamanhoMaximoNome)
            .IsRequired();

        // Slug é usado em rotas públicas (ex.: /categorias/{slug}) — precisa ser único.
        builder.HasIndex(c => c.Slug).IsUnique();

        // Auto-relacionamento: uma categoria pode ter uma pai e várias subcategorias.
        // Restrict (em vez de cascade) evita apagar uma árvore inteira por engano ao
        // excluir a categoria raiz — remoção em cascata, se necessária, deve ser explícita
        // no caso de uso, não um efeito colateral do banco.
        builder.HasOne(c => c.CategoriaPai)
            .WithMany(c => c.Subcategorias)
            .HasForeignKey(c => c.CategoriaPaiId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subcategorias é exposta só como IReadOnlyCollection (sem setter público) — o EF
        // precisa ler/escrever direto no campo privado que a apoia.
        builder.Metadata
            .FindNavigation(nameof(Categoria.Subcategorias))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
