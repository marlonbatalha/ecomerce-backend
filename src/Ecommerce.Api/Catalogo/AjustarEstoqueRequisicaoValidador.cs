using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class AjustarEstoqueRequisicaoValidador : AbstractValidator<AjustarEstoqueRequisicao>
{
    public AjustarEstoqueRequisicaoValidador()
    {
        RuleFor(r => r.NovoEstoque).GreaterThanOrEqualTo(0);
    }
}
