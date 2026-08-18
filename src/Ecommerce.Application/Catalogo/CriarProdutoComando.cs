namespace Ecommerce.Application.Catalogo;

public record CriarProdutoComando(string Nome, string Descricao, string Sku, decimal Preco, Guid CategoriaId);
