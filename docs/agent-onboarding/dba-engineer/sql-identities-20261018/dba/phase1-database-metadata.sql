SET NOCOUNT ON;
SELECT DB_NAME() AS database_name;
SELECT name,type_desc,authentication_type_desc,USER_NAME(owning_principal_id) AS owner_name FROM sys.database_principals WHERE name IN (N'kdi_migrations',N'kdi_application',N'kdi_tests',N'kdi_migrator',N'kdi_app',N'kdi_test_runner',N'guest');
SELECT r.name AS role_name,u.name AS member_name FROM sys.database_role_members m JOIN sys.database_principals r ON r.principal_id=m.role_principal_id JOIN sys.database_principals u ON u.principal_id=m.member_principal_id WHERE r.name IN (N'kdi_migrations',N'kdi_application',N'kdi_tests') OR u.name IN (N'kdi_migrator',N'kdi_app',N'kdi_test_runner');
SELECT p.name,dp.state_desc,dp.permission_name,dp.class_desc,CASE WHEN dp.class=3 THEN SCHEMA_NAME(dp.major_id) WHEN dp.class=1 THEN OBJECT_SCHEMA_NAME(dp.major_id)+N'.'+OBJECT_NAME(dp.major_id) ELSE DB_NAME() END AS securable FROM sys.database_permissions dp JOIN sys.database_principals p ON p.principal_id=dp.grantee_principal_id WHERE p.name IN (N'kdi_migrations',N'kdi_application',N'kdi_tests',N'public',N'guest',N'kdi_migrator',N'kdi_app',N'kdi_test_runner') ORDER BY p.name,dp.class,dp.major_id,dp.permission_name;
SELECT name,USER_NAME(principal_id) AS owner_name FROM sys.schemas WHERE name IN(N'identity',N'organizations',N'portfolio');
SELECT SCHEMA_NAME(schema_id) AS schema_name,name FROM sys.tables WHERE is_ms_shipped=0;
