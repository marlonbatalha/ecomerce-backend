using Ecommerce.Domain.Vendas;

namespace Ecommerce.Api.Vendas;

public record CriarCupomRequisicao(
    string Codigo,
    string? Descricao,
    TipoDeDesconto Tipo,
    decimal Valor,
    decimal? ValorMinimoDoPedido,
    DateTime? ValidoAteUtc,
    int? LimiteDeUsos);
