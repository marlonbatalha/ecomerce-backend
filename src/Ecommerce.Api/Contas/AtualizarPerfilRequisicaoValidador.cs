using FluentValidation;

namespace Ecommerce.Api.Contas;

public class AtualizarPerfilRequisicaoValidador : AbstractValidator<AtualizarPerfilRequisicao>
{
    public AtualizarPerfilRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
    }
}
