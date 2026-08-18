using FluentValidation;

namespace Ecommerce.Api.Contas;

public class TrocarSenhaRequisicaoValidador : AbstractValidator<TrocarSenhaRequisicao>
{
    public TrocarSenhaRequisicaoValidador()
    {
        RuleFor(r => r.SenhaAtual).NotEmpty();
        RuleFor(r => r.NovaSenha).NotEmpty().MinimumLength(8).WithMessage("A nova senha precisa ter pelo menos 8 caracteres.");
    }
}
