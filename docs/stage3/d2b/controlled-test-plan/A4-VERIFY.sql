-- =====================================================================================
--  A4 - verificacion de los permisos runtime (SOLO LECTURA)
--
--  Comprueba el resultado de KulaniKDI_Test-runtime-grants.sql con aserciones duras:
--  si el conjunto de permisos NO es exactamente el aprobado, el script falla.
--
--  Set esperado: 7 objetos, 10 permisos, para el rol kdi_application.
--  Prohibido: DELETE en cualquier sitio, permiso sobre organizations.AuditEntries,
--  concessions de esquema, membresias de rol nuevas, logins o usuarios nuevos.
-- =====================================================================================
:ON ERROR EXIT
SET NOCOUNT ON;

IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized verification target.', 1;

PRINT '=== A. Permisos a nivel de objeto concedidos a kdi_application ===';
SELECT OBJECT_SCHEMA_NAME(o.object_id) AS schema_name, o.name AS object_name, p.permission_name
FROM sys.database_permissions p
JOIN sys.objects o ON o.object_id = p.major_id
JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
WHERE pr.name = N'kdi_application' AND p.state_desc = 'GRANT' AND p.class_desc = 'OBJECT_OR_COLUMN'
ORDER BY schema_name, object_name, p.permission_name;

PRINT '=== B. Aserciones sobre el conjunto exacto de permisos ===';

-- Intercalaciones. Las columnas sysname del catalogo (o.name, p.permission_name) y el
-- resultado de OBJECT_SCHEMA_NAME() usan intercalaciones distintas de la del default de la
-- base, y comparar entre intercalaciones distintas falla. En lugar de fijar una a mano,
-- AMBOS lados se convierten a la intercalacion de la base: las variables de tabla se
-- declaran sin COLLATE (toman la del default) y el lado del catalogo se convierte con
-- CONVERT. Los literales de la tabla esperada toman tambien la del default.

-- B1. Lo concedido debe coincidir EXACTAMENTE con lo aprobado: ni uno mas, ni uno menos.
DECLARE @expected TABLE (schema_name nvarchar(128), object_name nvarchar(128), permission_name nvarchar(128));
INSERT INTO @expected VALUES
    (N'identity',      N'Users',                N'SELECT'),
    (N'identity',      N'Users',                N'INSERT'),
    (N'identity',      N'PermissionCatalog',    N'SELECT'),
    (N'identity',      N'Grants',               N'SELECT'),
    (N'identity',      N'Sessions',             N'SELECT'),
    (N'identity',      N'Sessions',             N'INSERT'),
    (N'identity',      N'Sessions',             N'UPDATE'),
    (N'identity',      N'AuditEntries',         N'INSERT'),
    (N'organizations', N'Organizations',        N'SELECT'),
    (N'organizations', N'OrganizationMemberships', N'SELECT');

-- El conjunto concedido se materializa una sola vez, con intercalacion explicita en las
-- tres columnas, para que las comparaciones con @expected no dependan de la intercalacion
-- que devuelvan las funciones de metadatos.
DECLARE @granted TABLE (schema_name nvarchar(128), object_name nvarchar(128), permission_name nvarchar(128));
INSERT INTO @granted
SELECT CONVERT(nvarchar(128), OBJECT_SCHEMA_NAME(o.object_id)),
       CONVERT(nvarchar(128), o.name),
       CONVERT(nvarchar(128), p.permission_name)
FROM sys.database_permissions p
JOIN sys.objects o ON o.object_id = p.major_id
JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
WHERE pr.name = N'kdi_application' AND p.state_desc = 'GRANT' AND p.class_desc = 'OBJECT_OR_COLUMN';

-- B1. Lo concedido debe coincidir EXACTAMENTE con lo aprobado: ni uno mas, ni uno menos.
SELECT 'FALTANTE (aprobado y no concedido)' AS finding, schema_name, object_name, permission_name
FROM (SELECT * FROM @expected EXCEPT SELECT * FROM @granted) d
UNION ALL
SELECT 'SOBRANTE (concedido y no aprobado)' AS finding, schema_name, object_name, permission_name
FROM (SELECT * FROM @granted EXCEPT SELECT * FROM @expected) d;

IF EXISTS (SELECT 1 FROM @granted g
           WHERE NOT EXISTS (SELECT 1 FROM @expected e
                             WHERE e.schema_name = g.schema_name AND e.object_name = g.object_name
                               AND e.permission_name = g.permission_name))
   OR EXISTS (SELECT 1 FROM @expected e
              WHERE NOT EXISTS (SELECT 1 FROM @granted g
                                WHERE g.schema_name = e.schema_name AND g.object_name = e.object_name
                                  AND g.permission_name = e.permission_name))
    THROW 51000, 'The object permissions of kdi_application are not exactly the approved set. See the findings above.', 1;

PRINT 'B1 OK: el conjunto de permisos coincide exactamente con el aprobado.';

-- B2. Prohibido DELETE en cualquier objeto para kdi_application.
IF EXISTS (SELECT 1 FROM sys.database_permissions p
           JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
           WHERE pr.name = N'kdi_application' AND p.permission_name = N'DELETE')
    THROW 51000, 'kdi_application must not hold DELETE. The review removed the DELETE on Sessions and it must not come back.', 1;
PRINT 'B2 OK: kdi_application no tiene DELETE.';

-- B3. Prohibido cualquier permiso sobre organizations.AuditEntries.
IF EXISTS (SELECT 1 FROM sys.database_permissions p
           JOIN sys.objects o ON o.object_id = p.major_id
           JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
           WHERE pr.name = N'kdi_application' AND p.state_desc = 'GRANT'
             AND o.name = N'AuditEntries' AND OBJECT_SCHEMA_NAME(o.object_id) = N'organizations')
    THROW 51000, 'kdi_application must not have any permission on organizations.AuditEntries.', 1;
PRINT 'B3 OK: sin permisos sobre organizations.AuditEntries.';

-- B4. Prohibidas concesiones de esquema: kdi_application solo puede tener el ALTER DENY previo.
IF EXISTS (SELECT 1 FROM sys.database_permissions p
           JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
           WHERE pr.name = N'kdi_application' AND p.class_desc = N'SCHEMA' AND p.state_desc = N'GRANT')
    THROW 51000, 'kdi_application must not receive schema level grants.', 1;
PRINT 'B4 OK: sin concesiones de esquema.';

-- B5. Los DENY de DDL del runtime deben seguir intactos.
IF (SELECT COUNT(*) FROM sys.database_permissions p
    JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
    WHERE pr.name = N'kdi_application' AND p.state_desc = N'DENY'
      AND p.permission_name IN (N'CREATE TABLE', N'CREATE SCHEMA', N'CREATE VIEW', N'CREATE PROCEDURE', N'CREATE FUNCTION', N'ALTER')) < 6
    THROW 51000, 'The DDL denies of kdi_application are not intact.', 1;
PRINT 'B5 OK: los DENY de DDL siguen intactos.';

PRINT '=== C. kdi_tests debe permanecer sin escritura ===';
IF EXISTS (SELECT 1 FROM sys.database_permissions p
           JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
           WHERE pr.name = N'kdi_tests' AND p.permission_name IN (N'INSERT', N'UPDATE', N'DELETE'))
    THROW 51000, 'kdi_tests must remain read-only. The D2-B plan grants it SELECT only.', 1;
PRINT 'C OK: kdi_tests sigue siendo de solo lectura.';

PRINT '=== D. Membresias de rol y logins: nada nuevo ===';
IF (SELECT COUNT(*) FROM sys.database_principals dp
    JOIN sys.server_principals sp ON sp.sid = dp.sid
    LEFT JOIN sys.database_role_members drm ON drm.member_principal_id = dp.principal_id
    WHERE sp.name IN (N'kdi_app', N'kdi_test_runner', N'kdi_migrator')
      AND drm.role_principal_id IS NOT NULL) <> 3
    THROW 51000, 'The role memberships of the three application logins changed.', 1;
IF (SELECT COUNT(*) FROM sys.sql_logins WHERE name IN (N'kdi_app', N'kdi_test_runner', N'kdi_migrator')) <> 3
    THROW 51000, 'The set of application logins changed.', 1;
PRINT 'D OK: membresias y logins sin cambios.';

PRINT '=== E. Condiciones del interceptor de runtime para kdi_app ===';
BEGIN TRY
    EXECUTE AS LOGIN = 'kdi_app';
    IF IS_SRVROLEMEMBER('sysadmin') = 1 OR IS_MEMBER('db_owner') = 1
       OR IS_MEMBER('kdi_application') <> 1
       OR HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') <> 0
       OR HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') <> 0
       OR HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE SCHEMA') <> 0
       OR HAS_PERMS_BY_NAME(N'identity', 'SCHEMA', 'ALTER') <> 0
       OR HAS_PERMS_BY_NAME(N'organizations', 'SCHEMA', 'ALTER') <> 0
       OR HAS_PERMS_BY_NAME(N'portfolio', 'SCHEMA', 'ALTER') <> 0
        THROW 51000, 'kdi_app would be rejected by the runtime privilege interceptor.', 1;
END TRY
BEGIN CATCH
    PRINT 'Effective check for kdi_app failed: ' + ERROR_MESSAGE();
    THROW 51000, 'kdi_app does not satisfy the runtime privilege interceptor conditions.', 1;
END CATCH
REVERT;
PRINT 'E OK: kdi_app cumple todas las condiciones del interceptor de runtime.';

PRINT 'A4 VERIFICATION OK';
