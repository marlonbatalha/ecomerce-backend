using Ecommerce.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoVarianteDeProduto : IEntityTypeConfiguration<VarianteDeProduto>
{
    public void Configure(EntityTypeBuilder<VarianteDeProduto> builder)
    {
        builder.ToTable("VariantesDeProduto");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Nome)
            .HasMaxLength(VarianteDeProduto.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(v => v.Sku)
            .HasMaxLength(VarianteDeProduto.TamanhoMaximoSku)
            .IsRequired();

        builder.Property(v => v.Preco).HasPrecision(18, 2);

        builder.HasIndex(v => v.Sku).IsUnique();

        builder.HasIndex(v => v.ProdutoId);
    }
}
