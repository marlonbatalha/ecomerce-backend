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

        // (UsuarioId, CriadoEmUtc): "meus pedidos" já sai ordenado do índice (e cobre a FK).
        // (Status, CriadoEmUtc) e (CriadoEmUtc): listagem admin com/sem filtro de status, sem
        // full scan + sort — o primeiro gargalo que apareceria com volume de pedidos.
        builder.HasIndex(p => new { p.UsuarioId, p.CriadoEmUtc });

        builder.HasIndex(p => new { p.Status, p.CriadoEmUtc });

        builder.HasIndex(p => p.CriadoEmUtc);

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

        // Colunas na própria VND_Pedidos (owned) — é uma cópia do endereço, não uma relação.
        // Opcional no banco só por causa dos pedidos anteriores a esta mudança.
        builder.OwnsOne(p => p.Entrega, entrega =>
        {
            entrega.Property(e => e.Logradouro).HasColumnName("EntregaLogradouro").HasMaxLength(200);
            entrega.Property(e => e.Numero).HasColumnName("EntregaNumero").HasMaxLength(20);
            entrega.Property(e => e.Complemento).HasColumnName("EntregaComplemento").HasMaxLength(100);
            entrega.Property(e => e.Bairro).HasColumnName("EntregaBairro").HasMaxLength(100);
            entrega.Property(e => e.Cidade).HasColumnName("EntregaCidade").HasMaxLength(100);
            entrega.Property(e => e.Estado).HasColumnName("EntregaEstado").HasMaxLength(2);
            entrega.Property(e => e.Cep).HasColumnName("EntregaCep").HasMaxLength(9);
        });

        builder.Property(p => p.MetodoDeEnvio).HasMaxLength(50);

        builder.Property(p => p.ValorFrete).HasPrecision(18, 2);

        // Subtotal/Total são calculados (itens − desconto + frete) — não persistidos.
        builder.Ignore(p => p.Subtotal);
        builder.Ignore(p => p.Total);
    }
}
