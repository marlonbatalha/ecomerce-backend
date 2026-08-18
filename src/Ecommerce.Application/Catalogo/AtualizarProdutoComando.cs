namespace Ecommerce.Application.Catalogo;

public record AtualizarProdutoComando(Guid ProdutoId, string Nome, string Descricao, decimal Preco);
