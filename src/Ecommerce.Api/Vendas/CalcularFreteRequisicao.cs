namespace Ecommerce.Api.Vendas;

public record CalcularFreteRequisicao(string Cep, IReadOnlyCollection<ItemFreteRequisicao> Itens);

public record ItemFreteRequisicao(Guid VarianteDeProdutoId, int Quantidade);
