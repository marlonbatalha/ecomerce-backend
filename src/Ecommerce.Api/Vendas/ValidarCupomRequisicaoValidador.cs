using Ecommerce.Domain.Vendas;
using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class ValidarCupomRequisicaoValidador : AbstractValidator<ValidarCupomRequisicao>
{
    public ValidarCupomRequisicaoValidador()
    {
        RuleFor(r => r.Codigo).NotEmpty().MaximumLength(Cupom.TamanhoMaximoCodigo);
        RuleFor(r => r.Subtotal).GreaterThanOrEqualTo(0);
    }
}
