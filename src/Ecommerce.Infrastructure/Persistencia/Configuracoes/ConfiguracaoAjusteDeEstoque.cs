using Ecommerce.Domain.Estoque;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoAjusteDeEstoque : IEntityTypeConfiguration<AjusteDeEstoque>
{
    public void Configure(EntityTypeBuilder<AjusteDeEstoque> builder)
    {
        builder.ToTable("EST_AjustesDeEstoque");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Motivo).HasMaxLength(AjusteDeEstoque.TamanhoMaximoMotivo).IsRequired();

        // Histórico de uma variante sai já ordenado por data do índice.
        builder.HasIndex(a => new { a.VarianteDeProdutoId, a.CriadoEmUtc });

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
