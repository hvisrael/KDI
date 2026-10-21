# A1 — plantilla de registro de evidencia

Una ejecución por intento. Archivos nuevos bajo `docs/stage3/d2b/controlled-test-plan/evidence/A1/`. **No sobrescribir** un intento anterior.

---

## Identificación del intento

| Campo | Valor |
|---|---|
| Identificador del intento | `A1-intento-<n>-<AAAA-MM-DD>` |
| Fecha y hora UTC de la ejecución | `<YYYY-MM-DDTHH:MM:SSZ>` |
| Operator (nombre de la identidad Windows que hospeda la herramienta) | `<texto>` |
| Identidad SQL de la conexión | `kdi_test_runner` (debe coincidir) |
| Servidor | `DESKTOP-F25O3MF` |
| Base de datos | `KulaniKDI_Test` |
| Herramienta SQL y versión | `<texto>` |
| SQLCMD Mode activo | `<sí / no>` |
| Hash del script ejecutado | `60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c` |
| Hash del candidato D2-B | `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` |
| Comprobación previa de archivos | `A1-FILE-CHECK.ps1` → `<OK / detalle del fallo>` |

## Resultado observado

Marcar solo lo observado. Si no se pudo comprobar algo, escribir `NO VERIFICADO`; no dejar en blanco ni suponer.

| Comprobación | Valor observado |
|---|---|
| `DB_NAME()` | `<texto>` |
| `SERVERPROPERTY('ServerName')` | `<texto>` |
| `SERVERPROPERTY('ProductVersion')` | `<texto>` |
| `SERVERPROPERTY('Edition')` | `<texto>` |
| `compatibility_level` | `<número>` |
| `recovery_model_desc` | `<texto>` |
| `state_desc` | `<texto>` |
| `USER_NAME()` | `<texto>` |
| `ORIGINAL_LOGIN()` | `<texto>` |
| Esquemas visibles (`identity`, `organizations`, `portfolio`) | `<lista>` |
| Historia de migraciones `identity` | `<MigrationId / ProductVersion>` |
| Historia de migraciones `organizations` | `<MigrationId / ProductVersion>` |
| Historia de migraciones `portfolio` | `<MigrationId / ProductVersion>` |
| Tablas visibles por módulo | `<lista>` |
| Permisos de la conexión actual (`sys.fn_my_permissions`) | `<lista>` |

> Los permisos listados son **los de la conexión actual**. No acreditan los privilegios efectivos de `kdi_app` ni de `kdi_application`, y no sustituyen la medición prevista en A4.

## Guardas y desviaciones

| Item | Estado | Observación |
|---|---|---|
| ¿Se ejecutó el archivo completo y una sola vez? | `<sí / no>` | |
| ¿Alguna guarda `THROW 51000` se activó? | `<sí / no>` | `<mensaje literal>` |
| ¿`ORIGINAL_LOGIN` = `kdi_test_runner`? | `<sí / no>` | |
| ¿`USER_NAME` corresponde al mapping esperado? | `<sí / no>` | |
| ¿Hubo errores o advertencias en Messages? | `<sí / no>` | `<texto literal, sin datos ajenos>` |
| ¿Salida parcial? | `<sí / no>` | |

## Archivos de evidencia adjuntos

| Archivo | Contenido | Hash SHA-256 |
|---|---|---|
| `<nombre>` | Resultados de este lote | `<hash>` |
| `<nombre>` | Mensajes de este lote | `<hash>` |

## Dictamen del operador (no es QA)

Ejecución completada: `<sí / no>`
Observación principal: `<texto>`
¿Se detiene la ejecución aquí? `<sí / no>` — si hay cualquier desviación, **sí**.

## Límites de esta evidencia

- Solo metadatos autorizados. No incluye tickets, handles, identidades reales, datos de aplicación ni datos ajenos al lote.
- No incluye configuración de conexión, contraseñas, procesos completos ni volcado de tablas.
- No acredita snapshot completo, privilegios efectivos runtime, capacidad de backup ni ausencia de drift.
- No constituye PASS de A1. El dictamen de A1 lo emiten QA y Code Review de forma independiente sobre esta salida real.

---

Revisión de preparación y límites: [GO-NO-GO.md](../GO-NO-GO.md). Estado canónico: [docs/stage3/STATE.md](../../../STATE.md).
