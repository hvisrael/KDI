using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

if (args.Length != 2 || args[0] is not ("KulaniKDI_Dev" or "KulaniKDI_Test"))
{
    Console.Error.WriteLine("Offline SQL generation only. Usage: <KulaniKDI_Dev|KulaniKDI_Test> <output-directory>");
    return 2;
}
var database = args[0];
Directory.CreateDirectory(args[1]);
foreach (var module in new[] { "identity", "organizations", "portfolio" })
{
    using var context = MigrationContexts.Create(module, $"Server={SqlTarget.Server};Database={database};Integrated Security=true;Encrypt=true");
    var migrationId = context.GetService<IMigrationsAssembly>().Migrations.Keys.Single();
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
            EXEC(N'IF EXISTS (SELECT 1 FROM [{module}].[__EFMigrationsHistory] WHERE MigrationId <> N''{migrationId}'' OR ProductVersion <> N''10.0.13'') THROW 51000, ''Unexpected migration history; stop for evaluation.'', 1;');
        PRINT 'MIGRATION START {database}/{module}';
        GO
        {sql}
        PRINT 'MIGRATION SUCCESS {database}/{module}';
        GO
        """;
    File.WriteAllText(Path.Combine(args[1], $"{database}-{module}.sql"), guarded);
    Console.WriteLine($"Generated {database}/{module}; no SQL connection opened.");
}
return 0;
