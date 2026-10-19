using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class CheckoutRequisicaoValidador : AbstractValidator<CheckoutRequisicao>
{
    public CheckoutRequisicaoValidador()
    {
        RuleFor(r => r.EnderecoId).NotEmpty().WithMessage("Escolha o endereço de entrega.");
        RuleFor(r => r.MetodoDeEnvio).NotEmpty().MaximumLength(50).WithMessage("Escolha o método de envio.");
    }
}
