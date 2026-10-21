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
//
// The baseline probes are outside the D2-B plan (PLAN.md, "No ejecutar SqlBaselineTests"): they migrate and
// reverse the migration history of identity, organizations and portfolio, and they need a Windows
// administrative identity. They therefore use their own opt-in, KDI_SQL_BASELINE_AUTHORIZED, and they refuse
// to run while the D2-B fixture switch is present, so a D2-B invocation can never reach them by accident.
//
// The switch is read from the environment of the test host process. The project runs the xUnit v3 tests
// through the Microsoft.Testing.Platform in process runner (global.json selects
// "test": { "runner": "Microsoft.Testing.Platform" }) and it has no runner specific configuration file, so
// the variables only have to be present in the environment of the process that starts the run.
public sealed class SqlBaselineTests
{
    private const string BaselineAuthorization = "BASELINE-PROBES";

    private static void RequireBaselineAuthorization()
    {
        if (Environment.GetEnvironmentVariable("KDI_D2B_SQL_AUTHORIZED") is not null)
            throw new InvalidOperationException("BLOCKED: the baseline probes are outside the D2-B plan and must not run while KDI_D2B_SQL_AUTHORIZED is set. Unset the D2-B fixture switch and run the baseline probes in their own invocation.");
        if (Environment.GetEnvironmentVariable("KDI_SQL_BASELINE_AUTHORIZED") != BaselineAuthorization)
            throw new InvalidOperationException("BLOCKED: the baseline probes require their own express authorization, KDI_SQL_BASELINE_AUTHORIZED=" + BaselineAuthorization + ", and a separate invocation.");
    }

    [Fact]
    public async Task Sql_server_baselines_repeat_and_transactional_downgrade_rolls_back_under_migration_role()
    {
        var cancellation = TestContext.Current.CancellationToken;
        RequireBaselineAuthorization();
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
                Assert.Equal(module == "portfolio" ? 1 : 2, first.Length);
                Assert.StartsWith("20261018000100_Initial", first[0]);
                await context.Database.MigrateAsync(cancellation);
                Assert.Equal(first, (await context.Database.GetAppliedMigrationsAsync(cancellation)).ToArray());
                Assert.Empty(await context.Database.GetPendingMigrationsAsync(cancellation));

                // Exercise the actual generated reverse chain inside an outer SQL transaction.
                // Baseline Down has no schema drop; only its own history row is removed temporarily.
                var reverse = context.GetService<IMigrator>().GenerateScript(first[^1], "0", MigrationsSqlGenerationOptions.NoTransactions);
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
        RequireBaselineAuthorization();
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
