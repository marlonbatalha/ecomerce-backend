using System.Collections.Concurrent;
using System.Linq;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Catalogo;
using Ecommerce.Infrastructure.Identidade;
using Ecommerce.Infrastructure.Notificacoes;
using Ecommerce.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ecommerce.IntegrationTests;

/// <summary>
/// Troca o <see cref="ContextoBancoDados"/> pra usar o provider EF Core InMemory em vez do
/// Oracle configurado em <c>InjecaoDeDependencia</c> — permite testar o pipeline HTTP completo
/// (Program.cs, DI, controllers, Identity/JWT) sem depender de Docker/Oracle disponível
/// (ver [[docker-indisponivel-ambiente-dev]] — a migration real contra Oracle é validada à parte).
/// </summary>
public class FabricaDeAplicacaoTeste : WebApplicationFactory<Program>
{
    private readonly string _nomeBancoEmMemoria = $"testes-{Guid.NewGuid()}";

    /// <summary>Imagens "enviadas" ficam aqui — nada toca o disco nem um bucket de verdade.</summary>
    public ArmazenamentoDeImagensEmMemoria Armazenamento { get; } = new();

    /// <summary>E-mails que o processador em segundo plano "enviou".</summary>
    public EnviadorDeEmailCapturado Emails { get; } = new();

    /// <summary>
    /// Provedores fixados em fake/log — o appsettings.Development.json local pode apontar pra
    /// Mercado Pago/S3/SMTP de verdade, e teste nunca pode chamar serviço externo. Subclasses
    /// sobrescrevem pra testar outro provedor (ver <see cref="FabricaDeAplicacaoMercadoPago"/>).
    /// </summary>
    protected virtual IDictionary<string, string?> Configuracoes => new Dictionary<string, string?>
    {
        ["Pagamentos:Provedor"] = "Fake",
        ["Storage:Provedor"] = "Local",
        ["Email:Provedor"] = "Log",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (chave, valor) in Configuracoes)
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<IArmazenamentoDeImagens>();
            servicos.AddSingleton<IArmazenamentoDeImagens>(Armazenamento);

            servicos.RemoveAll<IEnviadorDeEmail>();
            servicos.AddSingleton<IEnviadorDeEmail>(Emails);

            // EF Core 9+ guarda o lambda de configuração do AddDbContext original num
            // IDbContextOptionsConfiguration<T> separado — remover só o DbContextOptions<T> não
            // basta: se houver connection string local (appsettings.Development.json), o Oracle
            // continuaria sendo aplicado junto com o InMemory e o DI quebra.
            servicos.RemoveAll<DbContextOptions<ContextoBancoDados>>();
            servicos.RemoveAll<IDbContextOptionsConfiguration<ContextoBancoDados>>();

            servicos.AddDbContext<ContextoBancoDados>(opcoes => opcoes.UseInMemoryDatabase(_nomeBancoEmMemoria));
        });
    }

    /// <summary>
    /// Cria um usuário com a role Admin direto via Identity (sem passar pelo endpoint público de
    /// registro, que sempre atribui "cliente") — útil pra testar endpoints admin-only. O teste
    /// ainda precisa logar via <c>/api/v1/auth/login</c> pra obter um JWT válido com a claim de role.
    /// </summary>
    public async Task CriarUsuarioAdminAsync(string email, string senha)
    {
        using var escopo = Services.CreateScope();
        var gerenciadorUsuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        var usuario = new Usuario { UserName = email, Email = email, Nome = "Admin de Teste" };
        var resultado = await gerenciadorUsuarios.CreateAsync(usuario, senha);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        await gerenciadorUsuarios.AddToRoleAsync(usuario, Papeis.Admin);
    }
}

public class ArmazenamentoDeImagensEmMemoria : IArmazenamentoDeImagens
{
    public ConcurrentDictionary<string, byte[]> Arquivos { get; } = new();

    public async Task<string> SalvarAsync(Stream conteudo, string extensaoDeArquivo, string tipoConteudo, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await conteudo.CopyToAsync(buffer, ct);

        var url = $"https://imagens.teste/produtos/{Guid.NewGuid()}{extensaoDeArquivo}";
        Arquivos[url] = buffer.ToArray();
        return url;
    }

    public Task RemoverAsync(string url, CancellationToken ct = default)
    {
        Arquivos.TryRemove(url, out _);
        return Task.CompletedTask;
    }
}

public class EnviadorDeEmailCapturado : IEnviadorDeEmail
{
    public ConcurrentQueue<MensagemDeEmail> Enviados { get; } = new();

    public Task EnviarAsync(MensagemDeEmail mensagem, CancellationToken ct = default)
    {
        Enviados.Enqueue(mensagem);
        return Task.CompletedTask;
    }

    /// <summary>O envio é assíncrono (fila + BackgroundService) — espera até o e-mail aparecer.</summary>
    public async Task<MensagemDeEmail> AguardarAsync(Func<MensagemDeEmail, bool> filtro, int timeoutMs = 3000)
    {
        var limite = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < limite)
        {
            var encontrado = Enviados.FirstOrDefault(filtro);
            if (encontrado is not null)
            {
                return encontrado;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Nenhum e-mail correspondente foi enviado a tempo.");
    }
}
