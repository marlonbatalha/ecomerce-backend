using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Autenticacao;
using Ecommerce.Application.Catalogo;
using Ecommerce.Application.Contas;
using Ecommerce.Application.Notificacoes;
using Ecommerce.Application.Vendas;
using Ecommerce.Infrastructure.Autenticacao;
using Ecommerce.Infrastructure.Catalogo;
using Ecommerce.Infrastructure.Contas;
using Ecommerce.Infrastructure.Identidade;
using Ecommerce.Infrastructure.Notificacoes;
using Ecommerce.Infrastructure.Persistencia;
using Ecommerce.Infrastructure.Vendas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure;

public static class InjecaoDeDependencia
{
    /// <summary>
    /// Registra os serviços da camada de Infrastructure (persistência, Identity/JWT, integrações
    /// externas). A connection string vem de configuração — em produção/staging, via variável de
    /// ambiente (ex.: ConnectionStrings__Default) apontando para o Oracle Wallet, nunca commitada.
    /// </summary>
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var stringConexao = configuracao.GetConnectionString("Default");

        servicos.AddDbContext<ContextoBancoDados>(opcoes =>
        {
            if (string.IsNullOrWhiteSpace(stringConexao))
            {
                // Sem connection string configurada (ex.: primeiro build local): registra o
                // DbContext mesmo assim para não quebrar o DI, mas qualquer uso real falhará
                // até ConnectionStrings:Default ser definido (appsettings.Development.json,
                // variável de ambiente ou secret).
                return;
            }

            ConfiguracaoOracle.Aplicar(opcoes, stringConexao);
        });

        AdicionarIdentidadeEJwt(servicos, configuracao);
        AdicionarOpcoesWebhook(servicos, configuracao);
        servicos.Configure<OpcoesAplicacao>(configuracao.GetSection(OpcoesAplicacao.SecaoConfiguracao));
        AdicionarArmazenamentoDeImagens(servicos, configuracao);
        AdicionarPagamentos(servicos, configuracao);
        AdicionarEmails(servicos, configuracao);

        servicos.AddScoped<IServicoAutenticacao, ServicoAutenticacao>();
        servicos.AddScoped<IServicoCategorias, ServicoCategorias>();
        servicos.AddScoped<IServicoProdutos, ServicoProdutos>();
        servicos.AddScoped<IServicoCarrinho, ServicoCarrinho>();
        servicos.AddScoped<IServicoDePedidos, ServicoDePedidos>();
        servicos.AddScoped<IServicoCupons, ServicoCupons>();
        servicos.AddScoped<IServicoDeFrete, ServicoDeFreteFake>();
        servicos.AddScoped<IServicoEnderecos, ServicoEnderecos>();
        servicos.AddScoped<IServicoConta, ServicoConta>();
        servicos.AddScoped<IServicoClientes, ServicoClientes>();

        return servicos;
    }

    private static void AdicionarIdentidadeEJwt(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos
            .AddIdentityCore<Usuario>(opcoes =>
            {
                opcoes.Password.RequiredLength = 8;
                opcoes.User.RequireUniqueEmail = true;

                // Força bruta contra uma conta específica vinda de vários IPs passa pelo rate
                // limit (que é por cliente) — o bloqueio por conta cobre esse caso. Ver
                // ServicoAutenticacao.LoginAsync.
                opcoes.Lockout.MaxFailedAccessAttempts = 5;
                opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddErrorDescriber<DescritorDeErrosIdentity>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ContextoBancoDados>()
            .AddSignInManager();

        var secaoJwt = configuracao.GetSection(OpcoesJwt.SecaoConfiguracao);
        var opcoesJwt = secaoJwt.Get<OpcoesJwt>() ?? new OpcoesJwt();

        if (string.IsNullOrWhiteSpace(opcoesJwt.ChaveAssinatura))
        {
            // Sem chave configurada (ex.: dev local sem secret definido): gera uma chave
            // aleatória válida só pra esse processo, pra não travar o boot. Tokens emitidos não
            // sobrevivem a um restart — aceitável em dev, e em staging/produção a chave real
            // SEMPRE vem de variável de ambiente/secret (nunca cai nesse caminho lá).
            opcoesJwt.ChaveAssinatura = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        // Registra a MESMA instância (já com o fallback de chave resolvido) como IOptions —
        // é o que ServicoAutenticacao usa pra assinar; precisa bater com o que AddJwtBearer usa
        // abaixo pra validar, senão nenhum token emitido passaria na validação.
        servicos.Configure<OpcoesJwt>(o =>
        {
            o.Emissor = opcoesJwt.Emissor;
            o.Audiencia = opcoesJwt.Audiencia;
            o.ChaveAssinatura = opcoesJwt.ChaveAssinatura;
            o.MinutosExpiracaoAccessToken = opcoesJwt.MinutosExpiracaoAccessToken;
            o.DiasExpiracaoRefreshToken = opcoesJwt.DiasExpiracaoRefreshToken;
        });

        servicos
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcoesJwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = opcoesJwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoesJwt.ChaveAssinatura)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        servicos.AddAuthorization();
    }

    /// <summary>Disco local (dev) ou Object Storage S3-compatível, por <c>Storage:Provedor</c>.</summary>
    private static void AdicionarArmazenamentoDeImagens(IServiceCollection servicos, IConfiguration configuracao)
    {
        var secao = configuracao.GetSection(OpcoesArmazenamento.SecaoConfiguracao);
        servicos.Configure<OpcoesArmazenamento>(secao);

        var provedor = secao.Get<OpcoesArmazenamento>()?.Provedor ?? OpcoesArmazenamento.ProvedorLocal;
        if (string.Equals(provedor, OpcoesArmazenamento.ProvedorS3, StringComparison.OrdinalIgnoreCase))
        {
            // Singleton: o AmazonS3Client é thread-safe e caro de criar (pool de conexões próprio).
            servicos.AddSingleton<IArmazenamentoDeImagens, ArmazenamentoDeImagensS3>();
        }
        else
        {
            servicos.AddScoped<IArmazenamentoDeImagens, ArmazenamentoDeImagensLocal>();
        }
    }

    /// <summary>Gateway fake (dev/testes) ou Mercado Pago, por <c>Pagamentos:Provedor</c>.</summary>
    private static void AdicionarPagamentos(IServiceCollection servicos, IConfiguration configuracao)
    {
        var secao = configuracao.GetSection(OpcoesPagamentos.SecaoConfiguracao);
        servicos.Configure<OpcoesPagamentos>(secao);

        var provedor = secao.Get<OpcoesPagamentos>()?.Provedor ?? OpcoesPagamentos.ProvedorFake;
        if (string.Equals(provedor, OpcoesPagamentos.ProvedorMercadoPago, StringComparison.OrdinalIgnoreCase))
        {
            servicos.AddHttpClient<IServicoDePagamento, ServicoDePagamentoMercadoPago>(cliente =>
            {
                cliente.BaseAddress = new Uri(ServicoDePagamentoMercadoPago.UrlBase);
                cliente.Timeout = TimeSpan.FromSeconds(15);
            });
        }
        else
        {
            servicos.AddScoped<IServicoDePagamento, ServicoDePagamentoFake>();
        }
    }

    /// <summary>
    /// E-mails transacionais: montados por <see cref="ServicoDeNotificacoesPorEmail"/>, enfileirados
    /// em memória e enviados em segundo plano (SMTP ou só log, por <c>Email:Provedor</c>).
    /// </summary>
    private static void AdicionarEmails(IServiceCollection servicos, IConfiguration configuracao)
    {
        var secao = configuracao.GetSection(OpcoesEmail.SecaoConfiguracao);
        servicos.Configure<OpcoesEmail>(secao);

        servicos.AddSingleton<FilaDeEmails>();
        servicos.AddHostedService<ProcessadorDeFilaDeEmails>();
        servicos.AddScoped<IServicoDeNotificacoes, ServicoDeNotificacoesPorEmail>();

        var provedor = secao.Get<OpcoesEmail>()?.Provedor ?? OpcoesEmail.ProvedorLog;
        if (string.Equals(provedor, OpcoesEmail.ProvedorSmtp, StringComparison.OrdinalIgnoreCase))
        {
            servicos.AddScoped<IEnviadorDeEmail, EnviadorDeEmailSmtp>();
        }
        else
        {
            servicos.AddScoped<IEnviadorDeEmail, EnviadorDeEmailLog>();
        }
    }

    private static void AdicionarOpcoesWebhook(IServiceCollection servicos, IConfiguration configuracao)
    {
        var secaoWebhook = configuracao.GetSection(OpcoesWebhook.SecaoConfiguracao);
        var opcoesWebhook = secaoWebhook.Get<OpcoesWebhook>() ?? new OpcoesWebhook();

        if (string.IsNullOrWhiteSpace(opcoesWebhook.ChaveSecreta))
        {
            // Mesmo raciocínio do fallback de OpcoesJwt acima: sem chave configurada, gera uma só
            // pra esse processo (dev). Em staging/produção a chave real vem de secret e é a MESMA
            // configurada no gateway de pagamento (nunca cai nesse caminho lá).
            opcoesWebhook.ChaveSecreta = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        servicos.Configure<OpcoesWebhook>(o => o.ChaveSecreta = opcoesWebhook.ChaveSecreta);
    }
}
