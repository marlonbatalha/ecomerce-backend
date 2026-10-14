namespace Ecommerce.Api.Catalogo;

/// <summary>Ids de todas as imagens do produto, na nova ordem de exibição.</summary>
public record ReordenarImagensRequisicao(IReadOnlyList<Guid> ImagemIds);
