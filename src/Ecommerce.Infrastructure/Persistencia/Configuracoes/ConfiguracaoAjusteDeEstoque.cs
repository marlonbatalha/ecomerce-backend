using Ecommerce.Domain.Catalogo;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoAjusteDeEstoque : IEntityTypeConfiguration<AjusteDeEstoque>
{
    public void Configure(EntityTypeBuilder<AjusteDeEstoque> builder)
    {
        builder.ToTable("AjustesDeEstoque");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Motivo).HasMaxLength(AjusteDeEstoque.TamanhoMaximoMotivo).IsRequired();

        builder.HasIndex(a => a.VarianteDeProdutoId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
