using Ecommerce.Application.Catalogo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Catalogo;

/// <summary>
/// Salva a imagem em disco, dentro de <c>wwwroot/uploads/produtos</c> (servido por
/// <c>app.UseStaticFiles()</c>, ver <c>Program.cs</c>) — implementação de dev; staging/produção
/// usam <see cref="ArmazenamentoDeImagensS3"/> (<c>Storage:Provedor = S3</c>).
/// `Storage:UrlPublica` (appsettings) define o host usado pra montar a URL absoluta devolvida —
/// precisa ser absoluta porque quem consome (o browser, via `&lt;img&gt;`/`next/image` no
/// frontend) busca a imagem direto, sem passar pela API própria.
/// </summary>
public class ArmazenamentoDeImagensLocal(IWebHostEnvironment ambiente, IOptions<OpcoesArmazenamento> opcoes) : IArmazenamentoDeImagens
{
    private const string PastaRelativa = "uploads/produtos";

    public async Task<string> SalvarAsync(Stream conteudo, string extensaoDeArquivo, string tipoConteudo, CancellationToken ct = default)
    {
        var nomeArquivo = $"{Guid.NewGuid()}{extensaoDeArquivo}";

        await using (var arquivo = File.Create(Path.Combine(ObterPastaAbsoluta(), nomeArquivo)))
        {
            await conteudo.CopyToAsync(arquivo, ct);
        }

        return $"{ObterUrlBase()}/{PastaRelativa}/{nomeArquivo}";
    }

    public Task RemoverAsync(string url, CancellationToken ct = default)
    {
        var prefixo = $"{ObterUrlBase()}/{PastaRelativa}/";
        if (!url.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
        {
            // URL de outro storage (ex.: imagem cadastrada antes de trocar de provedor) — não
            // é deste armazenamento, nada a apagar aqui.
            return Task.CompletedTask;
        }

        // Path.GetFileName descarta qualquer "../" — o nome nunca escapa da pasta de uploads.
        var caminho = Path.Combine(ObterPastaAbsoluta(), Path.GetFileName(url[prefixo.Length..]));
        if (File.Exists(caminho))
        {
            File.Delete(caminho);
        }

        return Task.CompletedTask;
    }

    private string ObterPastaAbsoluta()
    {
        // WebRootPath vem null se wwwroot/ ainda não existir no disco no momento em que o host
        // sobe (comum: a pasta só passa a existir depois do primeiro upload) — ContentRootPath
        // sempre existe, então monta o caminho a partir dele em vez de depender de WebRootPath.
        var pasta = Path.Combine(ambiente.ContentRootPath, "wwwroot", PastaRelativa);
        Directory.CreateDirectory(pasta);
        return pasta;
    }

    private string ObterUrlBase()
    {
        var urlBase = opcoes.Value.UrlPublica;
        if (string.IsNullOrWhiteSpace(urlBase))
        {
            throw new InvalidOperationException("Storage:UrlPublica não configurada.");
        }

        return urlBase.TrimEnd('/');
    }
}
