using Ecommerce.Domain.Catalogo;
using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class CriarCategoriaRequisicaoValidador : AbstractValidator<CriarCategoriaRequisicao>
{
    public CriarCategoriaRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(Categoria.TamanhoMaximoNome);
    }
}
