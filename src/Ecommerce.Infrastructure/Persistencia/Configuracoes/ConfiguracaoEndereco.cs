using Ecommerce.Domain.Contas;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoEndereco : IEntityTypeConfiguration<Endereco>
{
    public void Configure(EntityTypeBuilder<Endereco> builder)
    {
        builder.ToTable("CTA_Enderecos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Rotulo).HasMaxLength(Endereco.TamanhoMaximoRotulo).IsRequired();
        builder.Property(e => e.Logradouro).HasMaxLength(Endereco.TamanhoMaximoLogradouro).IsRequired();
        builder.Property(e => e.Numero).HasMaxLength(Endereco.TamanhoMaximoNumero).IsRequired();
        builder.Property(e => e.Complemento).HasMaxLength(Endereco.TamanhoMaximoComplemento);
        builder.Property(e => e.Bairro).HasMaxLength(Endereco.TamanhoMaximoBairro).IsRequired();
        builder.Property(e => e.Cidade).HasMaxLength(Endereco.TamanhoMaximoCidade).IsRequired();
        builder.Property(e => e.Estado).HasMaxLength(Endereco.TamanhoEstado).IsRequired();
        builder.Property(e => e.Cep).HasMaxLength(9).IsRequired();

        builder.HasIndex(e => e.UsuarioId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
