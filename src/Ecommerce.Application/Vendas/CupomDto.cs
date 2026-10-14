using Ecommerce.Domain.Vendas;

namespace Ecommerce.Application.Vendas;

public record CupomDto(
    Guid Id,
    string Codigo,
    string Descricao,
    TipoDeDesconto Tipo,
    decimal Valor,
    decimal? ValorMinimoDoPedido,
    DateTime? ValidoAteUtc,
    int? LimiteDeUsos,
    int QuantidadeDeUsos,
    bool Ativo,
    DateTime CriadoEmUtc);

/// <summary>Prévia do desconto pro carrinho — o valor definitivo é recalculado no checkout.</summary>
public record ValidacaoDeCupomDto(string Codigo, string Descricao, decimal Desconto);

public record CriarCupomComando(
    string Codigo,
    string Descricao,
    TipoDeDesconto Tipo,
    decimal Valor,
    decimal? ValorMinimoDoPedido,
    DateTime? ValidoAteUtc,
    int? LimiteDeUsos);
