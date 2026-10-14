using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Notificacoes;
using Ecommerce.Domain.Identidade;
using Ecommerce.Infrastructure.Identidade;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="IServicoAutenticacao"/> sobre o ASP.NET Core Identity. Emite
/// access token (JWT curto) e refresh token (valor aleatório opaco, persistido só como hash —
/// ver <see cref="TokenAtualizacao"/>).
/// </summary>
public class ServicoAutenticacao(
    UserManager<Usuario> gerenciadorUsuarios,
    ContextoBancoDados contexto,
    IOptions<OpcoesJwt> opcoesJwt,
    IServicoDeNotificacoes notificacoes) : IServicoAutenticacao
{
    private readonly OpcoesJwt _opcoes = opcoesJwt.Value;

    public async Task<ResultadoAutenticacao> RegistrarAsync(RegistrarUsuarioComando comando, CancellationToken ct = default)
    {
        var usuario = new Usuario
        {
            UserName = comando.Email,
            Email = comando.Email,
            Nome = comando.Nome,
        };

        var resultado = await gerenciadorUsuarios.CreateAsync(usuario, comando.Senha);
        if (!resultado.Succeeded)
        {
            return ResultadoAutenticacao.ComFalha(DescreverErros(resultado));
        }

        await gerenciadorUsuarios.AddToRoleAsync(usuario, Papeis.Cliente);
        await notificacoes.BoasVindasAsync(usuario.Email!, usuario.Nome, ct);

        return await GerarTokensAsync(usuario, ct);
    }

    public async Task<ResultadoAutenticacao> LoginAsync(LoginComando comando, CancellationToken ct = default)
    {
        var usuario = await gerenciadorUsuarios.FindByEmailAsync(comando.Email);
        if (usuario is null || !await gerenciadorUsuarios.CheckPasswordAsync(usuario, comando.Senha))
        {
            return ResultadoAutenticacao.ComFalha("E-mail ou senha inválidos.");
        }

        return await GerarTokensAsync(usuario, ct);
    }

    public async Task<ResultadoAutenticacao> AtualizarTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = CalcularHash(refreshToken);
        var tokenExistente = await contexto.TokensAtualizacao.FirstOrDefaultAsync(t => t.HashToken == hash, ct);

        if (tokenExistente is null)
        {
            return ResultadoAutenticacao.ComFalha("Refresh token inválido.");
        }

        if (tokenExistente.EstaRevogado)
        {
            // Token já usado antes: sinal de possível roubo/reuso — revoga toda a cadeia ativa
            // do usuário, forçando login de novo em todos os dispositivos.
            await RevogarTodosOsTokensAtivosAsync(tokenExistente.UsuarioId, ct);
            return ResultadoAutenticacao.ComFalha("Refresh token inválido.");
        }

        if (tokenExistente.EstaExpirado)
        {
            return ResultadoAutenticacao.ComFalha("Refresh token expirado.");
        }

        var usuario = await gerenciadorUsuarios.FindByIdAsync(tokenExistente.UsuarioId.ToString());
        if (usuario is null)
        {
            return ResultadoAutenticacao.ComFalha("Usuário não encontrado.");
        }

        var resultado = await GerarTokensAsync(usuario, ct, revogarESubstituir: tokenExistente);

        return resultado;
    }

    public async Task RevogarTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = CalcularHash(refreshToken);
        var token = await contexto.TokensAtualizacao.FirstOrDefaultAsync(t => t.HashToken == hash, ct);

        if (token is not null && !token.EstaRevogado)
        {
            token.Revogar();
            await contexto.SaveChangesAsync(ct);
        }
    }

    private async Task RevogarTodosOsTokensAtivosAsync(Guid usuarioId, CancellationToken ct)
    {
        var tokensAtivos = await contexto.TokensAtualizacao
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEmUtc == null)
            .ToListAsync(ct);

        foreach (var token in tokensAtivos)
        {
            token.Revogar();
        }

        if (tokensAtivos.Count > 0)
        {
            await contexto.SaveChangesAsync(ct);
        }
    }

    private async Task<ResultadoAutenticacao> GerarTokensAsync(
        Usuario usuario,
        CancellationToken ct,
        TokenAtualizacao? revogarESubstituir = null)
    {
        var papeis = await gerenciadorUsuarios.GetRolesAsync(usuario);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new("nome", usuario.Nome),
            // Jti garante um token único mesmo se emitido no mesmo segundo (mesmo exp) que um
            // anterior pro mesmo usuário — sem isso, dois tokens "iguais" viram o mesmo JWT
            // (mesma assinatura), o que também ajuda a rastrear/revogar um access token específico
            // no futuro, se precisar.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(papeis.Select(papel => new Claim(ClaimTypes.Role, papel)));

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.ChaveAssinatura));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var accessTokenExpiraEm = DateTime.UtcNow.AddMinutes(_opcoes.MinutosExpiracaoAccessToken);

        var jwt = new JwtSecurityToken(
            issuer: _opcoes.Emissor,
            audience: _opcoes.Audiencia,
            claims: claims,
            expires: accessTokenExpiraEm,
            signingCredentials: credenciais);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refreshTokenBruto = GerarRefreshTokenBruto();
        var hashNovoToken = CalcularHash(refreshTokenBruto);
        var duracaoRefresh = TimeSpan.FromDays(_opcoes.DiasExpiracaoRefreshToken);
        var tokenAtualizacao = TokenAtualizacao.Criar(usuario.Id, hashNovoToken, duracaoRefresh);

        contexto.TokensAtualizacao.Add(tokenAtualizacao);
        revogarESubstituir?.Revogar(hashNovoToken);

        await contexto.SaveChangesAsync(ct);

        return ResultadoAutenticacao.ComSucesso(usuario.Id, accessToken, accessTokenExpiraEm, refreshTokenBruto, tokenAtualizacao.ExpiraEmUtc);
    }

    private static string GerarRefreshTokenBruto() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string CalcularHash(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

    private static string DescreverErros(IdentityResult resultado) =>
        string.Join(" ", resultado.Errors.Select(e => e.Description));
}
