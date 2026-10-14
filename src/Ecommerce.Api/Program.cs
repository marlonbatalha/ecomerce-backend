using System.Threading.RateLimiting;
using Ecommerce.Api;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Api.Catalogo;
using Ecommerce.Api.Contas;
using Ecommerce.Api.Middlewares;
using Ecommerce.Api.Validacao;
using Ecommerce.Api.Vendas;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Identidade;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((contexto, servicos, configuracao) => configuracao
        .ReadFrom.Configuration(contexto.Configuration)
        .ReadFrom.Services(servicos)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Registra os serviços no container de DI.
    builder.Services.AddControllers(opcoes => opcoes.Filters.Add<FiltroDeValidacao>());
    builder.Services.AddScoped<FiltroDeValidacao>();
    builder.Services.AddExceptionHandler<ManipuladorDeExcecoesDeDominio>();
    builder.Services.AddProblemDetails();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opcoes =>
    {
        opcoes.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Ecommerce API",
            Version = "v1",
            Description = "API do e-commerce (ecomerce-backend) — .NET 10 / Oracle Database.",
        });

        // TODO(Fase 1): documentar o esquema Bearer JWT no Swagger (AddSecurityDefinition/
        // AddSecurityRequirement) assim que confirmarmos a forma exata da API de segurança do
        // Microsoft.OpenApi 2.x usada pelo Swashbuckle 10 — os endpoints já funcionam via
        // Authorization: Bearer <token> mesmo sem essa anotação, só falta o botão "Authorize" na UI.
    });

    builder.Services.AdicionarInfraestrutura(builder.Configuration);

    // Validadores FluentValidation dos DTOs de request (ver seção 2.5 do plano) — aplicados
    // automaticamente pelo FiltroDeValidacao registrado acima.
    builder.Services.AddScoped<IValidator<RegistrarRequisicao>, RegistrarRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<LoginRequisicao>, LoginRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<CriarCategoriaRequisicao>, CriarCategoriaRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<RenomearCategoriaRequisicao>, RenomearCategoriaRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<CriarProdutoRequisicao>, CriarProdutoRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<AtualizarProdutoRequisicao>, AtualizarProdutoRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<AdicionarVarianteRequisicao>, AdicionarVarianteRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<AjustarEstoqueRequisicao>, AjustarEstoqueRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<AdicionarItemRequisicao>, AdicionarItemRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<AtualizarQuantidadeRequisicao>, AtualizarQuantidadeRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<MarcarComoEnviadoRequisicao>, MarcarComoEnviadoRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<CalcularFreteRequisicao>, CalcularFreteRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<CriarEnderecoRequisicao>, CriarEnderecoRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<AtualizarPerfilRequisicao>, AtualizarPerfilRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<TrocarSenhaRequisicao>, TrocarSenhaRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<ReordenarImagensRequisicao>, ReordenarImagensRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<CriarCupomRequisicao>, CriarCupomRequisicaoValidador>();
    builder.Services.AddScoped<IValidator<ValidarCupomRequisicao>, ValidarCupomRequisicaoValidador>();

    builder.Services.AddRateLimiter(opcoes =>
    {
        opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Login/registro: superfície clássica de brute-force/enumeração de e-mail. Em
        // Development (ambiente também usado pelos testes de integração via
        // WebApplicationFactory) o limite é bem mais alto — vários testes que sobem o mesmo host
        // fazem várias chamadas de auth em segundos, e não queremos derrubar a suíte por 429.
        var limiteAutenticacao = builder.Environment.IsDevelopment() ? 1000 : 10;

        opcoes.AddFixedWindowLimiter(PoliticasDeRateLimiting.Autenticacao, janela =>
        {
            janela.PermitLimit = limiteAutenticacao;
            janela.Window = TimeSpan.FromMinutes(1);
            janela.QueueLimit = 0;
        });

        // Por IP: sem isso, a validação pública de cupom vira oráculo pra testar códigos em massa.
        opcoes.AddPolicy(PoliticasDeRateLimiting.Cupons, contexto => RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Environment.IsDevelopment() ? 1000 : 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    });

    builder.Services
        .AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
    // Um health check dedicado ao banco (AddOracle/AddDbContextCheck) será adicionado
    // junto com o primeiro DbContext real, na Fase 1 (ver docs/plano-ecommerce.md).

    var politicaCors = "Frontend";
    var origensPermitidas = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(opcoes =>
    {
        opcoes.AddPolicy(politicaCors, politica =>
        {
            if (origensPermitidas.Length > 0)
            {
                politica.WithOrigins(origensPermitidas)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    var app = builder.Build();

    await SementeDePapeis.GarantirPapeisAsync(app.Services);

    app.UseExceptionHandler();

    app.UseSerilogRequestLogging();

    // Cabeçalhos de segurança básicos (ver seção 1.7/Fase 1 do plano — revisão completa fica pra
    // Fase 6, mas o mínimo já entra aqui desde o início).
    app.Use(async (contexto, proximo) =>
    {
        contexto.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        contexto.Response.Headers.Append("X-Frame-Options", "DENY");
        await proximo();
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // Configura o pipeline HTTP.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opcoes =>
        {
            opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1");
        });
    }

    app.UseHttpsRedirection();

    // Serve wwwroot/uploads/produtos (ver ArmazenamentoDeImagensLocal) — imagem é buscada direto
    // pelo browser via <img>/next-image, não passa por autenticação nem CORS (carregamento de
    // imagem via tag <img> não é sujeito a CORS). FileProvider explícito (em vez de
    // env.WebRootFileProvider, o default de UseStaticFiles()) porque wwwroot/ pode não existir
    // ainda no boot (só é criada no primeiro upload) — o provider default resolve pra
    // NullFileProvider nesse caso e nunca passa a servir nada depois, mesmo com a pasta criada
    // em runtime.
    var caminhoWwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(caminhoWwwroot); // PhysicalFileProvider exige que a pasta já exista.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(caminhoWwwroot),
    });

    app.UseCors(politicaCors);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A aplicação falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Necessário para <c>WebApplicationFactory&lt;Program&gt;</c> nos testes de integração.</summary>
public partial class Program
{
}
