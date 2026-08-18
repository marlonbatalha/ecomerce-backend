namespace Ecommerce.Application.Contas;

public record EnderecoDto(
    Guid Id,
    string Rotulo,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep,
    bool Padrao);
