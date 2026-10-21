:ON ERROR EXIT
SET NOCOUNT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized migration target.', 1;
IF SCHEMA_ID(N'organizations') IS NULL
    THROW 51000, 'Provision the authorized schemas before migration.', 1;
IF OBJECT_ID(N'[organizations].[__EFMigrationsHistory]') IS NOT NULL
    EXEC(N'IF EXISTS (SELECT 1 FROM [organizations].[__EFMigrationsHistory] WHERE MigrationId <> N''20261018000100_InitialOrganizations'' OR ProductVersion <> N''10.0.13'') THROW 51000, ''Unexpected migration history; stop for evaluation.'', 1;');
PRINT 'MIGRATION START KulaniKDI_Test/organizations';
GO
IF OBJECT_ID(N'[organizations].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'organizations') IS NULL EXEC(N'CREATE SCHEMA [organizations];');
    CREATE TABLE [organizations].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018000100_InitialOrganizations'
)
BEGIN
    IF SCHEMA_ID(N'organizations') IS NULL EXEC(N'CREATE SCHEMA [organizations];');
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018000100_InitialOrganizations'
)
BEGIN
    INSERT INTO [organizations].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018000100_InitialOrganizations', N'10.0.13');
END;

COMMIT;
GO


PRINT 'MIGRATION SUCCESS KulaniKDI_Test/organizations';
GO