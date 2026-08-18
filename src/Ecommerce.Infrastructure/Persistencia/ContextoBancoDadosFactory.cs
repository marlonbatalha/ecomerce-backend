using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ecommerce.Infrastructure.Persistencia;

/// <summary>
/// Usada só pelas ferramentas de design-time do EF Core (<c>dotnet ef migrations</c>,
/// <c>dotnet ef database update</c>) quando não há um host ASP.NET Core rodando pra montar o
/// <see cref="ContextoBancoDados"/> via DI (ver <c>InjecaoDeDependencia</c>). A connection string
/// abaixo só precisa apontar para o dialeto certo (Oracle) para gerar migrations — não precisa de
/// um banco real acessível — e usa as mesmas credenciais de desenvolvimento do
/// <c>docker/docker-compose.yml</c> (não é segredo de produção). Nunca é usada em runtime.
/// </summary>
public class ContextoBancoDadosFactory : IDesignTimeDbContextFactory<ContextoBancoDados>
{
    public ContextoBancoDados CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ContextoBancoDados>();
        ConfiguracaoOracle.Aplicar(builder, "User Id=ecommerce;Password=DevPassword123;Data Source=localhost:1521/XEPDB1");
        var opcoes = builder.Options;

        return new ContextoBancoDados(opcoes);
    }
}
