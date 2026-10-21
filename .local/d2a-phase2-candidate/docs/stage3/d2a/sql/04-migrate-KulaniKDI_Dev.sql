:ON ERROR EXIT
SET NOCOUNT ON;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF' OR DB_NAME() <> N'KulaniKDI_Dev'
    THROW 51000, 'Unauthorized migration wrapper target.', 1;
EXECUTE AS USER = N'kdi_migration_witness';
PRINT 'MIGRATION PRINCIPAL: kdi_migration_witness WITHOUT LOGIN; not independent Windows authentication.';
GO
:r docs/stage3/d2a/sql/generated/KulaniKDI_Dev-identity.sql
:r docs/stage3/d2a/sql/generated/KulaniKDI_Dev-organizations.sql
:r docs/stage3/d2a/sql/generated/KulaniKDI_Dev-portfolio.sql
REVERT;
GO
