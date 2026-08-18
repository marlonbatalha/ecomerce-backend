namespace Ecommerce.Api.Catalogo;

public record CriarProdutoRequisicao(string Nome, string Descricao, string Sku, decimal Preco, Guid CategoriaId);
