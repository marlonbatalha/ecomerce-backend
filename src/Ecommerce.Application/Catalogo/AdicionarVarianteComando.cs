namespace Ecommerce.Application.Catalogo;

public record AdicionarVarianteComando(Guid ProdutoId, string Nome, string Sku, decimal Preco, int Estoque);
