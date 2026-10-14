using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class ReordenarImagensRequisicaoValidador : AbstractValidator<ReordenarImagensRequisicao>
{
    public ReordenarImagensRequisicaoValidador()
    {
        RuleFor(r => r.ImagemIds).NotEmpty();
    }
}
