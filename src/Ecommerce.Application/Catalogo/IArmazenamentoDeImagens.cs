namespace Ecommerce.Application.Catalogo;

/// <summary>
/// Abstração de onde as imagens de produto ficam guardadas — nunca o binário no Oracle (seção 1 do
/// plano), só a referência (<c>ImagemDeProduto.Url</c>). Implementado em Infrastructure: disco
/// local em dev (<c>ArmazenamentoDeImagensLocal</c>) ou Object Storage via API S3-compatível
/// (<c>ArmazenamentoDeImagensS3</c> — OCI Object Storage, AWS S3, R2, MinIO), escolhido por
/// <c>Storage:Provedor</c>. Mesmo padrão de <see cref="Ecommerce.Application.Vendas.IServicoDePagamento"/>:
/// trocar a implementação, não o contrato.
/// </summary>
public interface IArmazenamentoDeImagens
{
    /// <summary>Salva o conteúdo e devolve a URL pública (absoluta) pra acessá-lo depois.</summary>
    Task<string> SalvarAsync(Stream conteudo, string extensaoDeArquivo, string tipoConteudo, CancellationToken ct = default);

    /// <summary>
    /// Apaga o arquivo referenciado por <paramref name="url"/> (devolvida antes por
    /// <see cref="SalvarAsync"/>). Não falha se o arquivo já não existir — remoção é idempotente.
    /// </summary>
    Task RemoverAsync(string url, CancellationToken ct = default);
}
