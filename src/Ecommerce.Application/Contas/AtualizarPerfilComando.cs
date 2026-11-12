namespace Ecommerce.Application.Contas;

/// <param name="Cpf">Null mantém o atual. Uma vez cadastrado, não pode ser apagado (só trocado).</param>
public record AtualizarPerfilComando(Guid UsuarioId, string Nome, string? Cpf = null);
