:ON ERROR EXIT
SET NOCOUNT ON;
-- CORRECTION ONLY: artifacts created by this D2-A session; never a general history repair.
-- Principal authorized this correction after the 55/56 integration run exposed invalid 12-digit IDs.
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() NOT IN (N'KulaniKDI_Dev',N'KulaniKDI_Test')
    THROW 51000, 'Unauthorized baseline correction target.', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) <> 3
    THROW 51000, 'Unexpected persistent objects; stop correction.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    EXECUTE AS USER = N'kdi_migration_witness';
    IF (SELECT COUNT(*) FROM [identity].[__EFMigrationsHistory] WITH (UPDLOCK,HOLDLOCK)) <> 1
        OR NOT EXISTS (SELECT 1 FROM [identity].[__EFMigrationsHistory] WHERE MigrationId=N'202610180001_InitialIdentity' AND ProductVersion=N'10.0.13')
        THROW 51000, 'Identity source history is not the exact session baseline.', 1;
    IF (SELECT COUNT(*) FROM [organizations].[__EFMigrationsHistory] WITH (UPDLOCK,HOLDLOCK)) <> 1
        OR NOT EXISTS (SELECT 1 FROM [organizations].[__EFMigrationsHistory] WHERE MigrationId=N'202610180001_InitialOrganizations' AND ProductVersion=N'10.0.13')
        THROW 51000, 'Organizations source history is not the exact session baseline.', 1;
    IF (SELECT COUNT(*) FROM [portfolio].[__EFMigrationsHistory] WITH (UPDLOCK,HOLDLOCK)) <> 1
        OR NOT EXISTS (SELECT 1 FROM [portfolio].[__EFMigrationsHistory] WHERE MigrationId=N'202610180001_InitialPortfolio' AND ProductVersion=N'10.0.13')
        THROW 51000, 'Portfolio source history is not the exact session baseline.', 1;
    UPDATE [identity].[__EFMigrationsHistory] SET MigrationId=N'20261018000100_InitialIdentity'
        WHERE MigrationId=N'202610180001_InitialIdentity' AND ProductVersion=N'10.0.13';
    IF @@ROWCOUNT <> 1 THROW 51000, 'Identity correction row count mismatch.', 1;
    UPDATE [organizations].[__EFMigrationsHistory] SET MigrationId=N'20261018000100_InitialOrganizations'
        WHERE MigrationId=N'202610180001_InitialOrganizations' AND ProductVersion=N'10.0.13';
    IF @@ROWCOUNT <> 1 THROW 51000, 'Organizations correction row count mismatch.', 1;
    UPDATE [portfolio].[__EFMigrationsHistory] SET MigrationId=N'20261018000100_InitialPortfolio'
        WHERE MigrationId=N'202610180001_InitialPortfolio' AND ProductVersion=N'10.0.13';
    IF @@ROWCOUNT <> 1 THROW 51000, 'Portfolio correction row count mismatch.', 1;
    COMMIT;
    REVERT;
    PRINT 'SESSION CORRECTION SUCCESS: exactly three baseline IDs normalized to EF14-digit format; no schema/table/data changes.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    IF USER_NAME()=N'kdi_migration_witness' REVERT;
    THROW;
END CATCH;
