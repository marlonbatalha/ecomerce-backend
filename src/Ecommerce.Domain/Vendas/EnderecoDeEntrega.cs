using Ecommerce.Domain.Comum;

namespace Ecommerce.Domain.Vendas;

/// <summary>
/// Cópia do endereço no momento da compra (value object do <see cref="Pedido"/>). Não referencia
/// o <c>Endereco</c> da conta de propósito: o cliente pode editar ou apagar o endereço depois, e o
/// pedido precisa continuar dizendo pra onde foi enviado.
/// </summary>
public class EnderecoDeEntrega
{
    public string Logradouro { get; private set; } = null!;

    public string Numero { get; private set; } = null!;

    public string? Complemento { get; private set; }

    public string Bairro { get; private set; } = null!;

    public string Cidade { get; private set; } = null!;

    public string Estado { get; private set; } = null!;

    public string Cep { get; private set; } = null!;

    /// <summary>Construtor sem parâmetros exigido pelo EF Core — não usar diretamente.</summary>
    private EnderecoDeEntrega()
    {
    }

    public static EnderecoDeEntrega Criar(
        string logradouro, string numero, string? complemento, string bairro, string cidade, string estado, string cep)
    {
        if (string.IsNullOrWhiteSpace(logradouro) || string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(bairro)
            || string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(estado) || string.IsNullOrWhiteSpace(cep))
        {
            throw new ExcecaoDeDominio("O endereço de entrega está incompleto.");
        }

        return new EnderecoDeEntrega
        {
            Logradouro = logradouro.Trim(),
            Numero = numero.Trim(),
            Complemento = string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim(),
            Bairro = bairro.Trim(),
            Cidade = cidade.Trim(),
            Estado = estado.Trim().ToUpperInvariant(),
            Cep = cep.Trim(),
        };
    }
}
