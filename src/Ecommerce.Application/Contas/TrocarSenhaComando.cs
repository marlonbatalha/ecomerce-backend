namespace Ecommerce.Application.Contas;

/// <summary>Exige a senha atual (reautenticação implícita) — ver Fase 4 do plano.</summary>
public record TrocarSenhaComando(Guid UsuarioId, string SenhaAtual, string NovaSenha);
