using Ecommerce.Application.Vendas;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Vendas;

[ApiController]
[Route("api/v1/frete")]
public class FreteController(IServicoDeFrete servicoDeFrete) : ControllerBase
{
    /// <summary>Mockado/fixo por enquanto (ver Fase 3 do plano) — evolui pra Correios/Melhor Envio na Fase 5.</summary>
    [HttpPost("calcular")]
    public async Task<ActionResult<IReadOnlyCollection<MetodoDeEnvioDto>>> Calcular(CalcularFreteRequisicao requisicao, CancellationToken ct)
    {
        var itens = requisicao.Itens.Select(i => new ItemFreteConsulta(i.VarianteDeProdutoId, i.Quantidade)).ToList();
        return Ok(await servicoDeFrete.CalcularAsync(requisicao.Cep, itens, ct));
    }
}
