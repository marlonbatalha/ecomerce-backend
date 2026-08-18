using Ecommerce.Domain.Identidade;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoTokenAtualizacao : IEntityTypeConfiguration<TokenAtualizacao>
{
    public void Configure(EntityTypeBuilder<TokenAtualizacao> builder)
    {
        builder.ToTable("TokensAtualizacao");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.HashToken)
            .HasMaxLength(200)
            .IsRequired();

        // Consulta de refresh token é sempre por hash — precisa ser único e indexado.
        builder.HasIndex(t => t.HashToken).IsUnique();

        builder.HasIndex(t => t.UsuarioId);

        builder.Property(t => t.SubstituidoPorHashToken).HasMaxLength(200);

        // Sem navigation property pro Usuario (Domain não conhece o tipo de Infrastructure) —
        // a FK é só de banco, aplicada via HasOne<T> sem propriedade de navegação inversa.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
