# A4 — resultado de la concesión de permisos runtime

Fecha: 2026-10-21 (sesión local). Autoridad: Human Orchestrator. Gate: **D2-B, OPEN/BLOCKED** — A4 cierra, el gate no.

Candidato: `91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448` (197 archivos).
Artefacto aplicado: `KulaniKDI_Test-runtime-grants.sql`, SHA-256 `CB34B1ACDEBCE32163E37125B3A79BD9810A0EC0C2826A0C7DD63B6CE2C3C139` (manifiesto revisado de 14 artefactos).

## Concedido

10 permisos sobre 7 objetos, para el rol `kdi_application`:

| Esquema | Objeto | Permisos |
|---|---|---|
| `identity` | `Users` | SELECT, INSERT |
| `identity` | `PermissionCatalog` | SELECT |
| `identity` | `Grants` | SELECT |
| `identity` | `Sessions` | SELECT, INSERT, UPDATE |
| `identity` | `AuditEntries` | INSERT |
| `organizations` | `Organizations` | SELECT |
| `organizations` | `OrganizationMemberships` | SELECT |

## Verificación con aserciones duras (exit 0)

| Aserción | Resultado |
|---|---|
| B1 · el conjunto es **exactamente** el aprobado (comparación en los dos sentidos) | OK |
| B2 · `kdi_application` **no tiene `DELETE`** | OK |
| B3 · **sin** permisos sobre `organizations.AuditEntries` | OK |
| B4 · **sin** concesiones de esquema | OK |
| B5 · los seis `DENY` de DDL intactos | OK |
| C · `kdi_tests` **sigue siendo de solo lectura** | OK |
| D · membresías de rol y logins sin cambios | OK |
| E · `kdi_app` cumple las **nueve** condiciones del interceptor de runtime | OK |

**La verificación no era vacía**: ejecutada antes de conceder, fallaba nombrando los diez permisos que faltaban, uno por concesión aprobada. Está en `evidence/A1/A4-apply-20261021-031002.log` y en el registro previo.

## Quién concedió, y por qué

**La concesión la ejecutó la identidad del operador** (autenticación Windows integrada), no `kdi_migrator`.

Motivo, medido: los objetos D2-B **no tienen dueño**. `sys.objects.principal_id` es `NULL` en las 5 tablas de `identity` y en las 3 de `organizations` (y también en las tablas de D2-A). SQL Server solo permite conceder permisos sobre un objeto sin dueño a quien tenga `CONTROL` sobre él o pertenezca al rol fijo que lo posee. Con `kdi_migrator` el `GRANT` falla con:

```
Msg 15151, Level 16, State 1, Server DESKTOP-F25O3MF, Line 11
Cannot find the object 'Users', because it does not exist or you do not have permission.
```

Comprobado con una prueba reversible: la identidad del operador concede los 2 permisos de `identity.Users` y la transacción revierte sin residuo.

**El conjunto concedido es idéntico por ambas vías**: lo que cambia es quién lo concede, no qué se concede. Y los `DENY` de DDL de `kdi_application` no se ven afectados: `DENY` siempre prevalece sobre `GRANT` con independencia de quien conceda.

`kdi_migrator` conserva intactos sus permisos de migración. `kdi_tests` conserva `SELECT` y nada más.

## Snapshot antes y despues

| Momento | Permisos de objeto de `kdi_application` | Evidencia |
|---|---|---|
| Antes | **0** | `evidence/A1/A4-snapshot-antes-20261021.log` |
| Después | 10 sobre 7 objetos | `evidence/A1/A4-snapshot-despues-20261021.log` |

## Contexto que esto habilita

`kdi_app` **ya cumplía** las nueve condiciones del interceptor de privilegios del runtime antes de A4 (no es sysadmin, no es `db_owner`, es miembro de `kdi_application`, y no tiene `CONTROL`, `CREATE TABLE`, `CREATE SCHEMA` ni `ALTER` de esquema). A4 le da la lectura de la identidad y la pertenencia, y la escritura en las tres tablas que la propia API usa. **No abre la puerta al DDL.**

## Límites declarados

- **No se ha tocado** ningún otro permiso, rol, login, usuario, esquema ni objeto. `kdi_tests` sigue en solo lectura. Las membresías siguen siendo exactamente tres.
- No se ha ejecutado ninguna prueba SQL, ni el harness de fixtures, ni el CLI de administración.
- No se ha aplicado ningún permiso en `KulaniKDI_Dev`, `master` ni en ninguna otra base.
- No se ha creado ni modificado ningún objeto de SQL Server: solo hay `GRANT` sobre objetos existentes.
- La concesión no se ha hecho desde el modelo de `kdi_migrator` porque **no tiene autoridad** sobre objetos sin dueño; es una limitación real de la base, no una preferencia.
