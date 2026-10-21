using System.Text.RegularExpressions;
using Kulani.KDI.Infrastructure.Persistence;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Kulani.KDI.SqlIntegrationTests;

// Explicit administrative test harness only. Does not authenticate a Windows runtime identity.
// Opt-in invocation requires KDI_SQL_TEST_CONNECTION; missing configuration is a failing check.
public sealed class SqlBaselineTests
{
    [Fact]
    public async Task Sql_server_baselines_repeat_and_transactional_downgrade_rolls_back_under_migration_role()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var connectionString = SqlTarget.Validate(Environment.GetEnvironmentVariable("KDI_SQL_TEST_CONNECTION")
            ?? throw new InvalidOperationException("BLOCKED: KDI_SQL_TEST_CONNECTION is required for SQL integration."), testOnly: true);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellation);
        await Execute(connection, "EXECUTE AS USER = N'kdi_migration_witness';", cancellation);
        try
        {
            foreach (var module in new[] { "identity", "organizations", "portfolio" })
            {
                await using var context = MigrationContexts.Create(module, connectionString);
                context.Database.SetDbConnection(connection, contextOwnsConnection: false);
                await context.Database.MigrateAsync(cancellation);
                var first = (await context.Database.GetAppliedMigrationsAsync(cancellation)).ToArray();
                Assert.Single(first);
                await context.Database.MigrateAsync(cancellation);
                Assert.Equal(first, (await context.Database.GetAppliedMigrationsAsync(cancellation)).ToArray());
                Assert.Empty(await context.Database.GetPendingMigrationsAsync(cancellation));

                // Exercise the actual generated reverse chain inside an outer SQL transaction.
                // Baseline Down has no schema drop; only its own history row is removed temporarily.
                var reverse = context.GetService<IMigrator>().GenerateScript(first[0], "0", MigrationsSqlGenerationOptions.NoTransactions);
                Assert.DoesNotContain("DROP SCHEMA", reverse, StringComparison.OrdinalIgnoreCase);
                using var transaction = connection.BeginTransaction();
                foreach (var batch in Regex.Split(reverse, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Where(value => !string.IsNullOrWhiteSpace(value)))
                {
                    await using var command = new SqlCommand(batch, connection, transaction);
                    await command.ExecuteNonQueryAsync(cancellation);
                }
                await using (var count = new SqlCommand($"SELECT COUNT(*) FROM [{module}].[__EFMigrationsHistory];", connection, transaction))
                    Assert.Equal(0, Convert.ToInt32(await count.ExecuteScalarAsync(cancellation)));
                transaction.Rollback();
                Assert.Equal(first, (await context.Database.GetAppliedMigrationsAsync(cancellation)).ToArray());
            }
        }
        finally { await Execute(connection, "REVERT;", cancellation); }

        // Test role can read history and cannot mutate it. No persistent data is written.
        await Execute(connection, "EXECUTE AS USER = N'kdi_test_witness';", cancellation);
        try
        {
            await Execute(connection, "SELECT MigrationId FROM [identity].[__EFMigrationsHistory];", cancellation);
            var error = await Assert.ThrowsAsync<SqlException>(() => Execute(connection,
                "UPDATE [identity].[__EFMigrationsHistory] SET ProductVersion=ProductVersion WHERE 1=0;", cancellation));
            Assert.Equal(229, error.Number);
        }
        finally { await Execute(connection, "REVERT;", cancellation); }
    }

    [Fact]
    public async Task Runtime_EF_connection_rejects_sysadmin_before_application_use()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var connectionString = SqlTarget.Validate(Environment.GetEnvironmentVariable("KDI_SQL_TEST_CONNECTION")
            ?? throw new InvalidOperationException("BLOCKED: KDI_SQL_TEST_CONNECTION is required for SQL integration."), testOnly: true);
        var services = new ServiceCollection();
        services.AddKdiPersistence(connectionString);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Database.OpenConnectionAsync(cancellation));
        Assert.Equal("KDI runtime database identity or privileges rejected.", error.Message);
    }

    private static async Task Execute(SqlConnection connection, string text, CancellationToken cancellation)
    {
        await using var command = new SqlCommand(text, connection);
        await command.ExecuteNonQueryAsync(cancellation);
    }
}
