using Ecommerce.Application.Catalogo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Catalogo;

/// <summary>
/// Salva a imagem em disco, dentro de <c>wwwroot/uploads/produtos</c> (servido por
/// <c>app.UseStaticFiles()</c>, ver <c>Program.cs</c>) — implementação de dev/staging enquanto o
/// Object Storage OCI não é provisionado (deferido, ver decisão de sequenciamento no plano).
/// `Storage:UrlPublica` (appsettings) define o host usado pra montar a URL absoluta devolvida —
/// precisa ser absoluta porque quem consome (o browser, via `&lt;img&gt;`/`next/image` no
/// frontend) busca a imagem direto, sem passar pela API própria.
/// </summary>
public class ArmazenamentoDeImagensLocal(IWebHostEnvironment ambiente, IConfiguration configuracao) : IArmazenamentoDeImagens
{
    private const string PastaRelativa = "uploads/produtos";

    public async Task<string> SalvarAsync(Stream conteudo, string extensaoDeArquivo, CancellationToken ct = default)
    {
        // WebRootPath vem null se wwwroot/ ainda não existir no disco no momento em que o host
        // sobe (comum: a pasta só passa a existir depois do primeiro upload) — ContentRootPath
        // sempre existe, então monta o caminho a partir dele em vez de depender de WebRootPath.
        var pastaAbsoluta = Path.Combine(ambiente.ContentRootPath, "wwwroot", PastaRelativa);
        Directory.CreateDirectory(pastaAbsoluta);

        var nomeArquivo = $"{Guid.NewGuid()}{extensaoDeArquivo}";
        var caminhoAbsoluto = Path.Combine(pastaAbsoluta, nomeArquivo);

        await using (var arquivo = File.Create(caminhoAbsoluto))
        {
            await conteudo.CopyToAsync(arquivo, ct);
        }

        var urlPublicaBase = configuracao["Storage:UrlPublica"]
            ?? throw new InvalidOperationException("Storage:UrlPublica não configurada.");

        return $"{urlPublicaBase.TrimEnd('/')}/{PastaRelativa}/{nomeArquivo}";
    }
}
