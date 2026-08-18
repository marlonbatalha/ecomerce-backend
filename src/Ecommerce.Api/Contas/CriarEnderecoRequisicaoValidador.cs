using Ecommerce.Domain.Contas;
using FluentValidation;

namespace Ecommerce.Api.Contas;

/// <summary>Reaproveitado também na atualização — os campos são exatamente os mesmos.</summary>
public class CriarEnderecoRequisicaoValidador : AbstractValidator<CriarEnderecoRequisicao>
{
    public CriarEnderecoRequisicaoValidador()
    {
        RuleFor(r => r.Rotulo).NotEmpty().MaximumLength(Endereco.TamanhoMaximoRotulo);
        RuleFor(r => r.Logradouro).NotEmpty().MaximumLength(Endereco.TamanhoMaximoLogradouro);
        RuleFor(r => r.Numero).NotEmpty().MaximumLength(Endereco.TamanhoMaximoNumero);
        RuleFor(r => r.Complemento).MaximumLength(Endereco.TamanhoMaximoComplemento);
        RuleFor(r => r.Bairro).NotEmpty().MaximumLength(Endereco.TamanhoMaximoBairro);
        RuleFor(r => r.Cidade).NotEmpty().MaximumLength(Endereco.TamanhoMaximoCidade);
        RuleFor(r => r.Estado).NotEmpty().Length(Endereco.TamanhoEstado);
        RuleFor(r => r.Cep).NotEmpty().Matches(@"^\d{5}-?\d{3}$").WithMessage("CEP inválido.");
    }
}
