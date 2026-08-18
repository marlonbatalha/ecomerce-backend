using Ecommerce.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;

namespace Ecommerce.Api.Middlewares;

/// <summary>
/// Converte qualquer <see cref="ExcecaoDeDominio"/> não tratada em 400 Bad Request — evita repetir
/// try/catch em cada controller pra regra de negócio violada (SKU duplicado, nome inválido, etc.).
/// Exceções de outro tipo não são tratadas aqui (retorna false, cai no handler padrão -> 500).
/// </summary>
public class ManipuladorDeExcecoesDeDominio(ILogger<ManipuladorDeExcecoesDeDominio> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        if (excecao is not ExcecaoDeDominio excecaoDeDominio)
        {
            return false;
        }

        logger.LogWarning(excecaoDeDominio, "Regra de domínio violada em {Caminho}", contexto.Request.Path);

        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        await contexto.Response.WriteAsJsonAsync(new { erro = excecaoDeDominio.Message }, ct);

        return true;
    }
}
