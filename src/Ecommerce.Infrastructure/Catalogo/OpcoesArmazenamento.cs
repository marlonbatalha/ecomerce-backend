namespace Ecommerce.Infrastructure.Catalogo;

/// <summary>Vinculada à seção "Storage" da configuração. Ver <c>InjecaoDeDependencia</c>.</summary>
public class OpcoesArmazenamento
{
    public const string SecaoConfiguracao = "Storage";

    public const string ProvedorLocal = "Local";

    public const string ProvedorS3 = "S3";

    /// <summary><see cref="ProvedorLocal"/> (padrão, dev) ou <see cref="ProvedorS3"/>.</summary>
    public string Provedor { get; set; } = ProvedorLocal;

    /// <summary>
    /// Base das URLs públicas devolvidas ao frontend. Local: o host da própria API (ex.:
    /// <c>http://localhost:5000</c>). S3: a URL pública do bucket ou da CDN na frente dele (ex.:
    /// <c>https://objectstorage.sa-saopaulo-1.oraclecloud.com/n/{namespace}/b/{bucket}/o</c>).
    /// </summary>
    public string UrlPublica { get; set; } = string.Empty;

    public OpcoesS3 S3 { get; set; } = new();
}

/// <summary>
/// Credenciais/endereço do Object Storage S3-compatível. No OCI: <c>Endpoint</c> =
/// <c>https://{namespace}.compat.objectstorage.{regiao}.oraclecloud.com</c> e as chaves são uma
/// "Customer Secret Key" do usuário. Chaves vêm sempre de secret/variável de ambiente.
/// </summary>
public class OpcoesS3
{
    public string Endpoint { get; set; } = string.Empty;

    public string Regiao { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;

    public string ChaveDeAcesso { get; set; } = string.Empty;

    public string ChaveSecreta { get; set; } = string.Empty;
}
