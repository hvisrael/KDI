-- REVIEW ONLY. NOT EXECUTED. Requires express approval for each target database.
-- Execute separately in KulaniKDI_Test, then KulaniKDI_Dev only after authorization.
:ON ERROR EXIT
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized target.', 1;
IF DATABASE_PRINCIPAL_ID(N'kdi_application') IS NULL
    THROW 51000, 'Existing application role required.', 1;
BEGIN TRANSACTION;
GRANT SELECT, INSERT ON OBJECT::[identity].[Users] TO [kdi_application];
GRANT SELECT ON OBJECT::[identity].[PermissionCatalog] TO [kdi_application];
GRANT SELECT ON OBJECT::[identity].[Grants] TO [kdi_application];
GRANT SELECT, INSERT, UPDATE ON OBJECT::[identity].[Sessions] TO [kdi_application];
GRANT INSERT ON OBJECT::[identity].[AuditEntries] TO [kdi_application];
GRANT SELECT ON OBJECT::[organizations].[Organizations] TO [kdi_application];
GRANT SELECT ON OBJECT::[organizations].[OrganizationMemberships] TO [kdi_application];
COMMIT;
-- No schema grants, no Organizations AuditEntries grant, no ALTER ROLE, no login/user creation.
-- Retain existing DDL denies and kdi_tests unchanged (Test SELECT only; no Dev access).
