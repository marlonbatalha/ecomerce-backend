using Ecommerce.Domain.Vendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoItemDoCarrinho : IEntityTypeConfiguration<ItemDoCarrinho>
{
    public void Configure(EntityTypeBuilder<ItemDoCarrinho> builder)
    {
        builder.ToTable("ItensDoCarrinho");

        builder.HasKey(i => i.Id);

        builder.HasIndex(i => i.CarrinhoId);

        // Sem navigation pra VarianteDeProduto (agregados diferentes) — FK só de banco, sem
        // relacionamento no EF (checagem de existência é feita no ServicoCarrinho).
    }
}
