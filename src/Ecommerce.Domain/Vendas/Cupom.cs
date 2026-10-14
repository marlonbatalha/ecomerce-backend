using System.Globalization;
using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Cupom de desconto aplicado no checkout. O código é normalizado em maiúsculas (o cliente digita
/// "bemvindo10" ou "BEMVINDO10", tanto faz). <see cref="CalcularDesconto"/> valida tudo (ativo,
/// validade, limite de usos, valor mínimo) e nunca devolve mais que o subtotal — o pedido nunca
/// fica com total negativo. <see cref="QuantidadeDeUsos"/> é token de concorrência (ver
/// <c>ConfiguracaoCupom</c>): dois checkouts disputando o último uso, um deles falha.
/// </summary>
public class Cupom : Entidade
{
    public const int TamanhoMaximoCodigo = 30;

    public const int TamanhoMaximoDescricao = 200;

    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public string Codigo { get; private set; } = null!;

    public string Descricao { get; private set; } = string.Empty;

    public TipoDeDesconto Tipo { get; private set; }

    /// <summary>Percentual (1–100) ou valor em reais, conforme <see cref="Tipo"/>.</summary>
    public decimal Valor { get; private set; }

    public decimal? ValorMinimoDoPedido { get; private set; }

    public DateTime? ValidoAteUtc { get; private set; }

    /// <summary>Null = ilimitado.</summary>
    public int? LimiteDeUsos { get; private set; }

    public int QuantidadeDeUsos { get; private set; }

    public bool Ativo { get; private set; } = true;

    public DateTime CriadoEmUtc { get; private set; }

    private Cupom()
    {
    }

    private Cupom(Guid id, string codigo, string descricao, TipoDeDesconto tipo, decimal valor,
        decimal? valorMinimoDoPedido, DateTime? validoAteUtc, int? limiteDeUsos)
        : base(id)
    {
        Codigo = codigo;
        Descricao = descricao;
        Tipo = tipo;
        Valor = valor;
        ValorMinimoDoPedido = valorMinimoDoPedido;
        ValidoAteUtc = validoAteUtc;
        LimiteDeUsos = limiteDeUsos;
        CriadoEmUtc = DateTime.UtcNow;
    }

    public static Cupom Criar(string codigo, string descricao, TipoDeDesconto tipo, decimal valor,
        decimal? valorMinimoDoPedido = null, DateTime? validoAteUtc = null, int? limiteDeUsos = null)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ExcecaoDeDominio("O código do cupom é obrigatório.");
        }

        var codigoNormalizado = NormalizarCodigo(codigo);
        if (codigoNormalizado.Length > TamanhoMaximoCodigo || !codigoNormalizado.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new ExcecaoDeDominio($"O código do cupom deve ter até {TamanhoMaximoCodigo} caracteres (letras, números, '-' ou '_').");
        }

        var descricaoNormalizada = (descricao ?? string.Empty).Trim();
        if (descricaoNormalizada.Length > TamanhoMaximoDescricao)
        {
            throw new ExcecaoDeDominio($"A descrição do cupom não pode ter mais de {TamanhoMaximoDescricao} caracteres.");
        }

        if (tipo == TipoDeDesconto.Percentual && valor is <= 0 or > 100)
        {
            throw new ExcecaoDeDominio("O desconto percentual precisa estar entre 1 e 100.");
        }

        if (tipo == TipoDeDesconto.ValorFixo && valor <= 0)
        {
            throw new ExcecaoDeDominio("O desconto em valor precisa ser maior que zero.");
        }

        if (valorMinimoDoPedido is < 0)
        {
            throw new ExcecaoDeDominio("O valor mínimo do pedido não pode ser negativo.");
        }

        if (limiteDeUsos is <= 0)
        {
            throw new ExcecaoDeDominio("O limite de usos precisa ser maior que zero.");
        }

        return new Cupom(Guid.NewGuid(), codigoNormalizado, descricaoNormalizada, tipo, valor,
            valorMinimoDoPedido, validoAteUtc, limiteDeUsos);
    }

    public static string NormalizarCodigo(string codigo) => codigo.Trim().ToUpperInvariant();

    /// <summary>
    /// Desconto (em reais, arredondado a centavos) que este cupom dá sobre <paramref name="subtotal"/>.
    /// Lança <see cref="ExcecaoDeDominio"/> com a razão se o cupom não puder ser usado agora.
    /// </summary>
    public decimal CalcularDesconto(decimal subtotal, DateTime agoraUtc)
    {
        if (!Ativo)
        {
            throw new ExcecaoDeDominio("Este cupom não está mais ativo.");
        }

        if (ValidoAteUtc is { } validade && agoraUtc > validade)
        {
            throw new ExcecaoDeDominio("Este cupom expirou.");
        }

        if (LimiteDeUsos is { } limite && QuantidadeDeUsos >= limite)
        {
            throw new ExcecaoDeDominio("Este cupom atingiu o limite de usos.");
        }

        if (ValorMinimoDoPedido is { } minimo && subtotal < minimo)
        {
            throw new ExcecaoDeDominio($"Este cupom exige um pedido mínimo de {minimo.ToString("C", CulturaBrasileira)}.");
        }

        var desconto = Tipo == TipoDeDesconto.Percentual
            ? Math.Round(subtotal * Valor / 100m, 2, MidpointRounding.AwayFromZero)
            : Valor;

        return Math.Min(desconto, subtotal);
    }

    public void RegistrarUso() => QuantidadeDeUsos++;

    public void Ativar() => Ativo = true;

    public void Inativar() => Ativo = false;
}
