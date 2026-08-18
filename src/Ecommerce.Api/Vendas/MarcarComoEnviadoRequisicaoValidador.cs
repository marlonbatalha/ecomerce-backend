using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class MarcarComoEnviadoRequisicaoValidador : AbstractValidator<MarcarComoEnviadoRequisicao>
{
    public MarcarComoEnviadoRequisicaoValidador()
    {
        RuleFor(r => r.CodigoRastreio).NotEmpty().MaximumLength(50);
    }
}
