using Ecommerce.Api;
using Ecommerce.Api.Autenticacao;
using Ecommerce.Api.Catalogo;
using Ecommerce.Api.Middlewares;
using Ecommerce.Api.Validacao;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Identidade;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
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

    builder.Services.AddRateLimiter(opcoes =>
    {
        opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Login/registro: superfície clássica de brute-force/enumeração de e-mail.
        opcoes.AddFixedWindowLimiter(PoliticasDeRateLimiting.Autenticacao, janela =>
        {
            janela.PermitLimit = 10;
            janela.Window = TimeSpan.FromMinutes(1);
            janela.QueueLimit = 0;
        });
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
