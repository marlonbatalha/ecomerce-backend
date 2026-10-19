using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Identidade;

/// <summary>
/// Mensagens do ASP.NET Identity em português. Elas chegam ao cliente final (cadastro, troca de
/// senha — ver <c>ServicoAutenticacao.DescreverErros</c>), e o padrão do Identity é inglês.
/// Só os erros que um usuário consegue provocar; o resto continua com a mensagem padrão.
/// </summary>
public class DescritorDeErrosIdentity : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"Já existe uma conta com o e-mail {email}." };

    // UserName = e-mail neste sistema: sem isso o cadastro duplicado mostraria as duas mensagens.
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"Já existe uma conta com o e-mail {userName}." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "E-mail inválido." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "E-mail inválido." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Senha atual incorreta." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"A senha precisa ter pelo menos {length} caracteres." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "A senha precisa ter pelo menos um número." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "A senha precisa ter pelo menos uma letra minúscula." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "A senha precisa ter pelo menos uma letra maiúscula." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "A senha precisa ter pelo menos um símbolo (ex.: ! @ #)." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"A senha precisa ter pelo menos {uniqueChars} caracteres diferentes." };
}
