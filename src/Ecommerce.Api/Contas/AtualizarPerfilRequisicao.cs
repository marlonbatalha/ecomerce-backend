namespace Ecommerce.Api.Contas;

/// <param name="Cpf">Com ou sem pontuação. Omitido (null) mantém o atual.</param>
public record AtualizarPerfilRequisicao(string Nome, string? Cpf = null);
