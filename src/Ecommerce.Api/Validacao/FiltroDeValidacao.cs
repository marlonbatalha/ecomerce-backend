using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ecommerce.Api.Validacao;

/// <summary>
/// Filtro global: pra cada parâmetro de action que tenha um <see cref="IValidator{T}"/>
/// registrado no DI, valida antes de executar a action. Sem isso, cada controller precisaria
/// injetar e chamar seus validadores manualmente — registrado uma vez em Program.cs
/// (<c>MvcOptions.Filters</c>), cobre todo DTO de request que tiver um validador associado.
/// </summary>
public class FiltroDeValidacao(IServiceProvider servicos) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate proximo)
    {
        foreach (var argumento in contexto.ActionArguments.Values)
        {
            if (argumento is null)
            {
                continue;
            }

            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (servicos.GetService(tipoValidador) is not IValidator validador)
            {
                continue;
            }

            var resultado = await validador.ValidateAsync(new ValidationContext<object>(argumento));
            if (!resultado.IsValid)
            {
                var erros = resultado.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                contexto.Result = new BadRequestObjectResult(new ValidationProblemDetails(erros));
                return;
            }
        }

        await proximo();
    }
}
