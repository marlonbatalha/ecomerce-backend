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

        // Token de concorrência otimista: o UPDATE do estoque só afeta a linha se o valor não
        // mudou desde a leitura — duas requisições de checkout concorrentes debitando a mesma
        // variante, uma delas falha com DbUpdateConcurrencyException em vez de gerar overselling
        // (ver ServicoDePedidos, seção "Fase 3" do plano).
        builder.Property(v => v.Estoque).IsConcurrencyToken();
    }
}
