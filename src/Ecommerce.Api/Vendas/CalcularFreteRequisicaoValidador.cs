using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class CalcularFreteRequisicaoValidador : AbstractValidator<CalcularFreteRequisicao>
{
    public CalcularFreteRequisicaoValidador()
    {
        RuleFor(r => r.Cep).NotEmpty().Matches(@"^\d{5}-?\d{3}$").WithMessage("CEP inválido.");
        RuleFor(r => r.Itens).NotEmpty();
    }
}
