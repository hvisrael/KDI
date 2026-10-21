# A3 — resultado de la aplicación de las migraciones D2-B

Fecha: 2026-10-21 (sesión local). Autoridad: Human Orchestrator. Gate: **D2-B, OPEN/BLOCKED** — A3 cierra, el gate no.

Candidato: `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos). Identidad de migración: `kdi_migrator` (rol `kdi_migrations`), por la ruta R1 de menor privilegio.

## Artefactos aplicados

| # | Archivo | SHA-256 | Resultado |
|---|---|---|---|
| 1 | `KulaniKDI_Test-identity.sql` | `B18B5160371F20FDB8EAE6AD86E0973846E6219783F4A25348BADA35DD389CE3` | `MIGRATION SUCCESS KulaniKDI_Test/identity` |
| 2 | `KulaniKDI_Test-organizations.sql` | `5DD317EEF1257AD60368AD04140FA7E8437E8203E89090CB5BF7FE8700BAC63A` | `MIGRATION SUCCESS KulaniKDI_Test/organizations` |
| — | `A3-SEED-USER.sql` | revisado | `SEED OK: synthetic user created.` |

Los hashes coinciden con el manifiesto revisado (`prepared-sql/manifest.json`, 14 artefactos) y con los que aprobó la revisión de seguridad. **Los scripts SQL no se modificaron.**

## Verificación de solo lectura (exit 0)

| Comprobación | Resultado |
|---|---|
| Historial `identity` | `20261018000100_InitialIdentity` + `20261019040513_Accessidentity`, ambos `10.0.13` |
| Historial `organizations` | `20261018000100_InitialOrganizations` + `20261019040514_Accessorganizations`, ambos `10.0.13` |
| Historial `portfolio` | `20261018000100_InitialPortfolio` — **intacto** |
| Tablas | `identity.{AuditEntries, PermissionCatalog, Users, Grants, Sessions}` + `organizations.{AuditEntries, Organizations, OrganizationMemberships}` — las 8 presentes |
| Semilla de permisos | los 8 códigos: `access.manage`, `audit.read`, `clients.manage`, `clients.read`, `members.manage`, `members.read`, `projects.manage`, `projects.read` |
| Índices | 6 en `identity` (2 únicos: `IX_Sessions_HandleHash`, `IX_Users_Issuer_Subject_…`, 1 filtrado: `IX_Grants_…_WHERE [RevokedAtUtc] IS NULL`) + 3 en `organizations` (2 únicos) |
| Restricciones | 4 FK (`Grants→PermissionCatalog`, `Grants→Users`, `Sessions→Users`, `OrganizationMemberships→Organizations`) + 3 CHECK (`CK_Grants_Scope`, `CK_Sessions_Times`, `CK_Organizations_Code`) |
| `rowversion` | presente en las 5 entidades con `Version` |
| Filas | `identity.Users` 1 (la semilla), `identity.AuditEntries` 1, `identity.PermissionCatalog` 8, **resto 0** |
| Evidencia | `evidence/A1/A3-verify-20261021-013049.log` (exit 0) y los tres logs de aplicación |

## Incidente 1 — `QUOTED_IDENTIFIER` (intento fallido, revertido)

El primer intento falló con `Msg 1934`: `CREATE TABLE failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'`.

**Causa:** el cliente. `sqlcmd` (ODBC) abre la sesión con `QUOTED_IDENTIFIER = 0`, y el paquete crea columnas calculadas **PERSISTED** (`identity.Users.IssuerBytes`/`SubjectBytes`) e índices **filtrados** (`identity.Grants` con `WHERE [RevokedAtUtc] IS NULL`), que exigen `ON`. SSMS abre con `ON` por eso: **la ruta SSMS nunca habría sufrido este fallo**. Los scripts SQL y sus hashes no estaban en causa.

**Prueba antes de reintentar**, con una transacción revertible: el mismo `CREATE TABLE` con la forma exacta de `Users` falla con `Msg 1934` sin `-I` y se crea y revierte con `-I` (`PROBE ROLLED BACK`). Sin residuo: 0 objetos, 0 tablas.

**Estado tras el fallo:** limpio. La transacción por módulo revirtió por completo: 0 tablas nuevas, 0 objetos, historial con una sola fila por módulo. No hubo estado parcial que evaluar.

**Corrección:** `-I` en las invocaciones de `sqlcmd` del driver, más un archivo `A3-SET-OPTIONS.sql` que el driver ejecuta antes de aplicar con **control negativo** (el mismo archivo debe fallar sin `-I` y pasar con `-I`).

## Incidente 2 — deriva del inventario de la raíz

Un fichero `sql.txt` creado en la raíz del workspace por el Human Orchestrator (ver la desviación DEV-1) rompió la verificación de D2-A: `tools/d2a-manifest.py` inventaría los archivos sueltos de la raíz, y `sql.txt` pasó a ser una 122.ª entrada frente a las 121 del manifiesto → `AssertionError: Candidate drift`, exit 1.

**Resuelto** retirando el fichero del árbol congelado. `verify.py` vuelve a dar exit 0 y el workspace no contiene credenciales.

## Límites declarados

- **No se ejecutó ninguna operación fuera del alcance**: solo lectura (A1, verificación), backup (A2), y la aplicación de los dos archivos revisados más la semilla.
- No se aplicó ningún `GRANT`: los permisos de `kdi_application` y `kdi_tests` siguen como estaban (`kdi_application` con DDL DENY, `kdi_tests` solo `SELECT`). **A4 sigue pendiente.**
- No se ejecutó ninguna prueba SQL, ni migración de EF por otra vía, ni fixture del harness, ni el CLI de administración.
- No se accede a `KulaniKDI_Dev` ni a `master` salvo lectura de `msdb` para el registro del backup.
- No se tocó IDI, ni la configuración global de SQL Server, ni la política de ejecución de PowerShell.
- **No se fabricó ninguna aprobación humana.** A3 se ejecutó con la autorización vigente y la contraseña introducida por el operador en su consola.
