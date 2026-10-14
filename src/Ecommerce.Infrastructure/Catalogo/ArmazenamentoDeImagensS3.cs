using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Ecommerce.Application.Catalogo;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Catalogo;

/// <summary>
/// Object Storage via API S3-compatível — pensado pro OCI Object Storage (seção 1 do plano), mas
/// funciona igual com AWS S3, Cloudflare R2 ou MinIO. O bucket precisa permitir leitura pública
/// dos objetos (ou ter uma CDN na frente): a URL devolvida é buscada direto pelo browser.
/// </summary>
public sealed class ArmazenamentoDeImagensS3 : IArmazenamentoDeImagens, IDisposable
{
    private const string PrefixoChave = "produtos/";

    private readonly AmazonS3Client _cliente;
    private readonly OpcoesArmazenamento _opcoes;

    public ArmazenamentoDeImagensS3(IOptions<OpcoesArmazenamento> opcoes)
    {
        _opcoes = opcoes.Value;
        var s3 = _opcoes.S3;

        var configuracao = new AmazonS3Config
        {
            ServiceURL = s3.Endpoint,
            AuthenticationRegion = s3.Regiao,
            // Provedores S3-compatíveis (OCI incluso) não suportam o estilo "virtual host"
            // (bucket.endpoint) nem os checksums CRC que o SDK v4 manda por padrão.
            ForcePathStyle = true,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        _cliente = new AmazonS3Client(new BasicAWSCredentials(s3.ChaveDeAcesso, s3.ChaveSecreta), configuracao);
    }

    public async Task<string> SalvarAsync(Stream conteudo, string extensaoDeArquivo, string tipoConteudo, CancellationToken ct = default)
    {
        var chave = $"{PrefixoChave}{Guid.NewGuid()}{extensaoDeArquivo}";

        // O SDK precisa saber o tamanho (Content-Length) — a stream do upload HTTP nem sempre é
        // "seekable"; o limite de 5 MB do endpoint torna o buffer em memória barato.
        using var buffer = new MemoryStream();
        await conteudo.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        await _cliente.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _opcoes.S3.Bucket,
            Key = chave,
            InputStream = buffer,
            ContentType = tipoConteudo,
            // Imagem nunca muda depois de enviada (nome é um Guid novo) — cache agressivo.
            Headers = { CacheControl = "public, max-age=31536000, immutable" },
            // OCI não aceita o upload "aws-chunked" (streaming SigV4).
            UseChunkEncoding = false,
        }, ct);

        return $"{ObterUrlBase()}/{chave}";
    }

    public async Task RemoverAsync(string url, CancellationToken ct = default)
    {
        var chave = ExtrairChave(url, ObterUrlBase());
        if (chave is null)
        {
            return;
        }

        // DeleteObject do S3 já é idempotente (204 mesmo se a chave não existir).
        await _cliente.DeleteObjectAsync(_opcoes.S3.Bucket, chave, ct);
    }

    /// <summary>Chave do objeto a partir da URL pública — null se a URL não for deste bucket.</summary>
    internal static string? ExtrairChave(string url, string urlBase)
    {
        var prefixo = $"{urlBase}/";
        if (!url.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var chave = url[prefixo.Length..];
        return chave.StartsWith(PrefixoChave, StringComparison.Ordinal) && !chave.Contains("..") ? chave : null;
    }

    public void Dispose() => _cliente.Dispose();

    private string ObterUrlBase()
    {
        if (string.IsNullOrWhiteSpace(_opcoes.UrlPublica))
        {
            throw new InvalidOperationException("Storage:UrlPublica não configurada.");
        }

        return _opcoes.UrlPublica.TrimEnd('/');
    }
}
