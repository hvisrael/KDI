using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kulani.KDI.Infrastructure.Persistence;

// Explicit operator/design-time path; never registered in the runtime service container.
public static class MigrationContexts
{
    public static DbContext Create(string module, string connectionString)
    {
        var validated = SqlTarget.Validate(connectionString);
        return module switch
        {
            "identity" => new IdentityDbContext(Options<IdentityDbContext>(validated, module)),
            "organizations" => new OrganizationsDbContext(Options<OrganizationsDbContext>(validated, module)),
            "portfolio" => new PortfolioDbContext(Options<PortfolioDbContext>(validated, module)),
            _ => throw new ArgumentException("Unknown KDI module.", nameof(module))
        };
    }

    private static DbContextOptions<T> Options<T>(string connection, string schema) where T : DbContext =>
        new DbContextOptionsBuilder<T>().UseSqlServer(connection, sql =>
            sql.UseCompatibilityLevel(150).MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;

    internal static string ReadConnection() => Environment.GetEnvironmentVariable("KDI_MIGRATION_CONNECTION")
        ?? throw new InvalidOperationException("Set KDI_MIGRATION_CONNECTION for the authorized Windows migrator.");
}

public sealed class IdentityDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args) => (IdentityDbContext)MigrationContexts.Create("identity", MigrationContexts.ReadConnection());
}

public sealed class OrganizationsDesignTimeFactory : IDesignTimeDbContextFactory<OrganizationsDbContext>
{
    public OrganizationsDbContext CreateDbContext(string[] args) => (OrganizationsDbContext)MigrationContexts.Create("organizations", MigrationContexts.ReadConnection());
}

public sealed class PortfolioDesignTimeFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args) => (PortfolioDbContext)MigrationContexts.Create("portfolio", MigrationContexts.ReadConnection());
}
