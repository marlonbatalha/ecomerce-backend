using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ecommerce.Domain.Comum;

/// <summary>
/// Normalizações de texto compartilhadas: remoção de acento (usada por <see cref="GeradorDeSlug"/>)
/// e forma "de busca" (maiúsculo, sem acento, espaços colapsados). A forma de busca existe porque
/// <c>LIKE</c> no Oracle diferencia maiúsculas/minúsculas e acentos — guardar o nome já normalizado
/// numa coluna própria e normalizar o termo do mesmo jeito faz "camisa" achar "Camisá Polo".
/// </summary>
public static class NormalizadorDeTexto
{
    public static string ParaBusca(string texto) =>
        Regex.Replace(RemoverAcentos(texto.Trim().ToUpperInvariant()), @"\s+", " ");

    public static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
