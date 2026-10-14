using Ecommerce.Domain.Vendas;
using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class CriarCupomRequisicaoValidador : AbstractValidator<CriarCupomRequisicao>
{
    public CriarCupomRequisicaoValidador()
    {
        RuleFor(r => r.Codigo).NotEmpty().MaximumLength(Cupom.TamanhoMaximoCodigo);
        RuleFor(r => r.Descricao).MaximumLength(Cupom.TamanhoMaximoDescricao);
        RuleFor(r => r.Tipo).IsInEnum();
        RuleFor(r => r.Valor).GreaterThan(0);
        RuleFor(r => r.Valor).LessThanOrEqualTo(100).When(r => r.Tipo == TipoDeDesconto.Percentual)
            .WithMessage("O desconto percentual precisa estar entre 1 e 100.");
        RuleFor(r => r.ValorMinimoDoPedido).GreaterThanOrEqualTo(0).When(r => r.ValorMinimoDoPedido.HasValue);
        RuleFor(r => r.LimiteDeUsos).GreaterThan(0).When(r => r.LimiteDeUsos.HasValue);
    }
}
