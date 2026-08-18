using Ecommerce.Domain.Catalogo;
using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class AdicionarVarianteRequisicaoValidador : AbstractValidator<AdicionarVarianteRequisicao>
{
    public AdicionarVarianteRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(VarianteDeProduto.TamanhoMaximoNome);
        RuleFor(r => r.Sku).NotEmpty().MaximumLength(VarianteDeProduto.TamanhoMaximoSku);
        RuleFor(r => r.Preco).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Estoque).GreaterThanOrEqualTo(0);
    }
}
