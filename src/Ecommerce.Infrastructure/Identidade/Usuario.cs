using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Identidade;

/// <summary>
/// Usuário da aplicação (cliente ou admin). Estende <see cref="IdentityUser{TKey}"/> pra
/// reaproveitar hashing de senha, roles e demais mecanismos do ASP.NET Core Identity — decisão
/// da seção 2.4 do plano. É um tipo de infraestrutura (acoplado ao Identity/EF Core) por natureza,
/// por isso não vive em Domain.
/// </summary>
public class Usuario : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Só os 11 dígitos (ver <c>Domain.Comum.Cpf</c>). Opcional no cadastro; o gateway exige para
    /// emitir Pix e boleto, então o checkout recusa sem ele. Único entre as contas.
    /// </summary>
    public string? Cpf { get; set; }
}
