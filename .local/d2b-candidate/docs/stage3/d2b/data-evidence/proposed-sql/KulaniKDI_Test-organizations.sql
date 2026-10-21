:ON ERROR EXIT
SET NOCOUNT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized migration target.', 1;
IF SCHEMA_ID(N'organizations') IS NULL
    THROW 51000, 'Provision the authorized schemas before migration.', 1;
IF OBJECT_ID(N'[organizations].[__EFMigrationsHistory]') IS NOT NULL
    EXEC(N'IF EXISTS (SELECT 1 FROM [organizations].[__EFMigrationsHistory] WHERE MigrationId NOT IN (N''20261018000100_InitialOrganizations'',N''20261019040514_Accessorganizations'') OR ProductVersion <> N''10.0.13'') THROW 51000, ''Unexpected migration history; stop for evaluation.'', 1;');
IF OBJECT_ID(N'[organizations].[__EFMigrationsHistory]') IS NULL
    THROW 51000, 'Approved baseline history is required.', 1;
EXEC(N'IF NOT EXISTS(SELECT 1 FROM [organizations].[__EFMigrationsHistory] WHERE MigrationId=N''20261018000100_InitialOrganizations'') THROW 51000, ''Approved baseline migration is required.'', 1;');
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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    IF SCHEMA_ID(N'organizations') IS NULL EXEC(N'CREATE SCHEMA [organizations];');
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE TABLE [organizations].[AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [ActorKind] nvarchar(16) NOT NULL,
        [ActorId] nvarchar(184) NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [TargetId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [AtUtc] datetime2 NOT NULL,
        [CorrelationId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE TABLE [organizations].[Organizations] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Organizations_Code] CHECK ([Code] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9_-]%' AND DATALENGTH([Code])>0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE TABLE [organizations].[OrganizationMemberships] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_OrganizationMemberships] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationMemberships_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [organizations].[Organizations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_OrganizationId_AtUtc] ON [organizations].[AuditEntries] ([OrganizationId], [AtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrganizationMemberships_OrganizationId_UserId] ON [organizations].[OrganizationMemberships] ([OrganizationId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE INDEX [IX_OrganizationMemberships_UserId_IsActive] ON [organizations].[OrganizationMemberships] ([UserId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_Code] ON [organizations].[Organizations] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [organizations].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040514_Accessorganizations'
)
BEGIN
    INSERT INTO [organizations].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019040514_Accessorganizations', N'10.0.13');
END;

COMMIT;
GO


PRINT 'MIGRATION SUCCESS KulaniKDI_Test/organizations';
GO