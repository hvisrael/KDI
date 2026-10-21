:ON ERROR EXIT
SET NOCOUNT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Dev'
    THROW 51000, 'Unauthorized migration target.', 1;
IF SCHEMA_ID(N'identity') IS NULL
    THROW 51000, 'Provision the authorized schemas before migration.', 1;
IF OBJECT_ID(N'[identity].[__EFMigrationsHistory]') IS NOT NULL
    EXEC(N'IF EXISTS (SELECT 1 FROM [identity].[__EFMigrationsHistory] WHERE MigrationId NOT IN (N''20261018000100_InitialIdentity'',N''20261019040513_Accessidentity'') OR ProductVersion <> N''10.0.13'') THROW 51000, ''Unexpected migration history; stop for evaluation.'', 1;');
IF OBJECT_ID(N'[identity].[__EFMigrationsHistory]') IS NULL
    THROW 51000, 'Approved baseline history is required.', 1;
EXEC(N'IF NOT EXISTS(SELECT 1 FROM [identity].[__EFMigrationsHistory] WHERE MigrationId=N''20261018000100_InitialIdentity'') THROW 51000, ''Approved baseline migration is required.'', 1;');
PRINT 'MIGRATION START KulaniKDI_Dev/identity';
GO
IF OBJECT_ID(N'[identity].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
    CREATE TABLE [identity].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018000100_InitialIdentity'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018000100_InitialIdentity'
)
BEGIN
    INSERT INTO [identity].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018000100_InitialIdentity', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE TABLE [identity].[AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [ActorKind] nvarchar(16) NOT NULL,
        [ActorId] nvarchar(184) NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [TargetId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NULL,
        [AtUtc] datetime2 NOT NULL,
        [CorrelationId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE TABLE [identity].[PermissionCatalog] (
        [Code] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_PermissionCatalog] PRIMARY KEY ([Code])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE TABLE [identity].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [Issuer] nvarchar(512) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Subject] nvarchar(255) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [IssuerBytes] AS DATALENGTH([Issuer]) PERSISTED,
        [SubjectBytes] AS DATALENGTH([Subject]) PERSISTED,
        [DisplayName] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE TABLE [identity].[Grants] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [PermissionCode] nvarchar(40) NOT NULL,
        [ScopeType] nvarchar(16) NOT NULL,
        [ScopeId] uniqueidentifier NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_Grants] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Grants_Scope] CHECK (([ScopeType]='Organization' AND [ScopeId]=[OrganizationId]) OR ([ScopeType]='Project' AND [PermissionCode]='projects.read')),
        CONSTRAINT [FK_Grants_PermissionCatalog_PermissionCode] FOREIGN KEY ([PermissionCode]) REFERENCES [identity].[PermissionCatalog] ([Code]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Grants_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE TABLE [identity].[Sessions] (
        [Id] uniqueidentifier NOT NULL,
        [HandleHash] binary(32) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ProtectedTicket] varbinary(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastSeenAtUtc] datetime2 NOT NULL,
        [AbsoluteExpiresAtUtc] datetime2 NOT NULL,
        [IdleExpiresAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Sessions_Times] CHECK ([CreatedAtUtc]<=[LastSeenAtUtc] AND [LastSeenAtUtc]<=[IdleExpiresAtUtc] AND [IdleExpiresAtUtc]<=[AbsoluteExpiresAtUtc] AND ([RevokedAtUtc] IS NULL OR [ProtectedTicket] IS NULL)),
        CONSTRAINT [FK_Sessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code') AND [object_id] = OBJECT_ID(N'[identity].[PermissionCatalog]'))
        SET IDENTITY_INSERT [identity].[PermissionCatalog] ON;
    EXEC(N'INSERT INTO [identity].[PermissionCatalog] ([Code])
    VALUES (N''access.manage''),
    (N''audit.read''),
    (N''clients.manage''),
    (N''clients.read''),
    (N''members.manage''),
    (N''members.read''),
    (N''projects.manage''),
    (N''projects.read'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code') AND [object_id] = OBJECT_ID(N'[identity].[PermissionCatalog]'))
        SET IDENTITY_INSERT [identity].[PermissionCatalog] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_OrganizationId_AtUtc] ON [identity].[AuditEntries] ([OrganizationId], [AtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE INDEX [IX_Grants_PermissionCode] ON [identity].[Grants] ([PermissionCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Grants_UserId_OrganizationId_PermissionCode_ScopeType_ScopeId] ON [identity].[Grants] ([UserId], [OrganizationId], [PermissionCode], [ScopeType], [ScopeId]) WHERE [RevokedAtUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sessions_HandleHash] ON [identity].[Sessions] ([HandleHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE INDEX [IX_Sessions_UserId_AbsoluteExpiresAtUtc] ON [identity].[Sessions] ([UserId], [AbsoluteExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Issuer_Subject_IssuerBytes_SubjectBytes] ON [identity].[Users] ([Issuer], [Subject], [IssuerBytes], [SubjectBytes]);
END;

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019040513_Accessidentity'
)
BEGIN
    INSERT INTO [identity].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019040513_Accessidentity', N'10.0.13');
END;

COMMIT;
GO


PRINT 'MIGRATION SUCCESS KulaniKDI_Dev/identity';
GO