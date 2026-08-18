namespace Ecommerce.Api.Autenticacao;

/// <summary>Resposta pública de login/registro/renovação. O refresh token nunca vai aqui — só no cookie httpOnly.</summary>
public record TokenRespostaDto(string AccessToken, DateTime AccessTokenExpiraEmUtc);
