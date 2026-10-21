using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Kulani.KDI.UnitTests;

public sealed class PersistenceTests
{
    private const string Connection = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=true;Encrypt=true";

    [Theory]
    [InlineData("KulaniKDI_Dev")]
    [InlineData("KulaniKDI_Test")]
    public void Accepts_only_explicit_authorized_windows_targets(string database) =>
        Assert.Contains(database, SqlTarget.Validate(Connection.Replace("KulaniKDI_Test", database, StringComparison.Ordinal)));

    [Theory]
    [InlineData("Server=other;Database=KulaniKDI_Test;Integrated Security=true")]
    [InlineData("Server=DESKTOP-F25O3MF;Database=master;Integrated Security=true")]
    [InlineData("Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;User Id=unsafe;Password=synthetic")]
    [InlineData("Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=true;Failover Partner=other")]
    [InlineData("Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=true;Encrypt=false")]
    [InlineData("Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=true;Persist Security Info=true")]
    [InlineData("Server=DESKTOP-F25O3MF\\MSSQLSERVER;Database=KulaniKDI_Test;Integrated Security=true")]
    [InlineData("not a connection string")]
    public void Rejects_unauthorized_or_unsafe_connection_settings_without_echoing_input(string input)
    {
        var error = Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(input));
        Assert.DoesNotContain(input, error.Message);
        Assert.DoesNotContain("synthetic", error.Message);
    }

    [Fact]
    public void Test_guard_rejects_development_database() =>
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(Connection.Replace("Test", "Dev", StringComparison.Ordinal), testOnly: true));

    [Theory]
    [InlineData("identity")]
    [InlineData("organizations")]
    [InlineData("portfolio")]
    public void Baselines_have_one_owned_schema_and_history_without_business_entities(string module)
    {
        using var context = MigrationContexts.Create(module, Connection);
        Assert.Equal(module, context.Model.GetDefaultSchema());
        var migrations = context.GetService<IMigrationsAssembly>();
        var entry = migrations.Migrations.OrderBy(x => x.Key).First();
        Assert.True(context.GetService<IMigrationsIdGenerator>().IsValidId(entry.Key));
        var migration = migrations.CreateMigration(entry.Value, context.Database.ProviderName!);
        var ensure = Assert.IsType<EnsureSchemaOperation>(Assert.Single(migration.UpOperations));
        Assert.Equal(module, ensure.Name);
        Assert.Empty(migration.DownOperations);
        Assert.Empty(migration.TargetModel.GetEntityTypes());
        Assert.Equal(module == "portfolio" ? 1 : 2, migrations.Migrations.Count);
        Assert.Equal(module == "identity" ? 5 : module == "organizations" ? 3 : 0, context.Model.GetEntityTypes().Count());
        Assert.False(context.Database.HasPendingModelChanges());
        var script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains($"[{module}].[__EFMigrationsHistory]", script);
        Assert.Contains("IF NOT EXISTS", script);
        foreach (var foreign in new[] { "identity", "organizations", "portfolio" }.Where(value => value != module))
            Assert.DoesNotContain($"[{foreign}]", script);
    }
}
