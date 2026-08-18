using Ecommerce.Domain.Vendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoItemDoPedido : IEntityTypeConfiguration<ItemDoPedido>
{
    public void Configure(EntityTypeBuilder<ItemDoPedido> builder)
    {
        builder.ToTable("ItensDoPedido");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.NomeProduto)
            .HasMaxLength(ItemDoPedido.TamanhoMaximoNomeProduto)
            .IsRequired();

        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);

        builder.HasIndex(i => i.PedidoId);

        builder.Ignore(i => i.Subtotal);
    }
}
