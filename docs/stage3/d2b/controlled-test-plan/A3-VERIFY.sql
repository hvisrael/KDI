:ON ERROR EXIT
SET NOCOUNT ON;
-- Verificacion de solo lectura del resultado de A3. No escribe nada.

PRINT '=== 1. Objetivo y guards del paquete A3 ===';
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized verification target.', 1;
SELECT 'server' AS item, CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS value
UNION ALL SELECT 'database', DB_NAME()
UNION ALL SELECT 'version', CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion'));

-- Diagnostico previo: sin esto el script fallaria mas abajo con "Invalid object name",
-- que no distingue "A3 no aplicado" de "A3 aplicado con un objeto que falta".
IF OBJECT_ID(N'[identity].[Users]') IS NULL
   OR OBJECT_ID(N'[identity].[PermissionCatalog]') IS NULL
   OR OBJECT_ID(N'[identity].[Grants]') IS NULL
   OR OBJECT_ID(N'[identity].[Sessions]') IS NULL
   OR OBJECT_ID(N'[identity].[AuditEntries]') IS NULL
   OR OBJECT_ID(N'[organizations].[Organizations]') IS NULL
   OR OBJECT_ID(N'[organizations].[OrganizationMemberships]') IS NULL
   OR OBJECT_ID(N'[organizations].[AuditEntries]') IS NULL
    THROW 51000, 'A3 NOT APPLIED: one or more D2-B tables are missing. Run the two prepared SQL files first.', 1;
PRINT 'D2-B tables present.';

PRINT '=== 2. Historial de migraciones (deben ser 2 por modulo, EF 10.0.13) ===';
SELECT N'identity' AS module, MigrationId, ProductVersion FROM [identity].[__EFMigrationsHistory]
UNION ALL SELECT N'organizations', MigrationId, ProductVersion FROM [organizations].[__EFMigrationsHistory]
UNION ALL SELECT N'portfolio', MigrationId, ProductVersion FROM [portfolio].[__EFMigrationsHistory]
ORDER BY module, MigrationId;

PRINT '=== 3. Las ocho tablas D2-B (deben existir; portfolio intacto) ===';
SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name IN (N'identity', N'organizations', N'portfolio')
ORDER BY s.name, t.name;

PRINT '=== 4. Semilla de permisos (deben ser 8 codigos) ===';
SELECT Code FROM [identity].[PermissionCatalog] ORDER BY Code;

PRINT '=== 5. Indices creados por A3 ===';
SELECT OBJECT_SCHEMA_NAME(i.object_id) AS schema_name, OBJECT_NAME(i.object_id) AS table_name,
       i.name AS index_name, i.is_unique, i.is_primary_key, i.has_filter
FROM sys.indexes i
WHERE i.object_id IN (OBJECT_ID(N'[identity].[Users]'), OBJECT_ID(N'[identity].[Grants]'),
                       OBJECT_ID(N'[identity].[Sessions]'), OBJECT_ID(N'[identity].[AuditEntries]'),
                       OBJECT_ID(N'[identity].[PermissionCatalog]'), OBJECT_ID(N'[organizations].[Organizations]'),
                       OBJECT_ID(N'[organizations].[OrganizationMemberships]'), OBJECT_ID(N'[organizations].[AuditEntries]'))
  AND i.name IS NOT NULL
ORDER BY schema_name, table_name, index_name;

PRINT '=== 6. Claves foraneas y CHECK de las tablas nuevas ===';
SELECT OBJECT_SCHEMA_NAME(parent_object_id) AS schema_name, OBJECT_NAME(parent_object_id) AS table_name,
       name, type_desc
FROM sys.objects
WHERE parent_object_id IN (OBJECT_ID(N'[identity].[Users]'), OBJECT_ID(N'[identity].[Grants]'),
                           OBJECT_ID(N'[identity].[Sessions]'), OBJECT_ID(N'[identity].[AuditEntries]'),
                           OBJECT_ID(N'[organizations].[Organizations]'), OBJECT_ID(N'[organizations].[OrganizationMemberships]'))
  AND type IN ('F', 'C')
ORDER BY schema_name, table_name, type_desc, name;

PRINT '=== 7. rowversion en las entidades con Version (system_type_id 189) ===';
SELECT OBJECT_SCHEMA_NAME(c.object_id) AS schema_name, OBJECT_NAME(c.object_id) AS table_name, c.name AS column_name
FROM sys.columns c
WHERE c.system_type_id = 189
  AND c.object_id IN (OBJECT_ID(N'[identity].[Users]'), OBJECT_ID(N'[identity].[Grants]'),
                      OBJECT_ID(N'[identity].[Sessions]'), OBJECT_ID(N'[organizations].[Organizations]'),
                      OBJECT_ID(N'[organizations].[OrganizationMemberships]'))
ORDER BY schema_name, table_name;

PRINT '=== 8. Filas: deben ser 0 en las tablas nuevas (estado limpio) ===';
SELECT 'identity.Users' AS table_name, COUNT_BIG(*) AS rows_count FROM [identity].[Users]
UNION ALL SELECT 'identity.Grants', COUNT_BIG(*) FROM [identity].[Grants]
UNION ALL SELECT 'identity.Sessions', COUNT_BIG(*) FROM [identity].[Sessions]
UNION ALL SELECT 'identity.AuditEntries', COUNT_BIG(*) FROM [identity].[AuditEntries]
UNION ALL SELECT 'identity.PermissionCatalog', COUNT_BIG(*) FROM [identity].[PermissionCatalog]
UNION ALL SELECT 'organizations.Organizations', COUNT_BIG(*) FROM [organizations].[Organizations]
UNION ALL SELECT 'organizations.OrganizationMemberships', COUNT_BIG(*) FROM [organizations].[OrganizationMemberships]
UNION ALL SELECT 'organizations.AuditEntries', COUNT_BIG(*) FROM [organizations].[AuditEntries];

PRINT '=== 9. Permisos explicitos de los roles en la base (antes de A4) ===';
-- sys.fn_my_permissions no expone el estado (GRANT/DENY) en esta version, asi que se listan los
-- permisos explicitos por rol desde sys.database_permissions, con su estado real.
SELECT pr.name AS role_name, dp.class_desc, dp.permission_name, dp.state_desc
FROM sys.database_permissions dp
JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
WHERE pr.name IN (N'kdi_application', N'kdi_tests', N'kdi_migrations')
  AND dp.state_desc IN ('GRANT', 'DENY')
ORDER BY pr.name, dp.class_desc, dp.permission_name;

PRINT '=== VERIFICACION A3 COMPLETADA ===';
