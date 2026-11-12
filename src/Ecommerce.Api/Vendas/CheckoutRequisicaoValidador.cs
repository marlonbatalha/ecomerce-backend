using Ecommerce.Domain.Vendas;
using FluentValidation;

namespace Ecommerce.Api.Vendas;

public class CheckoutRequisicaoValidador : AbstractValidator<CheckoutRequisicao>
{
    public CheckoutRequisicaoValidador()
    {
        RuleFor(r => r.EnderecoId).NotEmpty().WithMessage("Escolha o endereço de entrega.");
        RuleFor(r => r.MetodoDeEnvio).NotEmpty().MaximumLength(50).WithMessage("Escolha o método de envio.");
        RuleFor(r => r.MetodoDePagamento)
            .Must(m => TentarLerMetodo(m, out _))
            .WithMessage("Escolha Pix ou boleto como forma de pagamento.");
    }

    /// <summary>Só os nomes do enum: "1" ou "Pix,Boleto" passariam no Enum.TryParse puro.</summary>
    public static bool TentarLerMetodo(string? valor, out MetodoDePagamento metodo)
    {
        metodo = default;
        return !string.IsNullOrWhiteSpace(valor)
            && Enum.GetNames<MetodoDePagamento>().Any(n => string.Equals(n, valor.Trim(), StringComparison.OrdinalIgnoreCase))
            && Enum.TryParse(valor.Trim(), ignoreCase: true, out metodo);
    }
}
