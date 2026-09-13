namespace Ecommerce.Application.Catalogo;

/// <summary>
/// Abstração de onde as imagens de produto ficam guardadas — nunca o binário no Oracle (seção 1 do
/// plano), só a referência (<c>ImagemDeProduto.Url</c>). Implementado em Infrastructure: hoje é
/// disco local (<c>ArmazenamentoDeImagensLocal</c>), pensado pra virar Object Storage OCI sem
/// mexer em Application/Domain quando a infraestrutura real for provisionada (mesmo padrão já
/// usado em <see cref="Ecommerce.Application.Vendas.IServicoDePagamento"/> e
/// <c>IServicoDeFrete</c> — trocar a implementação, não o contrato).
/// </summary>
public interface IArmazenamentoDeImagens
{
    /// <summary>Salva o conteúdo e devolve a URL pública (absoluta) pra acessá-lo depois.</summary>
    Task<string> SalvarAsync(Stream conteudo, string extensaoDeArquivo, CancellationToken ct = default);
}
