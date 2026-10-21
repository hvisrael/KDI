:ON ERROR EXIT
-- REVIEW ONLY: destructive D2-B data removal. Separate authorization and backup required.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized rollback target.', 1;
IF OBJECT_ID(N'[identity].[__EFMigrationsHistory]') IS NULL
    THROW 51000, 'Expected D2-B history required.', 1;
EXEC(N'IF (SELECT COUNT(*) FROM [identity].[__EFMigrationsHistory]) <> 2
    OR EXISTS(SELECT 1 FROM [identity].[__EFMigrationsHistory] WHERE MigrationId NOT IN (N''20261018000100_InitialIdentity'',N''20261019040513_Accessidentity'') OR ProductVersion <> N''10.0.13'')
    THROW 51000, ''Unexpected rollback history; stop for evaluation.'', 1;');
GO
BEGIN TRANSACTION;
DROP TABLE [identity].[AuditEntries];

DROP TABLE [identity].[Grants];

DROP TABLE [identity].[Sessions];

DROP TABLE [identity].[PermissionCatalog];

DROP TABLE [identity].[Users];

DELETE FROM [identity].[__EFMigrationsHistory]
WHERE [MigrationId] = N'20261019040513_Accessidentity';

COMMIT;
GO

