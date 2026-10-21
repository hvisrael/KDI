:ON ERROR EXIT
-- Run once with Windows administrative authentication against master.
-- Capture sqlcmd -e stdout/stderr as the DDL execution journal. No filesystem overrides.
SET NOCOUNT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    THROW 51000, 'Unauthorized SQL Server.', 1;
IF DB_NAME() <> N'master' THROW 51000, 'Provisioning requires master context.', 1;
IF DB_ID(N'KulaniKDI_Dev') IS NOT NULL OR DB_ID(N'KulaniKDI_Test') IS NOT NULL
    THROW 51000, 'An authorized database already exists: stop and obtain separate evaluation and authorization.', 1;
PRINT 'PRECHECK PASS: both authorized databases absent.';
CREATE DATABASE [KulaniKDI_Dev];
PRINT 'DDL SUCCESS: CREATE DATABASE KulaniKDI_Dev using instance default paths.';
CREATE DATABASE [KulaniKDI_Test];
PRINT 'DDL SUCCESS: CREATE DATABASE KulaniKDI_Test using instance default paths.';
ALTER DATABASE [KulaniKDI_Dev] SET COMPATIBILITY_LEVEL = 150;
ALTER DATABASE [KulaniKDI_Dev] SET RECOVERY SIMPLE;
ALTER DATABASE [KulaniKDI_Dev] SET AUTO_CLOSE OFF;
ALTER DATABASE [KulaniKDI_Dev] SET AUTO_SHRINK OFF;
ALTER DATABASE [KulaniKDI_Test] SET COMPATIBILITY_LEVEL = 150;
ALTER DATABASE [KulaniKDI_Test] SET RECOVERY SIMPLE;
ALTER DATABASE [KulaniKDI_Test] SET AUTO_CLOSE OFF;
ALTER DATABASE [KulaniKDI_Test] SET AUTO_SHRINK OFF;
PRINT 'DDL SUCCESS: compatibility 150, SIMPLE recovery, AUTO_CLOSE/OFF, AUTO_SHRINK/OFF on both databases.';
GO
USE [KulaniKDI_Dev];
GO
BEGIN TRY
    BEGIN TRANSACTION;
    EXEC(N'CREATE SCHEMA [identity] AUTHORIZATION [dbo];');
    EXEC(N'CREATE SCHEMA [organizations] AUTHORIZATION [dbo];');
    EXEC(N'CREATE SCHEMA [portfolio] AUTHORIZATION [dbo];');
    CREATE ROLE [kdi_migrations] AUTHORIZATION [dbo];
    CREATE ROLE [kdi_application] AUTHORIZATION [dbo];
    CREATE ROLE [kdi_tests] AUTHORIZATION [dbo];
    GRANT CONNECT TO [kdi_migrations];
    GRANT CREATE TABLE TO [kdi_migrations];
    GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, VIEW DEFINITION ON SCHEMA::[identity] TO [kdi_migrations];
    GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, VIEW DEFINITION ON SCHEMA::[organizations] TO [kdi_migrations];
    GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, VIEW DEFINITION ON SCHEMA::[portfolio] TO [kdi_migrations];
    GRANT CONNECT TO [kdi_application];
    DENY CREATE TABLE, CREATE VIEW, CREATE PROCEDURE, CREATE FUNCTION, CREATE SCHEMA TO [kdi_application];
    DENY ALTER ON SCHEMA::[identity] TO [kdi_application];
    DENY ALTER ON SCHEMA::[organizations] TO [kdi_application];
    DENY ALTER ON SCHEMA::[portfolio] TO [kdi_application];
    GRANT CONNECT TO [kdi_tests];
    GRANT SELECT ON SCHEMA::[identity] TO [kdi_tests];
    GRANT SELECT ON SCHEMA::[organizations] TO [kdi_tests];
    GRANT SELECT ON SCHEMA::[portfolio] TO [kdi_tests];
    CREATE USER [kdi_migration_witness] WITHOUT LOGIN;
    CREATE USER [kdi_application_witness] WITHOUT LOGIN;
    CREATE USER [kdi_test_witness] WITHOUT LOGIN;
    ALTER ROLE [kdi_migrations] ADD MEMBER [kdi_migration_witness];
    ALTER ROLE [kdi_application] ADD MEMBER [kdi_application_witness];
    ALTER ROLE [kdi_tests] ADD MEMBER [kdi_test_witness];
    COMMIT;
    PRINT 'DDL SUCCESS KulaniKDI_Dev: three schemas, three least-privilege roles and three WITHOUT LOGIN witnesses. Runtime Windows binding pending.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
USE [KulaniKDI_Test];
GO
BEGIN TRY
    BEGIN TRANSACTION;
    EXEC(N'CREATE SCHEMA [identity] AUTHORIZATION [dbo];');
    EXEC(N'CREATE SCHEMA [organizations] AUTHORIZATION [dbo];');
    EXEC(N'CREATE SCHEMA [portfolio] AUTHORIZATION [dbo];');
    CREATE ROLE [kdi_migrations] AUTHORIZATION [dbo];
    CREATE ROLE [kdi_application] AUTHORIZATION [dbo];
    CREATE ROLE [kdi_tests] AUTHORIZATION [dbo];
    GRANT CONNECT TO [kdi_migrations];
    GRANT CREATE TABLE TO [kdi_migrations];
    GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, VIEW DEFINITION ON SCHEMA::[identity] TO [kdi_migrations];
    GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, VIEW DEFINITION ON SCHEMA::[organizations] TO [kdi_migrations];
    GRANT ALTER, SELECT, INSERT, UPDATE, DELETE, REFERENCES, VIEW DEFINITION ON SCHEMA::[portfolio] TO [kdi_migrations];
    GRANT CONNECT TO [kdi_application];
    DENY CREATE TABLE, CREATE VIEW, CREATE PROCEDURE, CREATE FUNCTION, CREATE SCHEMA TO [kdi_application];
    DENY ALTER ON SCHEMA::[identity] TO [kdi_application];
    DENY ALTER ON SCHEMA::[organizations] TO [kdi_application];
    DENY ALTER ON SCHEMA::[portfolio] TO [kdi_application];
    GRANT CONNECT TO [kdi_tests];
    GRANT SELECT ON SCHEMA::[identity] TO [kdi_tests];
    GRANT SELECT ON SCHEMA::[organizations] TO [kdi_tests];
    GRANT SELECT ON SCHEMA::[portfolio] TO [kdi_tests];
    CREATE USER [kdi_migration_witness] WITHOUT LOGIN;
    CREATE USER [kdi_application_witness] WITHOUT LOGIN;
    CREATE USER [kdi_test_witness] WITHOUT LOGIN;
    ALTER ROLE [kdi_migrations] ADD MEMBER [kdi_migration_witness];
    ALTER ROLE [kdi_application] ADD MEMBER [kdi_application_witness];
    ALTER ROLE [kdi_tests] ADD MEMBER [kdi_test_witness];
    COMMIT;
    PRINT 'DDL SUCCESS KulaniKDI_Test: three schemas, three least-privilege roles and three WITHOUT LOGIN witnesses. Runtime Windows binding pending.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
