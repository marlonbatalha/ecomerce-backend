using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class AdicionarItemRequisicaoValidador : AbstractValidator<AdicionarItemRequisicao>
{
    public AdicionarItemRequisicaoValidador()
    {
        RuleFor(r => r.VarianteDeProdutoId).NotEmpty();
        RuleFor(r => r.Quantidade).GreaterThan(0);
    }
}
