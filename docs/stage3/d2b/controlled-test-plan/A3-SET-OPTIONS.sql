-- =====================================================================================
--  A3 - comprobacion de opciones SET de la sesion
--  Solo lectura. No crea, altera ni borra nada.
--
--  Por que existe: el paquete D2-B crea columnas calculadas PERSISTED
--  (identity.Users.IssuerBytes / SubjectBytes) e indices filtrados
--  (identity.Grants. WHERE [RevokedAtUtc] IS NULL). SQL Server exige para eso
--  QUOTED_IDENTIFIER ON en la sesion. El cliente sqlcmd arranca con OFF salvo que
--  se invoque con -I, y SSMS lo trae en ON por eso.
--
--  Con este OFF la ejecucion falla con Msg 1934 a mitad del CREATE TABLE, y la
--  transaccion por modulo revierte. Con ON, la migracion se aplica.
-- =====================================================================================
SET NOCOUNT ON;

IF SESSIONPROPERTY('QUOTED_IDENTIFIER') <> 1
    THROW 51000, 'QUOTED_IDENTIFIER is not ON in this session. The D2-B package requires it (persisted computed columns and filtered indexes). Use sqlcmd -I or SSMS.', 1;

SELECT
    SESSIONPROPERTY('QUOTED_IDENTIFIER') AS QUOTED_IDENTIFIER,
    SESSIONPROPERTY('ANSI_NULLS')        AS ANSI_NULLS,
    SESSIONPROPERTY('ANSI_WARNINGS')      AS ANSI_WARNINGS,
    SESSIONPROPERTY('ANSI_PADDING')       AS ANSI_PADDING,
    SESSIONPROPERTY('ARITHABORT')         AS ARITHABORT,
    SESSIONPROPERTY('CONCAT_NULL_YIELDS_NULL') AS CONCAT_NULL_YIELDS_NULL,
    SESSIONPROPERTY('NUMERIC_ROUNDABORT') AS NUMERIC_ROUNDABORT;

PRINT 'SET OPTIONS OK: the session can create persisted computed columns and filtered indexes.';
