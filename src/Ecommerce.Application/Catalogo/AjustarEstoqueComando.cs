namespace Ecommerce.Application.Catalogo;

public record AjustarEstoqueComando(Guid ProdutoId, Guid VarianteId, int NovoEstoque);
