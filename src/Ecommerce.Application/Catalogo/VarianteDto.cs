namespace Ecommerce.Application.Catalogo;

public record VarianteDto(Guid Id, string Nome, string Sku, decimal Preco, int Estoque, bool Ativo);
