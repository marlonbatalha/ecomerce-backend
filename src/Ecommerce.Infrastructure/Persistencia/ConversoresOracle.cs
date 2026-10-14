using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ecommerce.Infrastructure.Persistencia;

public static class ConversoresOracle
{
    /// <summary>
    /// No Oracle, string vazia É <c>NULL</c> — gravar "" numa coluna NOT NULL dá ORA-01400. Pra
    /// textos opcionais que o domínio representa como "" (nunca null), a coluna fica anulável e o
    /// conversor traduz "" ⇄ NULL nos dois sentidos, então o domínio continua vendo só "".
    /// </summary>
    public static PropertyBuilder<string> ComStringVaziaComoNulo(this PropertyBuilder<string> propriedade) =>
        propriedade
            .IsRequired(false)
            .HasConversion(new ValueConverter<string, string?>(
                valor => valor.Length == 0 ? null : valor,
                valor => valor ?? string.Empty,
                convertsNulls: true));
}
