using Ecommerce.Domain.Vendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoCupom : IEntityTypeConfiguration<Cupom>
{
    public void Configure(EntityTypeBuilder<Cupom> builder)
    {
        builder.ToTable("VND_Cupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Codigo)
            .HasMaxLength(Cupom.TamanhoMaximoCodigo)
            .IsRequired();

        builder.HasIndex(c => c.Codigo).IsUnique();

        builder.Property(c => c.Descricao)
            .HasMaxLength(Cupom.TamanhoMaximoDescricao)
            .ComStringVaziaComoNulo();

        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);

        builder.Property(c => c.Valor).HasPrecision(18, 2);

        builder.Property(c => c.ValorMinimoDoPedido).HasPrecision(18, 2);

        // Mesmo raciocínio do estoque da variante: dois checkouts disputando o último uso de um
        // cupom limitado — um deles falha com DbUpdateConcurrencyException (ver ServicoDePedidos).
        builder.Property(c => c.QuantidadeDeUsos).IsConcurrencyToken();
    }
}
