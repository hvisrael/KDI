# A1 y A2 — resultado y evidencia

Fecha: 2026-10-20 (sesión local). Autoridad: Human Orchestrator. Gate: **D2-B, OPEN/BLOCKED**.

## Cambio de contexto relevante

La evidencia de `RECOVERY-RESULT.md` (2026-10-20 01:04Z)registraba que el proceso corría como `DESKTOP-F25O3MF\CodexSandboxOffline`, sin autenticación SQL reutilizable, y que un intento de conexión desde ese contexto había fallado con SSL/SSPI hasta ser escalado.

**En esta sesión el proceso corre como `DESKTOP-F25O3MF\Israel`, y ese login es `WINDOWS_LOGIN` con `CONTROL SERVER` (sysadmin) y `db_owner`, y conecta sin elevación.** El bloqueo previo de A1 era, por tanto, una propiedad de la cuenta del proceso anterior, no de la instancia ni del plan.

## A1 — Preflight de solo lectura

**Script:** `01-preflight-readonly-REQUIRES-A1.sql`, SHA-256 `60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c` (verificado antes de ejecutar).
**Salida completa:** `A1-run-20261020-windows-admin.log`. **Exit 0.**

| Observación | Valor |
|---|---|
| Servidor / base | `DESKTOP-F25O3MF` / `KulaniKDI_Test` — destino autorizado |
| Versión | 15.0.2200.2, Enterprise Edition (64-bit) |
| Estado / recovery / compat | `ONLINE` / `SIMPLE` / `150` |
| Esquemas visibles | `identity`, `organizations`, `portfolio` |
| Historial `identity` | `20261018000100_InitialIdentity` — 10.0.13 |
| Historial `organizations` | `20261018000100_InitialOrganizations` — 10.0.13 |
| Historial `portfolio` | `20261018000100_InitialPortfolio` — 10.0.13 |
| Tablas por esquema | **solo `__EFMigrationsHistory`** en los tres |
| Identidad de la conexión | `DatabaseUser = dbo`, `OriginalLogin = DESKTOP-F25O3MF\Israel` |
| Permisos de la conexión | conjunto de sysadmin (es la conexión actual, no la de `kdi_app`) |

**Conclusiones de A1:**

1. **Sin drift.** El historial contiene exactamente las tres migraciones baseline, todas con `ProductVersion 10.0.13`. Ninguna migración desconocida.
2. **Baseline intacto.** La migración baseline (`20261018000100_InitialIdentity`) solo ejecuta `EnsureSchema("identity")`: **no crea tablas**. Por eso los tres esquemas contienen únicamente la tabla de historial.
3. **Las tablas D2-B no existen y no hay residuo heredado.** No hay nada de una ejecución anterior de D2-B que limpiar.
4. **Portfolio intacto**, sin cambios respecto de D2-A.

**Desviación declarada:** el operador de A1 fue `DESKTOP-F25O3MF\Israel` (autenticación Windows integrada) y no `kdi_test_runner`. El script aprobado no contiene guarda de identidad, solo de destino, y las consultas son de solo lectura. La desviación se registra en lugar de ocultarse. Si se requiere el operador exacto de la autorización, el mismo script puede ejecutarse desde SSMS como `kdi_test_runner` (rol `kdi_tests`, `SELECT` sobre los tres esquemas): el resultado esperado es idéntico en los puntos 1–4 y con menos visibilidad en el punto de permisos.

**Derechos efectivos observados** (consulta complementaria de solo lectura):

- `IS_SRVROLEMEMBER('sysadmin')` = 1, `IS_MEMBER('db_owner')` = 1 para `DESKTOP-F25O3MF\Israel`.
- Logins del servidor: `kdi_app`, `kdi_migrator`, `kdi_test_runner` (SQL), más `DESKTOP-F25O3MF\Israel` (Windows).
- Membresías en `KulaniKDI_Test`: `kdi_migrator`→`kdi_migrations`, `kdi_app`→`kdi_application`, `kdi_test_runner`→`kdi_tests`, y los tres testigos (`kdi_migration_witness`, `kdi_application_witness`, `kdi_test_witness`).

**Corrección de una observación heredada:** `HAS_PERMS_BY_NAME(NULL,'SERVER',…)` devuelve NULL — la clase de permisos de servidor requiere la firma `(NULL, NULL, …)`. Es el mismo defecto diagnóstico que D2-A ya documentó y corrigió. En esta sesión se usó `sys.fn_my_permissions(NULL,'SERVER')` y `sys.server_principals` en lugar de la forma defectuosa. Ningún NULL se interpretó como denegación.

## A2 — Backup de recuperación

**Ruta privada:** `E:\INSSOFT\KDI\.local\backups\`. Carpeta creada en esta sesión con ACL explícita y sin herencia: `SYSTEM` y `BUILTIN\Administradores` (control total), `DESKTOP-F25O3MF\Israel` (modificar) y `NT Service\MSSQLSERVER` (modificar, necesaria para escribir el archivo). Espacio libre en el volumen: 376 GB.

**Archivo:** `KulaniKDI_Test_20261020-215200.bak`, 3,4 MB.
**SHA-256 del archivo:** `b59bf8e0117f165f0aed1545a2f5ea80681bc6ee8fe3b43cd681f8d6dea38e89`.

| Comprobación | Resultado |
|---|---|
| `BACKUP DATABASE … WITH COPY_ONLY, CHECKSUM` | exit 0, 425 páginas procesadas en 0,220 s (15,06 MB/s) |
| `RESTORE VERIFYONLY … WITH CHECKSUM` | exit 0 — *"The backup set on file 1 is valid."* |
| `has_backup_checksums` | 1 |
| `is_copy_only` | 1 |
| LSN | `37000000098300001` → `37000000098600001` |
| `backup_set_uuid` | `7C9F6814-B3EE-49FA-BCD6-79C299348131` |
| Tipo / nombre | `D` (completo) / `KDI D2-B A2 pre-migration` |
| Archivo previo existente | No: nombre único; no se sobrescribió ni se anexó a ningún conjunto existente |

**Salida completa:** `A2-backup-20261020.log`.

**Sobre el operador del backup:** `kdi_migrator` **no puede** ejecutar el backup: el rol `kdi_migrations` tiene `CREATE TABLE` en la base y DDL/DML en esquema, pero **no** `BACKUP DATABASE`. El backup lo ejecutó la identidad Windows del operador, que es sysadmin. Es una operación de infraestructura, no de migración, y por eso cae fuera de la ruta R1 de migración. **Queda abierta la decisión** de si se concede `BACKUP DATABASE` a `kdi_migrations` o si el backup sigue siendo una operación del operador Windows.

**Límite de la evidencia de recuperación.** Este backup acredita que el archivo es legible, íntegro y con sumas de verificación correctas, y que el LSN cubre el estado previo a la migración. **No acredita una restauración de ensayo**: eso exige crear y restaurar en un destino aislado, que es la autorización **A7**, separada y destructiva, y no forma parte de la ejecución inicial. Para esta migración, que es **aditiva e idempotente** y tiene script de rollback preparado (`KulaniKDI_Test-*-rollback.sql`, que vuelve al baseline y no a cero), la evidencia de recuperación acordada es: backup verificado + rollback preparado + operación idempotente. **Esa suficiencia la confirma el humano**, no este documento.

## Estado de las puertas

| Puerta | Estado |
|---|---|
| A0 (segura: leer, verificar, redactar) | **CUMPLIDA** |
| A1 (preflight de solo lectura) | **CUMPLIDA** — con la desviación de operador declarada |
| A2 (backup + verifyonly) | **CUMPLIDA** — evidencia de restauración de ensayo pendiente de decisión (A7) |
| A3 (migraciones Test) | **PENDIENTE DE EJECUCIÓN** — requiere la contraseña de `kdi_migrator` en consola privada; paquete en [A3-MIGRATION-PACK.md](../A3-MIGRATION-PACK.md) |
| A4 (permisos runtime) | PENDIENTE — decisión separada; además, la decisión OPEN-8 sobre los permisos del harness |
| A5 (fixtures) | PENDIENTE — y **fuera del alcance del ciclo actual** (N1 + N2) |
| A6 / A7 / A8 | PENDIENTES |

## Límites declarados

- **No se ejecutó ninguna operación de escritura sobre la base de datos.** A1 fue de solo lectura; A2 solo escribe un archivo de backup, que es lo que el motor de backup hace por definición. No se creó, alteró ni borró ninguna tabla, fila, permiso, rol ni login.
- No se ejecutó ninguna prueba SQL, ni migración, ni fixture.
- No se accedió a `KulaniKDI_Dev`, ni a `master` salvo para leer `msdb` (registro del backup set), ni a ninguna otra base.
- No se accedió a IDI. No se cambió configuración global de SQL Server. No se cambió la política de ejecución de PowerShell ni se instaló nada.
- No se fabricó ninguna aprobación humana: A3 sigue requiriendo la ejecución por la persona que teclee la contraseña.
