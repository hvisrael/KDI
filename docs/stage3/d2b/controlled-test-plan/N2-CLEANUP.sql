-- =====================================================================================
--  N2 - limpieza del fixture de demostracion de administracion
--  REVISADO. Idempotente. Destino unico: KulaniKDI_Test.
--
--  Que limpia: EXCLUSIVAMENTE las filas creadas por la demostracion N2, identificadas por
--  sus identificadores exactos (un usuario y una organizacion sinteticos). No hay borrado
--  por actor, ni por prefijo, ni por tipo: todo lleva el id exacto del fixture.
--
--  Que NO toca: cualquier otra fila de la base. Los identificadores estan fijados en este
--  archivo y coinciden con los de la semilla (A3-SEED-USER.sql) y con la request
--  (n2-request-bootstrap.json). Si esos ids no son los esperados, el script se detiene.
--
--  Orden: respeta las claves foraneas (Users <- Sessions/Grants, Organizations <- Memberships).
--
--  Ejecucion: como kdi_migrator (rol kdi_migrations, que tiene DELETE en los tres esquemas):
--      sqlcmd -S DESKTOP-F25O3MF -d KulaniKDI_Test -U kdi_migrator -C -I -l 10 -t 60 -b -W ^
--             -i N2-CLEANUP.sql
-- =====================================================================================

:ON ERROR EXIT
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized cleanup target.', 1;

IF OBJECT_ID(N'[identity].[Users]') IS NULL
    THROW 51000, 'A3 not applied; nothing to clean.', 1;

DECLARE @UserId uniqueidentifier = N'6F1B0A4C-2D7E-4A55-9C31-8B0E5D7A1C90';
DECLARE @OrgId  uniqueidentifier = N'3A7C5E21-9B4D-4F18-8C63-2D0E7A91B5F4';
DECLARE @OrgCode nvarchar(32) = N'TEST_N2_ACME';

-- Guarda de alcance: si existe una organizacion con ese id pero con OTRO codigo, el fixture
-- no es el esperado y se detiene en vez de borrar filas ajenas.
IF EXISTS (SELECT 1 FROM [organizations].[Organizations] WHERE [Id] = @OrgId AND [Code] <> @OrgCode)
    THROW 51000, 'The organization id exists with a different code; this is not the N2 fixture. Stop.', 1;

IF EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @UserId AND [Subject] <> N'kdi-n2-bootstrap-user')
    THROW 51000, 'The user id exists with a different subject; this is not the N2 fixture. Stop.', 1;

BEGIN TRANSACTION;

-- 1. Grants del usuario o de la organizacion del fixture.
DELETE FROM [identity].[Grants] WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId;

-- 2. Sesiones del usuario del fixture.
DELETE FROM [identity].[Sessions] WHERE [UserId] = @UserId;

-- 3. Auditoria de identity: las filas de la administracion llevan OrganizationId de la
--    organizacion del fixture, y la fila User.Created lleva TargetId del usuario.
--    Se cubre por OrganizationId y por TargetId, nunca por ActorId (que seria un actor fijo).
DELETE FROM [identity].[AuditEntries]
WHERE [OrganizationId] = @OrgId OR [TargetId] = @UserId OR [ActorId] = CONVERT(nvarchar(184), @UserId);

-- 4. Auditoria de organizations de la organizacion del fixture.
DELETE FROM [organizations].[AuditEntries] WHERE [OrganizationId] = @OrgId;

-- 5. Membresias del usuario o de la organizacion del fixture.
DELETE FROM [organizations].[OrganizationMemberships]
WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId;

-- 6. Organizacion del fixture.
DELETE FROM [organizations].[Organizations] WHERE [Id] = @OrgId;

-- 7. Usuario del fixture.
DELETE FROM [identity].[Users] WHERE [Id] = @UserId;

COMMIT;

-- Verificacion explicita: los conteos se muestran aunque sean cero.
SELECT 'identity.Users' AS table_name, COUNT_BIG(*) AS rows_left FROM [identity].[Users] WHERE [Id] = @UserId
UNION ALL SELECT 'identity.Grants', COUNT_BIG(*) FROM [identity].[Grants] WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId
UNION ALL SELECT 'identity.Sessions', COUNT_BIG(*) FROM [identity].[Sessions] WHERE [UserId] = @UserId
UNION ALL SELECT 'identity.AuditEntries', COUNT_BIG(*) FROM [identity].[AuditEntries] WHERE [OrganizationId] = @OrgId OR [TargetId] = @UserId OR [ActorId] = CONVERT(nvarchar(184), @UserId)
UNION ALL SELECT 'organizations.Organizations', COUNT_BIG(*) FROM [organizations].[Organizations] WHERE [Id] = @OrgId
UNION ALL SELECT 'organizations.OrganizationMemberships', COUNT_BIG(*) FROM [organizations].[OrganizationMemberships] WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId
UNION ALL SELECT 'organizations.AuditEntries', COUNT_BIG(*) FROM [organizations].[AuditEntries] WHERE [OrganizationId] = @OrgId;

IF EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @UserId)
   OR EXISTS (SELECT 1 FROM [organizations].[Organizations] WHERE [Id] = @OrgId)
   OR EXISTS (SELECT 1 FROM [identity].[Grants] WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId)
   OR EXISTS (SELECT 1 FROM [organizations].[OrganizationMemberships] WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId)
    THROW 51000, 'Cleanup left rows behind; the counts above name them. Stop and evaluate.', 1;

PRINT 'N2 CLEANUP OK: no residue of the demonstration fixture.';
