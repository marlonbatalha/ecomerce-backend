using Ecommerce.Domain.Vendas;
using Ecommerce.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoCarrinho : IEntityTypeConfiguration<Carrinho>
{
    public void Configure(EntityTypeBuilder<Carrinho> builder)
    {
        builder.ToTable("VND_Carrinhos");

        builder.HasKey(c => c.Id);

        // Um usuário/sessão tem no máximo um carrinho ativo por vez.
        builder.HasIndex(c => c.UsuarioId).IsUnique().HasFilter(FiltroIndiceParcial("UsuarioId"));
        builder.HasIndex(c => c.IdSessao).IsUnique().HasFilter(FiltroIndiceParcial("IdSessao"));

        builder.Property(c => c.IdSessao).HasMaxLength(64);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Itens)
            .WithOne()
            .HasForeignKey(i => i.CarrinhoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Carrinho.Itens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    // Oracle usa aspas duplas pra identificador de coluna com case sensitivo (convenção do provider).
    private static string FiltroIndiceParcial(string coluna) => $"\"{coluna}\" IS NOT NULL";
}
