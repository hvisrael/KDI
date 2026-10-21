using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

if (args.Length == 2 && args[0] == "--scaffold-offline")
{
    foreach (var module in new[] { "identity", "organizations" })
    {
        using var context = MigrationContexts.Create(module, $"Server={SqlTarget.Server};Database=KulaniKDI_Test;Integrated Security=true;Encrypt=true");
        var services = new ServiceCollection().AddEntityFrameworkDesignTimeServices().AddDbContextDesignTimeServices(context);
#pragma warning disable EF1001 // SQL Server's documented provider design-time service registration.
        new SqlServerDesignTimeServices().ConfigureDesignTimeServices(services);
#pragma warning restore EF1001
        using var provider = services.BuildServiceProvider();
        var scaffolder = provider.GetRequiredService<IMigrationsScaffolder>();
        var migration = scaffolder.ScaffoldMigration("Access" + module, "Kulani.KDI.Infrastructure.Modules." + (module == "identity" ? "Identity" : "Organizations") + ".Persistence.Migrations", subNamespace: null, language: "C#", dryRun: false);
        scaffolder.Save(args[1], migration, Path.GetFullPath(Path.Combine(args[1], "Modules", module == "identity" ? "Identity" : "Organizations", "Persistence", "Migrations")), dryRun: false);
        Console.WriteLine($"Scaffolded {module}; no SQL connection opened.");
    }
    return 0;
}

if (args.Length == 5 && args[0] == "--apply" && args[3] == "--approve-sha256" && args[2] is "identity" or "organizations")
{
    var db = args[1]; var module = args[2];
    // The session owns the operator credential material and destroys it when the connection is closed.
    await using var session = OperatorConnection.Create(db);
    var connection = session.Connection;
    DbContext context = module == "identity"
        ? new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(connection, s => s.MigrationsHistoryTable("__EFMigrationsHistory", module)).Options)
        : new OrganizationsDbContext(new DbContextOptionsBuilder<OrganizationsDbContext>().UseSqlServer(connection, s => s.MigrationsHistoryTable("__EFMigrationsHistory", module)).Options);
    await using (context)
    {
        var migrator = context.GetService<IMigrator>();
        var script = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script))), args[4], StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Reviewed migration SQL hash mismatch.");
        var expected = context.GetService<IMigrationsAssembly>().Migrations.Keys.Order().ToArray();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        if (applied.Length == 0 || !applied.SequenceEqual(expected.Take(applied.Length))) throw new InvalidOperationException("Migration history is not an approved baseline prefix.");
        await using var guard = connection.CreateCommand();
        guard.CommandText = $"IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'DESKTOP-F25O3MF' OR DB_NAME()<>N'{db}' THROW 51000,'Unauthorized migration target',1; IF EXISTS(SELECT 1 FROM [{module}].[__EFMigrationsHistory] WHERE ProductVersion<>N'10.0.13') THROW 51000,'Unexpected EF version',1;";
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await guard.ExecuteNonQueryAsync();
        await migrator.MigrateAsync();
        Console.WriteLine($"Migration completed {db}/{module}; no secrets recorded.");
    }
    return 0;
}

if (args.Length != 2 || args[0] is not ("KulaniKDI_Dev" or "KulaniKDI_Test"))
{
    Console.Error.WriteLine("Offline SQL generation only. Usage: <KulaniKDI_Dev|KulaniKDI_Test> <output-directory>");
    return 2;
}
var database = args[0];
Directory.CreateDirectory(args[1]);
foreach (var module in new[] { "identity", "organizations" })
{
    using var context = MigrationContexts.Create(module, $"Server={SqlTarget.Server};Database={database};Integrated Security=true;Encrypt=true");
    var migrationIds = context.GetService<IMigrationsAssembly>().Migrations.Keys.Order().ToArray();
    var permitted = string.Join(",", migrationIds.Select(id => "N''" + id + "''"));
    var sql = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
    var guarded = $"""
        :ON ERROR EXIT
        SET NOCOUNT ON;
        IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
            OR DB_NAME() <> N'{database}'
            THROW 51000, 'Unauthorized migration target.', 1;
        IF SCHEMA_ID(N'{module}') IS NULL
            THROW 51000, 'Provision the authorized schemas before migration.', 1;
        IF OBJECT_ID(N'[{module}].[__EFMigrationsHistory]') IS NOT NULL
            EXEC(N'IF EXISTS (SELECT 1 FROM [{module}].[__EFMigrationsHistory] WHERE MigrationId NOT IN ({permitted}) OR ProductVersion <> N''10.0.13'') THROW 51000, ''Unexpected migration history; stop for evaluation.'', 1;');
        IF OBJECT_ID(N'[{module}].[__EFMigrationsHistory]') IS NULL
            THROW 51000, 'Approved baseline history is required.', 1;
        EXEC(N'IF NOT EXISTS(SELECT 1 FROM [{module}].[__EFMigrationsHistory] WHERE MigrationId=N''{migrationIds[0]}'') THROW 51000, ''Approved baseline migration is required.'', 1;');
        PRINT 'MIGRATION START {database}/{module}';
        GO
        {sql}
        PRINT 'MIGRATION SUCCESS {database}/{module}';
        GO
        """;
    File.WriteAllText(Path.Combine(args[1], $"{database}-{module}.sql"), guarded);
    File.WriteAllText(Path.Combine(args[1], $"{database}-{module}.migration.sha256"), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))));
    // Review artifact only: never passed to MigrateAsync or executed by this command.
    // Downgrade stops at the accepted baseline and preserves its schema/history.
    var rollback = context.GetService<IMigrator>().GenerateScript(migrationIds[^1], migrationIds[0]);
    var guardedRollback = $"""
        :ON ERROR EXIT
        -- REVIEW ONLY: destructive D2-B data removal. Separate authorization and backup required.
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
            OR DB_NAME() <> N'{database}'
            THROW 51000, 'Unauthorized rollback target.', 1;
        IF OBJECT_ID(N'[{module}].[__EFMigrationsHistory]') IS NULL
            THROW 51000, 'Expected D2-B history required.', 1;
        EXEC(N'IF (SELECT COUNT(*) FROM [{module}].[__EFMigrationsHistory]) <> {migrationIds.Length}
            OR EXISTS(SELECT 1 FROM [{module}].[__EFMigrationsHistory] WHERE MigrationId NOT IN ({permitted}) OR ProductVersion <> N''10.0.13'')
            THROW 51000, ''Unexpected rollback history; stop for evaluation.'', 1;');
        GO
        {rollback}
        """;
    File.WriteAllText(Path.Combine(args[1], $"{database}-{module}.rollback.sql"), guardedRollback);
    Console.WriteLine($"Generated {database}/{module}; no SQL connection opened.");
}
return 0;
