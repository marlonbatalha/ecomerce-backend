using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class AjustarEstoqueRequisicaoValidador : AbstractValidator<AjustarEstoqueRequisicao>
{
    public AjustarEstoqueRequisicaoValidador()
    {
        RuleFor(r => r.NovoEstoque).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Motivo).NotEmpty().MaximumLength(300);
    }
}
