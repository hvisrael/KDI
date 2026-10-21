-- REVIEW ONLY. Not executed. A1 connection authorization required.
-- Connect explicitly to DESKTOP-F25O3MF / KulaniKDI_Test using approved existing operator.
:ON ERROR EXIT
SET NOCOUNT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized preflight target.', 1;
SELECT DB_NAME() AS DatabaseName,
       CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS ServerName,
       CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
       CONVERT(nvarchar(128), SERVERPROPERTY('Edition')) AS Edition;
SELECT compatibility_level, recovery_model_desc, state_desc
FROM sys.databases WHERE database_id = DB_ID();
SELECT USER_NAME() AS DatabaseUser, ORIGINAL_LOGIN() AS OriginalLogin;
SELECT name AS ModuleSchema FROM sys.schemas
WHERE name IN (N'identity', N'organizations', N'portfolio');
IF OBJECT_ID(N'[identity].[__EFMigrationsHistory]') IS NULL
    OR OBJECT_ID(N'[organizations].[__EFMigrationsHistory]') IS NULL
    OR OBJECT_ID(N'[portfolio].[__EFMigrationsHistory]') IS NULL
    THROW 51000, 'Baseline not visible or absent; stop.', 1;
SELECT N'identity' AS Module, MigrationId, ProductVersion FROM [identity].[__EFMigrationsHistory];
SELECT N'organizations' AS Module, MigrationId, ProductVersion FROM [organizations].[__EFMigrationsHistory];
SELECT N'portfolio' AS Module, MigrationId, ProductVersion FROM [portfolio].[__EFMigrationsHistory];
SELECT s.name AS ModuleSchema, t.name AS TableName
FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name IN (N'identity', N'organizations', N'portfolio')
ORDER BY s.name, t.name;
-- Current connection only; does not establish kdi_app effective privileges.
SELECT permission_name FROM sys.fn_my_permissions(NULL, 'DATABASE') ORDER BY permission_name;
-- No application rows/tickets/handles, EXECUTE AS, backup, DDL, DML or GRANT.
-- Metadata visibility may be incomplete. Absence is not proof of nonexistence.
