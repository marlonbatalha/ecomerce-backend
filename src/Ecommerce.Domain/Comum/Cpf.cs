namespace Ecommerce.Domain.Comum;

/// <summary>
/// Validação e normalização de CPF (só os 11 dígitos, sem pontuação). Confere os dois dígitos
/// verificadores e recusa sequências repetidas ("111.111.111-11"), que passam na conta mas não
/// existem. Exigido pelo gateway para emitir Pix e boleto.
/// </summary>
public static class Cpf
{
    public const int Tamanho = 11;

    /// <summary>Devolve os 11 dígitos, ou lança <see cref="ExcecaoDeDominio"/> se o CPF for inválido.</summary>
    public static string Normalizar(string? cpf)
    {
        var digitos = new string((cpf ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (!DigitosValidos(digitos))
        {
            throw new ExcecaoDeDominio("CPF inválido.");
        }

        return digitos;
    }

    public static bool EhValido(string? cpf) =>
        DigitosValidos(new string((cpf ?? string.Empty).Where(char.IsAsciiDigit).ToArray()));

    private static bool DigitosValidos(string digitos)
    {
        if (digitos.Length != Tamanho || digitos.All(d => d == digitos[0]))
        {
            return false;
        }

        return digitos[9] - '0' == DigitoVerificador(digitos, 9)
            && digitos[10] - '0' == DigitoVerificador(digitos, 10);
    }

    /// <summary>Módulo 11 sobre os <paramref name="quantidade"/> primeiros dígitos, pesos decrescentes.</summary>
    private static int DigitoVerificador(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
        {
            soma += (digitos[i] - '0') * (quantidade + 1 - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
