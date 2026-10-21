SET NOCOUNT ON;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'DESKTOP-F25O3MF' OR IS_SRVROLEMEMBER('sysadmin')<>1
 THROW 51000,'Fixed target/admin visibility required.',1;
IF (SELECT COUNT(*) FROM sys.sql_logins WHERE name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner') AND is_policy_checked=1 AND is_expiration_checked=1 AND is_disabled=0)<>3
 THROW 51000,'Login policy/availability mismatch.',1;
IF EXISTS(SELECT 1 FROM sys.server_role_members m JOIN sys.server_principals p ON p.principal_id=m.member_principal_id WHERE p.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner'))
 THROW 51000,'Unexpected server role membership.',1;
GO
USE [KulaniKDI_Dev];
SELECT DB_NAME() database_name,u.name,u.type_desc,u.authentication_type_desc,CONVERT(varchar(170),u.sid,1) user_sid,CONVERT(varchar(170),s.sid,1) login_sid,r.name role_name
FROM sys.database_principals u LEFT JOIN sys.sql_logins s ON s.name=u.name
LEFT JOIN sys.database_role_members m ON m.member_principal_id=u.principal_id
LEFT JOIN sys.database_principals r ON r.principal_id=m.role_principal_id
WHERE u.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner');
IF (SELECT COUNT(*) FROM sys.database_principals WHERE name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner'))<>2
 OR EXISTS(SELECT 1 FROM sys.database_principals WHERE name=N'kdi_test_runner')
 THROW 51000,'Dev user inventory mismatch.',1;
IF (SELECT COUNT(*) FROM sys.database_principals u JOIN sys.sql_logins s ON s.name=u.name AND s.sid=u.sid
 JOIN sys.database_role_members m ON m.member_principal_id=u.principal_id JOIN sys.database_principals r ON r.principal_id=m.role_principal_id
 WHERE u.type='S' AND u.authentication_type_desc='INSTANCE' AND ((u.name=N'kdi_migrator' AND r.name=N'kdi_migrations') OR(u.name=N'kdi_app' AND r.name=N'kdi_application')))<>2
 OR (SELECT COUNT(*) FROM sys.database_role_members m JOIN sys.database_principals u ON u.principal_id=m.member_principal_id WHERE u.name IN(N'kdi_migrator',N'kdi_app'))<>2
 THROW 51000,'Dev SID/role mapping mismatch.',1;
SELECT DB_NAME() database_name,u.name grantee,p.class_desc,p.permission_name,p.state_desc FROM sys.database_permissions p JOIN sys.database_principals u ON u.principal_id=p.grantee_principal_id WHERE u.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner',N'kdi_migrations',N'kdi_application',N'kdi_tests',N'public') ORDER BY u.name,p.class,p.major_id,p.permission_name;
SELECT N'DEV_MAPPING_VERIFIED' result;
GO
USE [KulaniKDI_Test];
SELECT DB_NAME() database_name,u.name,u.type_desc,u.authentication_type_desc,CONVERT(varchar(170),u.sid,1) user_sid,CONVERT(varchar(170),s.sid,1) login_sid,r.name role_name
FROM sys.database_principals u LEFT JOIN sys.sql_logins s ON s.name=u.name
LEFT JOIN sys.database_role_members m ON m.member_principal_id=u.principal_id
LEFT JOIN sys.database_principals r ON r.principal_id=m.role_principal_id
WHERE u.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner');
IF (SELECT COUNT(*) FROM sys.database_principals WHERE name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner'))<>3
 THROW 51000,'Test user inventory mismatch.',1;
IF (SELECT COUNT(*) FROM sys.database_principals u JOIN sys.sql_logins s ON s.name=u.name AND s.sid=u.sid
 JOIN sys.database_role_members m ON m.member_principal_id=u.principal_id JOIN sys.database_principals r ON r.principal_id=m.role_principal_id
 WHERE u.type='S' AND u.authentication_type_desc='INSTANCE' AND ((u.name=N'kdi_migrator' AND r.name=N'kdi_migrations') OR(u.name=N'kdi_app' AND r.name=N'kdi_application') OR(u.name=N'kdi_test_runner' AND r.name=N'kdi_tests')))<>3
 OR (SELECT COUNT(*) FROM sys.database_role_members m JOIN sys.database_principals u ON u.principal_id=m.member_principal_id WHERE u.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner'))<>3
 THROW 51000,'Test SID/role mapping mismatch.',1;
SELECT DB_NAME() database_name,u.name grantee,p.class_desc,p.permission_name,p.state_desc FROM sys.database_permissions p JOIN sys.database_principals u ON u.principal_id=p.grantee_principal_id WHERE u.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner',N'kdi_migrations',N'kdi_application',N'kdi_tests',N'public') ORDER BY u.name,p.class,p.major_id,p.permission_name;
SELECT N'TEST_MAPPING_VERIFIED' result;
