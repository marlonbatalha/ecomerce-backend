using Ecommerce.Domain.Comum;
using FluentValidation;

namespace Ecommerce.Api.Contas;

public class AtualizarPerfilRequisicaoValidador : AbstractValidator<AtualizarPerfilRequisicao>
{
    public AtualizarPerfilRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Cpf)
            .Must(Cpf.EhValido)
            .When(r => r.Cpf is not null)
            .WithMessage("CPF inválido.");
    }
}
