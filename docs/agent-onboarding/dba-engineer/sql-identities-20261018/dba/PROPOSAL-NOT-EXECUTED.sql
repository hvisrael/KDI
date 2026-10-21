-- PROPUESTA NO EJECUTADA. REQUIERE AUTORIZACION HUMANA ESPECIFICA.
-- Destino fijo DESKTOP-F25O3MF / MSSQLSERVER. No cambiar configuracion global.
-- Las siguientes tres sentencias son plantillas no ejecutables: el secreto se inserta
-- exclusivamente en memoria por consola privada despues de autorizacion. Nunca sqlcmd -e.
CREATE LOGIN [kdi_migrator] WITH PASSWORD = <SECURE_IN_MEMORY>, CHECK_POLICY = ON, CHECK_EXPIRATION = ON, DEFAULT_DATABASE = [KulaniKDI_Dev];
CREATE LOGIN [kdi_app] WITH PASSWORD = <SECURE_IN_MEMORY>, CHECK_POLICY = ON, CHECK_EXPIRATION = ON, DEFAULT_DATABASE = [KulaniKDI_Dev];
CREATE LOGIN [kdi_test_runner] WITH PASSWORD = <SECURE_IN_MEMORY>, CHECK_POLICY = ON, CHECK_EXPIRATION = ON, DEFAULT_DATABASE = [KulaniKDI_Test];
-- No server role membership and no server GRANT.
USE [KulaniKDI_Dev];
CREATE USER [kdi_migrator] FOR LOGIN [kdi_migrator] WITH DEFAULT_SCHEMA = [dbo];
ALTER ROLE [kdi_migrations] ADD MEMBER [kdi_migrator];
CREATE USER [kdi_app] FOR LOGIN [kdi_app] WITH DEFAULT_SCHEMA = [dbo];
ALTER ROLE [kdi_application] ADD MEMBER [kdi_app];
-- Intentionally no kdi_test_runner user in Dev.
USE [KulaniKDI_Test];
CREATE USER [kdi_migrator] FOR LOGIN [kdi_migrator] WITH DEFAULT_SCHEMA = [dbo];
ALTER ROLE [kdi_migrations] ADD MEMBER [kdi_migrator];
CREATE USER [kdi_app] FOR LOGIN [kdi_app] WITH DEFAULT_SCHEMA = [dbo];
ALTER ROLE [kdi_application] ADD MEMBER [kdi_app];
CREATE USER [kdi_test_runner] FOR LOGIN [kdi_test_runner] WITH DEFAULT_SCHEMA = [dbo];
ALTER ROLE [kdi_tests] ADD MEMBER [kdi_test_runner];
