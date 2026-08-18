using Ecommerce.Domain.Catalogo;
using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class AtualizarProdutoRequisicaoValidador : AbstractValidator<AtualizarProdutoRequisicao>
{
    public AtualizarProdutoRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(Produto.TamanhoMaximoNome);
        RuleFor(r => r.Descricao).MaximumLength(Produto.TamanhoMaximoDescricao);
        RuleFor(r => r.Preco).GreaterThanOrEqualTo(0);
    }
}
