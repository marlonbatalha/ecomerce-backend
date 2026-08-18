using FluentValidation;

namespace Ecommerce.Api.Autenticacao;

public class LoginRequisicaoValidador : AbstractValidator<LoginRequisicao>
{
    public LoginRequisicaoValidador()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress();
        RuleFor(r => r.Senha).NotEmpty();
    }
}
