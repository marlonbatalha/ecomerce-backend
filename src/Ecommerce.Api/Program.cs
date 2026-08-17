using Ecommerce.Infrastructure;
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
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opcoes =>
    {
        opcoes.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Ecommerce API",
            Version = "v1",
            Description = "API do e-commerce (ecomerce-backend) — .NET 10 / Oracle Database.",
        });
    });

    builder.Services.AdicionarInfraestrutura(builder.Configuration);

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

    app.UseSerilogRequestLogging();

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
