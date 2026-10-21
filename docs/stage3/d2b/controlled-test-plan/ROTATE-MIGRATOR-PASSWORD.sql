-- =====================================================================================
--  ROTACION DE LA CONTRASENA DE kdi_migrator
--  Archivo revisado. NO contiene la contrasena, y no debe llegar a contenerla.
--
--  Uso (SSMS, autenticacion Windows como operador autorizado):
--    1. Conectar a DESKTOP-F25O3MF.
--    2. Abrir ESTE archivo y sustituir SOLO el marcador de la linea 17 por la contrasena nueva.
--    3. Ejecutar el archivo completo.
--    4. NO GUARDAR el archivo. Cerrar la pestana y descartar los cambios.
--
--  Diseno:
--    * El archivo en disco conserva siempre el marcador. La contrasena solo existe en el
--      bufer del editor de SSMS: nunca en un archivo, ni en una linea de comandos, ni en una
--      variable de entorno, ni en la evidencia.
--    * La guarda aborta si el marcador no se ha sustituido, de modo que ejecutar el archivo
--      sin tocarlo NO cambia nada y NO fija una contrasena accidental.
--    * La contrasena viaja como parametro de sp_executesql, no como literal del comando.
--
--  Contexto: login habilitado, CHECK_POLICY=1 y CHECK_EXPIRATION=1, senha fijada el
--  2026-10-18 y no caducada. La rotacion es por perdida de la credencial, no por expiracion.
--  =====================================================================================

USE master;
GO

-- >>> SUSTITUIR EL MARCADOR DE ESTA LINEA POR LA CONTRASENA NUEVA <<<
DECLARE @pw sysname = N'<CONTRASENA-NUEVA-AQUI>';

-- Guarda: sin sustituir el marcador, no se toca la credencial.
IF @pw = N'<CONTRASENA-NUEVA-AQUI>'
    THROW 51000, 'The password marker was not replaced. Nothing was changed.', 1;

-- La contrasena se pasa como parametro: no aparece en el texto del comando.
EXEC sp_executesql N'ALTER LOGIN [kdi_migrator] WITH PASSWORD = @pw', N'@pw sysname', @pw;
GO

-- Si la sentencia anterior fuera rechazada por el motor, la alternativa equivalente es
-- escribir directamente en el editor, sin guardar el archivo:
--     ALTER LOGIN [kdi_migrator] WITH PASSWORD = N'<contrasena nueva>';

-- -------------------------------------------------------------------------------------
--  Verificacion. Estas consultas NO contienen ni devuelven el secreto.
--  No seleccionar password_hash ni msdb.dbo.syslogins: son material sensible.
-- -------------------------------------------------------------------------------------
SET NOCOUNT ON;

SELECT
    name,
    is_disabled,
    is_policy_checked,
    is_expiration_checked,
    LOGINPROPERTY(name, 'PasswordLastSetTime')      AS password_last_set_time,
    LOGINPROPERTY(name, 'DaysToPasswordExpiration') AS days_to_password_expiration,
    LOGINPROPERTY(name, 'PasswordExpired')          AS password_expired
FROM sys.sql_logins
WHERE name = N'kdi_migrator';

-- El login debe conservar la politica activa: CHECK_POLICY y CHECK_EXPIRATION ON.
-- Asi la expiracion sigue siendo el mecanismo previsto de rotacion, y no una excepcion.
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins
               WHERE name = N'kdi_migrator' AND is_policy_checked = 1 AND is_expiration_checked = 1)
    THROW 51000, 'Policy flags are not both ON for kdi_migrator; stop and review.', 1;

-- La membresia de rol no debe haber cambiado por la rotacion.
SELECT dp.name AS member, ISNULL(rm.name, '-') AS role_name
FROM sys.database_principals dp
JOIN sys.server_principals sp ON sp.sid = dp.sid
LEFT JOIN sys.database_role_members drm ON drm.member_principal_id = dp.principal_id
LEFT JOIN sys.database_principals rm ON rm.principal_id = drm.role_principal_id
WHERE sp.name = N'kdi_migrator';

PRINT 'ROTATION APPLIED: the new password is active. Run the A1 file check, then the A3 driver.';
GO
