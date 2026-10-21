# N2 — demostración real del motor de administración

**Estado: preparado, NO ejecutado.** Requiere la contraseña de `kdi_migrator` en consola privada (tres veces: `plan`, `apply`, limpieza).

## Qué demuestra

El motor `AccessAdministration` es el que gobierna identidades, organizaciones, membresías, roles y revocación. N2 lo ejecuta **contra la base real** con el CLI `Kulani.KDI.AccessAdmin`, que es la vía de operador privilegiado del producto:

- `plan` — genera un **recibo exacto del estado** (identificadores y versiones de filas) **sin mutar nada**: abre transacción, toma la foto y hace `ROLLBACK`. Imprime el `SHA-256` del plan.
- `apply` — exige el hash del plan, que el plan no haya caducado (30 minutos), que el **operador Windows sea el mismo** y que el estado no haya cambiado desde el `plan`. Después aplica el comando dentro de una transacción con `sp_getapplock` exclusivo (`KDI:IdentityAdmin`) y escribe auditoría.

Comandos disponibles: `bootstrap`, `membership`, `role`, `user-state`, `organization-state`, `sessions-revoke` y `purge`.

## Estado de partida (verificado, solo lectura)

| Elemento | Estado |
|---|---|
| Usuario semilla `6F1B0A4C-…-8B0E5D7A1C90` | existe, `IsActive = 1`, `Issuer = https://synthetic.invalid/`, `Subject = kdi-n2-bootstrap-user` |
| Grants del usuario | 0 |
| Organizaciones | 0 |
| Membresías | 0 |
| Auditoría `identity` | 1 (la fila `User.Created` de la semilla) |
| Auditoría `organizations` | 0 |
| Request `n2-request-bootstrap.json` | válida contra `AccessAdministration.Validate`: `bootstrap` con `UserId` y `OrganizationId`, `Code` conforme a `^[A-Z0-9_-]{1,32}$`, `Name` de 25 caracteres |

## Secuencia

En una **ventana de PowerShell interactiva** (no redirigida):

```powershell
$cli   = 'E:\INSSOFT\KDI\.local\d2b-candidate\tools\Kulani.KDI.AccessAdmin\bin\Release\net10.0\Kulani.KDI.AccessAdmin.exe'
$req   = 'E:\INSSOFT\KDI\docs\stage3\d2b\controlled-test-plan\n2-request-bootstrap.json'
$plan  = "$env:TEMP\kdi-n2-plan.json"

# 1. Plan: no muta nada. Imprime "Plan SHA256=<hash>".
& $cli plan KulaniKDI_Test $req $plan

# 2. Apply: sustituye <HASH> por el que ha impreso el paso 1.
& $cli apply $plan <HASH>

# 3. Limpieza del fixture (opcional; ver mas abajo).
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
& $sqlcmd -S DESKTOP-F25O3MF -d KulaniKDI_Test -U kdi_migrator -C -I -l 10 -t 60 -b -W `
    -i 'E:\INSSOFT\KDI\docs\stage3\d2b\controlled-test-plan\N2-CLEANUP.sql'
```

El `apply` sin el hash correcto, con el plan caducado, con otro operador o si el estado cambió entre `plan` y `apply`, **falla y no aplica nada**.

## Qué se espera tras el `apply`

`bootstrap` sobre la organización `TEST_N2_ACME` debe dejar:

| Objeto | Resultado esperado |
|---|---|
| `organizations.Organizations` | 1 fila, `Code = TEST_N2_ACME`, `IsActive = 1` |
| `organizations.OrganizationMemberships` | 1 fila, el usuario semilla, activa |
| `identity.Grants` | 8 filas — el rol `Administrator` concede los ocho permisos del catálogo, con `ScopeType = Organization` y `ScopeId = OrganizationId` |
| `identity.AuditEntries` | 1 fila `Admin.bootstrap` |
| `organizations.AuditEntries` | 1 fila `Admin.bootstrap` |

Y el motor valida, dentro de la misma transacción, que **toda** organización activa de la base conserva un administrador activo. Como la base solo tiene esta organización, la comprobación la recorre entera.

## Limpieza del fixture

`N2-CLEANUP.sql` está preparado y revisado. Borra **exclusivamente** las filas de la demostración, identificadas por sus identificadores exactos, en orden que respeta las claves foráneas, y termina con una verificación que muestra los conteos restantes (incluso cero) y falla si queda residuo.

**No borra por actor, ni por prefijo, ni por tipo.** Es la lección del trabajo del harness: un alcance de borrado mal acotado es tan peligroso como uno mal acotado por intersección.

Además tiene dos guardas de alcance: si el id de la organización existe con **otro** código, o el id del usuario existe con **otro** `Subject`, se detiene sin borrar nada. Esos ids están fijados en el fichero y coinciden con la semilla y con la request.

**Decisión pendiente de tu parte:** si la demostración se queda en la base como fixture sintético declarado, o se limpia. Por defecto, la disciplina del proyecto (A5) es limpiar y verificar cero residuos.

## Archivos de este frente

| Archivo | Contenido |
|---|---|
| `n2-request-bootstrap.json` | Request de arranque |
| `N2-CLEANUP.sql` | Limpieza idempotente del fixture, con guardas de alcance y verificación |
| `A3-SEED-USER.sql` | Semilla del usuario que exige `bootstrap` (ya ejecutada) |
| `A3-VERIFY.sql` | Verificación de solo lectura del estado de las tablas |
| `A3-SET-OPTIONS.sql` | Comprobación de opciones SET de la sesión |
