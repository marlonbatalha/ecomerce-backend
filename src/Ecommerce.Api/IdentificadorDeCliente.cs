using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Api;

/// <summary>
/// Decide quem é "o cliente" pro rate limiting. O frontend (Next.js) chama a API só pelo servidor
/// dele — sem isso, toda requisição teria o IP de saída da Vercel e um limite "por IP" viraria um
/// limite único pra loja inteira. O servidor do Next se identifica com uma chave compartilhada
/// (<see cref="CabecalhoChave"/>, config <c>Frontend:ChaveDeConfianca</c>) e repassa o IP do
/// visitante em <see cref="CabecalhoIp"/>. O header de IP só vale quando a chave confere: sem
/// isso, qualquer um forjaria um IP novo a cada requisição e nunca bateria no limite.
/// </summary>
public static class IdentificadorDeCliente
{
    public const string CabecalhoChave = "X-Frontend-Chave";
    public const string CabecalhoIp = "X-Cliente-IP";

    /// <returns>
    /// Chave de partição do cliente, ou <c>null</c> quando a requisição é do próprio servidor do
    /// frontend sem visitante associado (ISR, build, revalidação) — tráfego confiável, sem limite.
    /// </returns>
    public static string? Identificar(HttpContext contexto, string? chaveDeConfianca)
    {
        if (ChaveConfere(contexto, chaveDeConfianca))
        {
            var ipDoVisitante = contexto.Request.Headers[CabecalhoIp].ToString();
            return IPAddress.TryParse(ipDoVisitante, out var ip) ? ip.ToString() : null;
        }

        // Já é o IP real do cliente quando há proxy reverso configurado (UseForwardedHeaders).
        return contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    }

    private static bool ChaveConfere(HttpContext contexto, string? chaveDeConfianca)
    {
        if (string.IsNullOrEmpty(chaveDeConfianca))
        {
            return false;
        }

        var chaveRecebida = contexto.Request.Headers[CabecalhoChave].ToString();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(chaveRecebida),
            Encoding.UTF8.GetBytes(chaveDeConfianca));
    }
}
