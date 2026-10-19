using System.Text.RegularExpressions;

namespace Ecommerce.Domain.Comum;

/// <summary>
/// Gera slugs (minúsculo, sem acento, palavras separadas por hífen) a partir de um texto — usado
/// por qualquer entidade que precise de identificador amigável de URL (<c>Categoria</c>,
/// <c>Produto</c>). Extraído de <c>Categoria</c> (era privado e duplicado ali) quando
/// <c>Produto</c> ganhou <c>Slug</c> também.
/// </summary>
public static class GeradorDeSlug
{
    public static string Gerar(string texto)
    {
        var semAcentos = NormalizadorDeTexto.RemoverAcentos(texto.Trim().ToLowerInvariant());
        return Regex.Replace(semAcentos, "[^a-z0-9]+", "-").Trim('-');
    }
}
