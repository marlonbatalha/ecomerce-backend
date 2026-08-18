namespace Ecommerce.Application.Catalogo;

public record AjusteDeEstoqueDto(Guid Id, int EstoqueAnterior, int EstoqueNovo, string Motivo, Guid UsuarioId, DateTime CriadoEmUtc);
