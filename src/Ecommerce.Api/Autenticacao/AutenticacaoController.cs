using System.Security.Claims;
using Ecommerce.Application.Autenticacao;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Autenticacao;

[ApiController]
[Route("api/v1/auth")]
public class AutenticacaoController(
    IServicoAutenticacao servicoAutenticacao,
    IValidator<RegistrarRequisicao> validadorRegistrar,
    IValidator<LoginRequisicao> validadorLogin) : ControllerBase
{
    private const string NomeCookieRefreshToken = "refreshToken";

    [HttpPost("registrar")]
    [EnableRateLimiting(PoliticasDeRateLimiting.Autenticacao)]
    public async Task<ActionResult<TokenRespostaDto>> Registrar(RegistrarRequisicao requisicao, CancellationToken ct)
    {
        var validacao = await validadorRegistrar.ValidateAsync(requisicao, ct);
        if (!validacao.IsValid)
        {
            return CriarErroValidacao(validacao);
        }

        var comando = new RegistrarUsuarioComando(requisicao.Nome, requisicao.Email, requisicao.Senha);
        var resultado = await servicoAutenticacao.RegistrarAsync(comando, ct);

        if (!resultado.Sucesso)
        {
            return BadRequest(new { erro = resultado.Erro });
        }

        return Ok(MontarResposta(resultado));
    }

    [HttpPost("login")]
    [EnableRateLimiting(PoliticasDeRateLimiting.Autenticacao)]
    public async Task<ActionResult<TokenRespostaDto>> Login(LoginRequisicao requisicao, CancellationToken ct)
    {
        var validacao = await validadorLogin.ValidateAsync(requisicao, ct);
        if (!validacao.IsValid)
        {
            return CriarErroValidacao(validacao);
        }

        var comando = new LoginComando(requisicao.Email, requisicao.Senha);
        var resultado = await servicoAutenticacao.LoginAsync(comando, ct);

        if (!resultado.Sucesso)
        {
            return Unauthorized(new { erro = resultado.Erro });
        }

        return Ok(MontarResposta(resultado));
    }

    [HttpPost("atualizar-token")]
    public async Task<ActionResult<TokenRespostaDto>> AtualizarToken(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(NomeCookieRefreshToken, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { erro = "Refresh token não informado." });
        }

        var resultado = await servicoAutenticacao.AtualizarTokenAsync(refreshToken, ct);

        if (!resultado.Sucesso)
        {
            Response.Cookies.Delete(NomeCookieRefreshToken);
            return Unauthorized(new { erro = resultado.Erro });
        }

        return Ok(MontarResposta(resultado));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(NomeCookieRefreshToken, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            await servicoAutenticacao.RevogarTokenAsync(refreshToken, ct);
        }

        Response.Cookies.Delete(NomeCookieRefreshToken);
        return NoContent();
    }

    /// <summary>Endpoint protegido de exemplo — prova que o access token emitido é aceito pelo pipeline de auth.</summary>
    [HttpGet("eu")]
    [Authorize]
    public IActionResult Eu()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            email = User.FindFirstValue(ClaimTypes.Email),
            nome = User.FindFirstValue("nome"),
            papeis = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
        });
    }

    private TokenRespostaDto MontarResposta(ResultadoAutenticacao resultado)
    {
        Response.Cookies.Append(NomeCookieRefreshToken, resultado.RefreshToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = resultado.RefreshTokenExpiraEmUtc,
        });

        return new TokenRespostaDto(resultado.AccessToken!, resultado.AccessTokenExpiraEmUtc!.Value);
    }

    private static ActionResult CriarErroValidacao(ValidationResult validacao)
    {
        var erros = validacao.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ValidationProblemDetails(erros));
    }
}
