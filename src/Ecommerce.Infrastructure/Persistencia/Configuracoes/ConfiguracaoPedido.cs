using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoPedido : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("VND_Pedidos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(p => p.CodigoRastreio).HasMaxLength(50);

        builder.HasIndex(p => p.UsuarioId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Pedido.Itens))!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.CodigoCupom).HasMaxLength(Cupom.TamanhoMaximoCodigo);

        builder.Property(p => p.Desconto).HasPrecision(18, 2);

        // Subtotal/Total são calculados (soma dos itens, menos o desconto) — não persistidos.
        builder.Ignore(p => p.Subtotal);
        builder.Ignore(p => p.Total);
    }
}
