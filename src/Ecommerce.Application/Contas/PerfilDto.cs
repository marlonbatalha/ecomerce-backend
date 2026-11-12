namespace Ecommerce.Application.Contas;

/// <param name="Cpf">Só dígitos; null até o cliente cadastrar (exigido para pagar com Pix ou boleto).</param>
public record PerfilDto(Guid Id, string Nome, string Email, string? Cpf);
