using Ecommerce.Domain.Catalogo;
using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class RenomearCategoriaRequisicaoValidador : AbstractValidator<RenomearCategoriaRequisicao>
{
    public RenomearCategoriaRequisicaoValidador()
    {
        RuleFor(r => r.NovoNome).NotEmpty().MaximumLength(Categoria.TamanhoMaximoNome);
    }
}
