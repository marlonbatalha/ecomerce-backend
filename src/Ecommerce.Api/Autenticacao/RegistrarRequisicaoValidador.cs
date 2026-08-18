using FluentValidation;

namespace Ecommerce.Api.Autenticacao;

public class RegistrarRequisicaoValidador : AbstractValidator<RegistrarRequisicao>
{
    public RegistrarRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(r => r.Senha).NotEmpty().MinimumLength(8).WithMessage("A senha precisa ter pelo menos 8 caracteres.");
    }
}
