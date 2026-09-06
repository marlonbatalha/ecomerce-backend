using Ecommerce.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoImagemDeProduto : IEntityTypeConfiguration<ImagemDeProduto>
{
    public void Configure(EntityTypeBuilder<ImagemDeProduto> builder)
    {
        builder.ToTable("CAT_ImagensDeProduto");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url)
            .HasMaxLength(ImagemDeProduto.TamanhoMaximoUrl)
            .IsRequired();

        builder.HasIndex(i => i.ProdutoId);
    }
}
