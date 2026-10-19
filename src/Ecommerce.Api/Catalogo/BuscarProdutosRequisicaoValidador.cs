using FluentValidation;

namespace Ecommerce.Api.Catalogo;

public class BuscarProdutosRequisicaoValidador : AbstractValidator<BuscarProdutosRequisicao>
{
    public const int TamanhoMinimoTermo = 2;
    public const int TamanhoMaximoTermo = 100;

    public BuscarProdutosRequisicaoValidador()
    {
        // Endpoint público e a consulta mais cara da API (LIKE '%termo%'): termo de 1 letra casa
        // com quase tudo, e sem teto qualquer um manda um termo de megabytes pro banco.
        RuleFor(r => r.Termo)
            .Must(termo => termo!.Trim().Length is >= TamanhoMinimoTermo and <= TamanhoMaximoTermo)
            .When(r => !string.IsNullOrWhiteSpace(r.Termo))
            .WithMessage($"O termo de busca deve ter entre {TamanhoMinimoTermo} e {TamanhoMaximoTermo} caracteres.");
    }
}
