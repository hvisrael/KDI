-- Usuario sintetico para la demostracion N2 (AccessAdmin CLI).
-- REVISADO. Idempotente. Destino unico: KulaniKDI_Test.
-- NO es parte de A3 (migraciones). Requiere su propia ejecucion, despues de A3.
--
-- Proposito: la tabla identity.Users queda VACIA tras A3 (la migracion baseline solo crea el
-- esquema). El comando 'bootstrap' del motor de administracion exige un usuario activo ya
-- existente, de modo que sin esta semilla el CLI no tiene nada que administrator.
-- Es un fixture sintetico minimo: un unico usuario, sin organizacion, sin membresia, sin grants.
--
-- Idempotencia: si el Issuer/Subject ya existe, no se inserta nada.
-- Reversibilidad: DELETE por TargetId exacto en identity.AuditEntries y por Id en identity.Users.
--                  No toca ningun otro registro: el Id y el Issuer/Subject son fijos y propios.

:ON ERROR EXIT
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized seed target.', 1;

IF OBJECT_ID(N'[identity].[Users]') IS NULL
    THROW 51000, 'A3 not applied: identity.Users does not exist; stop.', 1;

DECLARE @UserId uniqueidentifier = N'6F1B0A4C-2D7E-4A55-9C31-8B0E5D7A1C90';
DECLARE @Issuer nvarchar(512) = N'https://synthetic.invalid/';
DECLARE @Subject nvarchar(255) = N'kdi-n2-bootstrap-user';
DECLARE @DisplayName nvarchar(200) = N'Synthetic fixture user';
DECLARE @Correlation nvarchar(64) = N'kdi-n2-seed';
DECLARE @Now datetime2 = SYSUTCDATETIME();

IF EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Issuer] = @Issuer AND [Subject] = @Subject)
BEGIN
    PRINT 'SEED ALREADY PRESENT: nothing to do.';
    RETURN;
END;

IF EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @UserId)
    THROW 51000, 'Seed identifier already used by a different user; stop for evaluation.', 1;

BEGIN TRANSACTION;

INSERT INTO [identity].[Users] ([Id], [Issuer], [Subject], [DisplayName], [IsActive], [CreatedAtUtc])
VALUES (@UserId, @Issuer, @Subject, @DisplayName, 1, @Now);

INSERT INTO [identity].[AuditEntries] ([Id], [ActorKind], [ActorId], [Action], [TargetId], [OrganizationId], [AtUtc], [CorrelationId])
VALUES (NEWID(), N'User', @UserId, N'User.Created', @UserId, NULL, @Now, @Correlation);

COMMIT;

PRINT 'SEED OK: synthetic user created.';
SELECT [Id], [Issuer], [Subject], [DisplayName], [IsActive] FROM [identity].[Users] WHERE [Id] = @UserId;
