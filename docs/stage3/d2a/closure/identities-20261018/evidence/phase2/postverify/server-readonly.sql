-- Supplemental read-only observation. No login/password operations or persistent DDL/DML.
-- EXECUTE AS is session-scoped impersonation, not independent SQL authentication.
SET NOCOUNT ON;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
 OR DB_NAME() <> N'master' OR IS_SRVROLEMEMBER('sysadmin') <> 1
 THROW 51000, 'Fixed target/admin visibility precondition failed.',1;
SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) server_name,DB_NAME() database_name,
 CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly')) integrated_only,SYSUTCDATETIME() observed_utc;
SELECT name,type_desc,is_disabled,is_policy_checked,is_expiration_checked,default_database_name
FROM sys.sql_logins WHERE name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner');
SELECT p.name member_name,r.name server_role
FROM sys.server_role_members m JOIN sys.server_principals p ON p.principal_id=m.member_principal_id
JOIN sys.server_principals r ON r.principal_id=m.role_principal_id
WHERE p.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner');
SELECT p.name grantee,dp.class_desc,dp.permission_name,dp.state_desc,dp.major_id
FROM sys.server_permissions dp JOIN sys.server_principals p ON p.principal_id=dp.grantee_principal_id
WHERE p.name IN(N'kdi_migrator',N'kdi_app',N'kdi_test_runner',N'public') ORDER BY p.name,dp.class,dp.major_id,dp.permission_name;
GO
DECLARE @impersonated bit=0;
BEGIN TRY
 EXECUTE AS LOGIN=N'kdi_migrator'; SET @impersonated=1;
 SELECT N'SESSION_IMPERSONATION_NOT_AUTHENTICATION' evidence_type,SUSER_SNAME() effective_login,
 HAS_PERMS_BY_NAME(NULL,N'SERVER',N'CONTROL SERVER') legacy_control_server,
 HAS_PERMS_BY_NAME(NULL,NULL,N'CONTROL SERVER') control_server,
 HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY LOGIN') alter_any_login,
 HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY SERVER ROLE') alter_any_server_role;
 IF ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'CONTROL SERVER'),-1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY LOGIN'),-1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY SERVER ROLE'),-1)<>0
 THROW 51000,'Unexpected or unverified server permission.',1;
 REVERT; SET @impersonated=0;
END TRY BEGIN CATCH IF @impersonated=1 REVERT; THROW; END CATCH;
GO
DECLARE @impersonated bit=0;
BEGIN TRY
 EXECUTE AS LOGIN=N'kdi_app'; SET @impersonated=1;
 SELECT N'SESSION_IMPERSONATION_NOT_AUTHENTICATION' evidence_type,SUSER_SNAME() effective_login,
 HAS_PERMS_BY_NAME(NULL,N'SERVER',N'CONTROL SERVER') legacy_control_server,
 HAS_PERMS_BY_NAME(NULL,NULL,N'CONTROL SERVER') control_server,
 HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY LOGIN') alter_any_login,
 HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY SERVER ROLE') alter_any_server_role;
 IF ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'CONTROL SERVER'),-1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY LOGIN'),-1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY SERVER ROLE'),-1)<>0
 THROW 51000,'Unexpected or unverified server permission.',1;
 REVERT; SET @impersonated=0;
END TRY BEGIN CATCH IF @impersonated=1 REVERT; THROW; END CATCH;
GO
DECLARE @impersonated bit=0;
BEGIN TRY
 EXECUTE AS LOGIN=N'kdi_test_runner'; SET @impersonated=1;
 SELECT N'SESSION_IMPERSONATION_NOT_AUTHENTICATION' evidence_type,SUSER_SNAME() effective_login,
 HAS_PERMS_BY_NAME(NULL,N'SERVER',N'CONTROL SERVER') legacy_control_server,
 HAS_PERMS_BY_NAME(NULL,NULL,N'CONTROL SERVER') control_server,
 HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY LOGIN') alter_any_login,
 HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY SERVER ROLE') alter_any_server_role;
 IF ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'CONTROL SERVER'),-1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY LOGIN'),-1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,N'ALTER ANY SERVER ROLE'),-1)<>0
 THROW 51000,'Unexpected or unverified server permission.',1;
 REVERT; SET @impersonated=0;
END TRY BEGIN CATCH IF @impersonated=1 REVERT; THROW; END CATCH;
GO
IF IS_SRVROLEMEMBER('sysadmin')<>1 THROW 51000,'REVERT not restored.',1;
SELECT N'SUPPLEMENT_SERVER_PERMISSIONS_VERIFIED' result,SUSER_SNAME() restored_windows_admin;
