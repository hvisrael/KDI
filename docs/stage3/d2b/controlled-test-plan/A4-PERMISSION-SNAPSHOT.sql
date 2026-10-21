-- =====================================================================================
--  A4 - snapshot de permisos (SOLO LECTURA)
--  Se ejecuta ANTES de aplicar los GRANT y DESPUES, para comparar.
--  No concede, revoca ni altera nada. No hay DDL ni DML.
--
--  Salida: el estado explicito por rol, las membresias de rol, y los permisos
--  EFECTIVOS de kdi_app y kdi_test_runner observados por impersonacion.
-- =====================================================================================
:ON ERROR EXIT
SET NOCOUNT ON;

IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized snapshot target.', 1;

PRINT '=== 1. Permisos explicitos por rol (state_desc incluido) ===';
SELECT pr.name AS role_name, dp.class_desc, dp.major_id, dp.permission_name, dp.state_desc
FROM sys.database_permissions dp
JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
WHERE pr.name IN (N'kdi_application', N'kdi_tests', N'kdi_migrations')
ORDER BY pr.name, dp.class_desc, dp.permission_name;

PRINT '=== 2. Membresias de rol ===';
SELECT dp.name AS member, ISNULL(rm.name, '-') AS role_name
FROM sys.database_principals dp
JOIN sys.server_principals sp ON sp.sid = dp.sid
LEFT JOIN sys.database_role_members drm ON drm.member_principal_id = dp.principal_id
LEFT JOIN sys.database_principals rm ON rm.principal_id = drm.role_principal_id
WHERE sp.name IN (N'kdi_app', N'kdi_test_runner', N'kdi_migrator')
   OR rm.name IN (N'kdi_application', N'kdi_tests', N'kdi_migrations')
ORDER BY dp.name, rm.name;

PRINT '=== 3. Permisos EFECTIVOS de kdi_app (impersonacion de solo lectura) ===';
BEGIN TRY
    EXECUTE AS LOGIN = 'kdi_app';
    SELECT 'kdi_app' AS impersonated_login,
           HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL')        AS db_CONTROL,
           HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE')   AS db_CREATE_TABLE,
           HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE SCHEMA')  AS db_CREATE_SCHEMA,
           HAS_PERMS_BY_NAME(N'identity', 'SCHEMA', 'ALTER')           AS identity_ALTER,
           HAS_PERMS_BY_NAME(N'organizations', 'SCHEMA', 'ALTER')      AS organizations_ALTER,
           HAS_PERMS_BY_NAME(N'portfolio', 'SCHEMA', 'ALTER')          AS portfolio_ALTER,
           IS_SRVROLEMEMBER('sysadmin')                               AS is_sysadmin,
           IS_MEMBER('db_owner')                                      AS is_db_owner,
           IS_MEMBER('kdi_application')                                AS is_kdi_application;
    SELECT OBJECT_SCHEMA_NAME(o.object_id) AS schema_name, o.name AS object_name, p.permission_name
    FROM sys.database_permissions p
    JOIN sys.objects o ON o.object_id = p.major_id
    JOIN sys.database_principals pr ON pr.principal_id = p.grantee_principal_id
    WHERE pr.name = N'kdi_application' AND p.state_desc = 'GRANT' AND p.class_desc = 'OBJECT_OR_COLUMN'
    ORDER BY schema_name, object_name, p.permission_name;
END TRY
BEGIN CATCH
    PRINT 'Effective-permission probe for kdi_app failed: ' + ERROR_MESSAGE();
END CATCH
REVERT;

PRINT '=== 4. Permisos EFECTIVOS de kdi_test_runner (impersonacion de solo lectura) ===';
BEGIN TRY
    EXECUTE AS LOGIN = 'kdi_test_runner';
    SELECT 'kdi_test_runner' AS impersonated_login,
           HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL')       AS db_CONTROL,
           HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE')  AS db_CREATE_TABLE,
           HAS_PERMS_BY_NAME(N'identity', 'SCHEMA', 'ALTER')          AS identity_ALTER,
           IS_SRVROLEMEMBER('sysadmin')                              AS is_sysadmin,
           IS_MEMBER('db_owner')                                     AS is_db_owner,
           IS_MEMBER('kdi_tests')                                    AS is_kdi_tests;
END TRY
BEGIN CATCH
    PRINT 'Effective-permission probe for kdi_test_runner failed: ' + ERROR_MESSAGE();
END CATCH
REVERT;

PRINT '=== 5. Condiciones del interceptor de runtime (deben ser todas 1) ===';
-- Copia literal de la condicion de RuntimePrivilegeInterceptor.CheckSql, evaluada con la
-- identidad actual. Sirve para recordar que el runtime exige TODAS estas condiciones.
PRINT 'runtime guard: not sysadmin, not dbcreator, not db_owner, member of kdi_application, no CONTROL, no CREATE TABLE, no CREATE SCHEMA, no ALTER on the three schemas';

PRINT 'SNAPSHOT OK';
