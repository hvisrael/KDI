-- =====================================================================================
--  N2 - verificacion del resultado de la demostracion de administracion
--  SOLO LECTURA. No escribe nada.
--
--  Comprueba el estado esperado tras un 'bootstrap' exitoso de la organizacion
--  TEST_N2_ACME para el usuario semilla, con el rol Administrator.
-- =====================================================================================
:ON ERROR EXIT
SET NOCOUNT ON;

IF CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) <> N'DESKTOP-F25O3MF'
    OR DB_NAME() <> N'KulaniKDI_Test'
    THROW 51000, 'Unauthorized verification target.', 1;

DECLARE @UserId uniqueidentifier = N'6F1B0A4C-2D7E-4A55-9C31-8B0E5D7A1C90';
DECLARE @OrgId  uniqueidentifier = N'3A7C5E21-9B4D-4F18-8C63-2D0E7A91B5F4';

PRINT '=== 1. Organizacion creada ===';
SELECT [Id], [Code], [Name], [IsActive], [Version] FROM [organizations].[Organizations] WHERE [Id] = @OrgId;

PRINT '=== 2. Membresia (usuario activo en la organizacion) ===';
SELECT [Id], [OrganizationId], [UserId], [IsActive] FROM [organizations].[OrganizationMemberships]
WHERE [OrganizationId] = @OrgId OR [UserId] = @UserId;

PRINT '=== 3. Grants del rol Administrator (deben ser los 8 codigos del catalogo) ===';
SELECT g.[ScopeType], g.[ScopeId], g.[PermissionCode],
       CASE WHEN g.[RevokedAtUtc] IS NULL THEN 'activo' ELSE 'revocado' END AS estado
FROM [identity].[Grants] g
WHERE g.[UserId] = @UserId OR g.[OrganizationId] = @OrgId
ORDER BY g.[PermissionCode];

PRINT '=== 4. Conteo de grants por codigo (deben ser 8 y en ningun caso mas de 1) ===';
SELECT [PermissionCode], COUNT_BIG(*) AS grants
FROM [identity].[Grants]
WHERE [UserId] = @UserId OR [OrganizationId] = @OrgId
GROUP BY [PermissionCode]
ORDER BY [PermissionCode];

PRINT '=== 5. Los ocho codigos del catalogo deben estar cubiertos exactamente una vez ===';
IF (SELECT COUNT_BIG(*) FROM [identity].[PermissionCatalog]) <> 8
    THROW 51000, 'The permission catalog does not hold the eight expected codes.', 1;
IF (SELECT COUNT_BIG(*) FROM [identity].[Grants] g
    WHERE (g.[UserId] = @UserId OR g.[OrganizationId] = @OrgId)
      AND g.[RevokedAtUtc] IS NULL) <> 8
    THROW 51000, 'The administrator role does not hold exactly eight active grants.', 1;
IF EXISTS (SELECT 1 FROM [identity].[Grants] g
           WHERE (g.[UserId] = @UserId OR g.[OrganizationId] = @OrgId)
             AND g.[PermissionCode] NOT IN (SELECT [Code] FROM [identity].[PermissionCatalog]))
    THROW 51000, 'A grant references a code outside the permission catalog.', 1;
PRINT 'OK: ocho grants activos, todos con codigo del catalogo.';

PRINT '=== 6. Auditoria escrita por la administracion ===';
SELECT [ActorKind], [ActorId], [Action], [TargetId], [OrganizationId], [AtUtc]
FROM [identity].[AuditEntries]
WHERE [OrganizationId] = @OrgId OR [TargetId] = @UserId
ORDER BY [AtUtc];
SELECT [ActorId], [Action], [TargetId], [OrganizationId], [AtUtc]
FROM [organizations].[AuditEntries]
WHERE [OrganizationId] = @OrgId
ORDER BY [AtUtc];

PRINT '=== 7. El usuario semilla no debe haber cambiado ===';
SELECT [Id], [Issuer], [Subject], [DisplayName], [IsActive], [CreatedAtUtc]
FROM [identity].[Users] WHERE [Id] = @UserId;

PRINT '=== 8. Invariante global: toda organizacion activa con administrador activo ===';
SELECT o.[Id] AS organization_id,
       CASE WHEN EXISTS (
            SELECT 1 FROM [organizations].[OrganizationMemberships] m
            JOIN [identity].[Users] u ON u.[Id] = m.[UserId]
            JOIN [identity].[Grants] g1 ON g1.[UserId] = u.[Id] AND g1.[OrganizationId] = o.[Id]
                 AND g1.[ScopeType] = N'Organization' AND g1.[ScopeId] = o.[Id]
                 AND g1.[PermissionCode] = N'members.manage' AND g1.[RevokedAtUtc] IS NULL
            JOIN [identity].[Grants] g2 ON g2.[UserId] = u.[Id] AND g2.[OrganizationId] = o.[Id]
                 AND g2.[ScopeType] = N'Organization' AND g2.[ScopeId] = o.[Id]
                 AND g2.[PermissionCode] = N'access.manage' AND g2.[RevokedAtUtc] IS NULL
            WHERE m.[OrganizationId] = o.[Id] AND m.[IsActive] = 1 AND u.[IsActive] = 1)
            THEN 'con administrador' ELSE 'SIN ADMINISTRADOR' END AS estado
FROM [organizations].[Organizations] o
WHERE o.[IsActive] = 1
ORDER BY o.[Id];

PRINT 'N2 VERIFICATION OK';
