using Ecommerce.Domain.Vendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoPagamento : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("VND_Pagamentos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ReferenciaExterna)
            .HasMaxLength(Pagamento.TamanhoMaximoReferenciaExterna)
            .IsRequired();

        // Webhook busca o pagamento pela referência do gateway — precisa ser único e indexado.
        builder.HasIndex(p => p.ReferenciaExterna).IsUnique();

        builder.HasIndex(p => p.PedidoId);

        builder.Property(p => p.UrlPagamento).HasMaxLength(Pagamento.TamanhoMaximoUrlPagamento);

        builder.Property(p => p.IdTransacao).HasMaxLength(Pagamento.TamanhoMaximoReferenciaExterna);

        builder.Property(p => p.Valor).HasPrecision(18, 2);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.CodigoPix).HasMaxLength(Pagamento.TamanhoMaximoCodigoPix);

        builder.Property(p => p.LinhaDigitavel).HasMaxLength(Pagamento.TamanhoMaximoLinhaDigitavel);
    }
}
