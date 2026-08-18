using Ecommerce.Application.Vendas;

namespace Ecommerce.Infrastructure.Vendas;

/// <summary>
/// Cálculo de frete mockado/fixo — ver Fase 3 do plano. Ignora CEP/itens de propósito; evolui pra
/// Correios/Melhor Envio na Fase 5 trocando só esta implementação.
/// </summary>
public class ServicoDeFreteFake : IServicoDeFrete
{
    public Task<IReadOnlyCollection<MetodoDeEnvioDto>> CalcularAsync(
        string cep, IReadOnlyCollection<ItemFreteConsulta> itens, CancellationToken ct = default)
    {
        IReadOnlyCollection<MetodoDeEnvioDto> opcoes =
        [
            new("PAC", 19.90m, 8),
            new("SEDEX", 34.90m, 3),
        ];

        return Task.FromResult(opcoes);
    }
}
