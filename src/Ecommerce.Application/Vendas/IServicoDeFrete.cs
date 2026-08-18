namespace Ecommerce.Application.Vendas;

/// <summary>
/// Cálculo de frete — mockado/fixo por enquanto (ver Fase 3 do plano); evolui pra integração real
/// (Correios/Melhor Envio) na Fase 5, trocando só a implementação.
/// </summary>
public interface IServicoDeFrete
{
    Task<IReadOnlyCollection<MetodoDeEnvioDto>> CalcularAsync(string cep, IReadOnlyCollection<ItemFreteConsulta> itens, CancellationToken ct = default);
}
