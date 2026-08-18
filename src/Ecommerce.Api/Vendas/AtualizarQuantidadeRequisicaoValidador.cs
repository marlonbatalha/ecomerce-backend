using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class AtualizarQuantidadeRequisicaoValidador : AbstractValidator<AtualizarQuantidadeRequisicao>
{
    public AtualizarQuantidadeRequisicaoValidador()
    {
        RuleFor(r => r.Quantidade).GreaterThan(0);
    }
}
