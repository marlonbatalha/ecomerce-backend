using Ecommerce.Domain.Catalogo;
using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class CriarProdutoRequisicaoValidador : AbstractValidator<CriarProdutoRequisicao>
{
    public CriarProdutoRequisicaoValidador()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(Produto.TamanhoMaximoNome);
        RuleFor(r => r.Descricao).MaximumLength(Produto.TamanhoMaximoDescricao);
        RuleFor(r => r.Sku).NotEmpty().MaximumLength(Produto.TamanhoMaximoSku);
        RuleFor(r => r.Preco).GreaterThanOrEqualTo(0);
        RuleFor(r => r.CategoriaId).NotEmpty();
    }
}
