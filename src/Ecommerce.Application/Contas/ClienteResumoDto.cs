namespace Ecommerce.Application.Contas;

public record ClienteResumoDto(Guid Id, string Nome, string Email, IReadOnlyCollection<string> Papeis);
