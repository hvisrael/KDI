:ON ERROR EXIT
SET NOCOUNT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() NOT IN (N'KulaniKDI_Dev', N'KulaniKDI_Test')
    THROW 51000, 'Unauthorized verification target.', 1;
SELECT name, compatibility_level, recovery_model_desc, is_auto_close_on, is_auto_shrink_on, collation_name
FROM sys.databases WHERE database_id = DB_ID();
SELECT DB_NAME() AS database_name, name, type_desc, physical_name FROM sys.database_files;
SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')) AS default_data_path,
       CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath')) AS default_log_path;
SELECT name, USER_NAME(principal_id) AS owner_name FROM sys.schemas
WHERE name IN (N'identity',N'organizations',N'portfolio');
SELECT name, type_desc, authentication_type_desc FROM sys.database_principals
WHERE name IN (N'kdi_migrations',N'kdi_application',N'kdi_tests',
N'kdi_migration_witness',N'kdi_application_witness',N'kdi_test_witness');
SELECT rolep.name AS role_name, memberp.name AS member_name
FROM sys.database_role_members rm JOIN sys.database_principals rolep ON rolep.principal_id=rm.role_principal_id
JOIN sys.database_principals memberp ON memberp.principal_id=rm.member_principal_id
WHERE rolep.name IN (N'kdi_migrations',N'kdi_application',N'kdi_tests');
SELECT grantee.name, perm.state_desc, perm.permission_name, perm.class_desc,
       CASE WHEN perm.class=3 THEN SCHEMA_NAME(perm.major_id) ELSE DB_NAME() END AS scope_name
FROM sys.database_permissions perm JOIN sys.database_principals grantee ON perm.grantee_principal_id=grantee.principal_id
WHERE grantee.name IN (N'kdi_migrations',N'kdi_application',N'kdi_tests')
ORDER BY grantee.name, perm.class, perm.major_id, perm.permission_name;
EXECUTE AS USER = N'kdi_application_witness';
SELECT USER_NAME() AS witness, IS_MEMBER('db_owner') AS db_owner,
HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONNECT') AS can_connect,
HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CREATE TABLE') AS create_table,
HAS_PERMS_BY_NAME(N'identity','SCHEMA','ALTER') AS alter_identity,
HAS_PERMS_BY_NAME(N'organizations','SCHEMA','ALTER') AS alter_organizations,
HAS_PERMS_BY_NAME(N'portfolio','SCHEMA','ALTER') AS alter_portfolio;
REVERT;
PRINT 'READONLY VERIFICATION COMPLETE; witnesses do not establish Windows runtime identities.';
