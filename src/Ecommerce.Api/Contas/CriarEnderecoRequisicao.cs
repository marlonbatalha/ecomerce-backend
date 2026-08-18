namespace Ecommerce.Api.Contas;

public record CriarEnderecoRequisicao(
    string Rotulo,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep);
