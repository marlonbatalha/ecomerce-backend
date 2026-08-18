namespace Ecommerce.Application.Contas;

public record AtualizarEnderecoComando(
    Guid EnderecoId,
    Guid UsuarioId,
    string Rotulo,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep);
