using Ecommerce.Domain.Catalogo;
using Ecommerce.Domain.Contas;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoFavorito : IEntityTypeConfiguration<Favorito>
{
    public void Configure(EntityTypeBuilder<Favorito> builder)
    {
        builder.ToTable("CTA_Favoritos");

        builder.HasKey(f => f.Id);

        // Único: um produto aparece uma vez na lista de cada usuário. Começa por UsuarioId, então
        // também atende a listagem (WHERE UsuarioId = :id) sem índice extra.
        builder.HasIndex(f => new { f.UsuarioId, f.ProdutoId }).IsUnique();

        // Pra cascata da FK de produto não varrer a tabela.
        builder.HasIndex(f => f.ProdutoId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(f => f.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Produto>()
            .WithMany()
            .HasForeignKey(f => f.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
