using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kulani.KDI.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddKdiPersistence(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return services;
        var validated = SqlTarget.Validate(connectionString);
        services.AddSingleton<RuntimePrivilegeInterceptor>();
        services.AddDbContext<IdentityDbContext>((provider, options) => ConfigureRuntime(options, validated, "identity", provider));
        services.AddDbContext<OrganizationsDbContext>((provider, options) => ConfigureRuntime(options, validated, "organizations", provider));
        services.AddDbContext<PortfolioDbContext>((provider, options) => ConfigureRuntime(options, validated, "portfolio", provider));
        return services;
    }

    private static void ConfigureRuntime(DbContextOptionsBuilder options, string connection, string schema, IServiceProvider provider) =>
        options.UseSqlServer(connection, sql => sql.UseCompatibilityLevel(150).MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .AddInterceptors(provider.GetRequiredService<RuntimePrivilegeInterceptor>());

    // Readiness has no exception/configuration details in its public response.
    public static async Task<bool> IsKdiPersistenceReadyAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetService<IdentityDbContext>();
        if (context is null) return false;
        try
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            await context.Database.CloseConnectionAsync();
            return true;
        }
        catch (Exception) { return false; }
    }
}
