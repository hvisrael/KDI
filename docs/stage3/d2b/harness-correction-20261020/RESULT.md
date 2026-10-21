# Resultado — corrección del harness de fixtures D2-B

Fecha: 2026-10-20 (sesión local). Autoridad: Human Orchestrator. Gate: **D2-B, OPEN/BLOCKED**.

Candidato final: **`d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb`**, 195 archivos.

## Objetivo

Corregir los defectos F1–F7 del harness de pruebas SQL que el QA independiente dejó documentados como **condición previa de A5** en `docs/stage3/d2b/controlled-test-plan/QA-READINESS.md` §3–§4 y `PLAN.md` §7, y que el code-reviewer de preparación describió como riesgos de residuos y de limpieza no garantizada.

## Resultado

Los siete defectos están **corregidos o documentados como no corregibles sin decisión de plan**. El candidato fue revisado de forma independiente por un code-reviewer y un qa-engineer en sesiones separadas, sobre el mismo hash, con la traducción EF verificada contra el fichero real y **sin abrir ninguna conexión a SQL Server**.

| Defecto | Estado | Nota |
|---|---|---|
| F1 · auditoría huérfana de `user-state`/`sessions-revoke` | **CORREGIDO** | Ámbito por caso en unión `OR`; la fila con `OrganizationId` null queda cubierta por la vía `TargetId` |
| F2 · limpieza con el token del test | **CORREGIDO** | Token propio con presupuesto de 120 s |
| F3 · sentencia fallida aborta las siguientes; IDs solo en memoria | **CORREGIDO** | Sentencias aisladas + artefacto JSON de recuperación con escritura atómica |
| F4 · limpieza sin verificación | **CORREGIDO en verificación, NO en atomicidad** | 7 conteos por tabla con el mismo alcance del `DELETE`. La atomicidad es decisión de plan (MEDIO-3) |
| F5 · organización temporal de acceso interfiere con la invariante global | **MITIGADO Y DIAGNOSTICADO** | Foto previa antes de mutar + mensaje accionable. No prevenible sin romper una aserción del caso de acceso |
| F6 · `SqlBaselineTests` sin guarda propia | **CORREGIDO** | Guarda distinta y explícita; ninguna aserción eliminada ni debilitada |
| F7 · intersección `AND` en lugar de unión `OR` | **CORREGIDO** | Verificado con SQL generado por EF real |

## Un defecto que solo apareció al revisar el código

La primera entrega **afirmaba corregir F1 y no lo corregía**: encadenaba tres `.Where()` sucesivos, que en LINQ es intersección, no unión. Dejaba sin limpiar exactamente las filas de `user-state` y `sessions-revoke`, y en la clase de acceso dejaba de borrar **ninguna** fila de auditoría — una regresión respecto al original. El defecto fue detectado por el principal al releer el código y devuelto con la reproducción. Está documentado en [CODE-REVIEW-DELTA.md](CODE-REVIEW-DELTA.md).

## Evidencia

| Comprobación | Resultado | Fuente |
|---|---|---|
| Build backend Release | 0 errores, **0 advertencias** | QA |
| Unitarias | **57/57**, 0 fallos, 0 omitidas | QA (dos rondas) |
| API | **20/20**, 0 fallos, 0 omitidas | QA (dos rondas) |
| Arquitectura | **24/24**, 0 fallos, 0 omitidas | QA (dos rondas) |
| Frontend typecheck / lint / build | exit 0 en los tres; hashes de artefacto idénticos a la referencia | QA |
| Navegador Chromium | **11/11**, 0 fallos, 0 flaky | QA |
| Guardas del harness SQL | 8/8 con `BLOCKED` literal, **sin intento de conexión** (4 pruebas concordantes) | QA |
| Traducción EF | 11 traducciones: unión `OR`, términos vacíos eliminados, `WHERE 0 = 1`, **literal del operador ausente en las 11** | QA |
| Ámbito de los 14 `DELETE` | Ninguno alcanza filas ajenas a la corrida | Code review + QA |
| Integridad del candidato | `freeze.py --verify` exit 0 antes y después de cada ronda; hash estable | QA |
| Árbol de la raíz | `verify.py` de D2-A exit 0 antes y después | QA |
| `src/` | 66 entradas, **0 alteradas** | Principal + code review |

## Lo que queda bloqueado

**No se declara PASS de D2-B.** Sigue bloqueado, y ahora con un motivo nuevo y documentado:

1. **A1** — identidad SQL autenticada reutilizable. Bloqueo de operador, no de autorización.
2. **A5** — fixtures, DML y limpieza. No concedida.
3. **Permisos efectivos** — y un hallazgo de plan nuevo: el rol `kdi_tests` de `KulaniKDI_Test` **solo tiene `GRANT SELECT`** sobre los tres esquemas, y `SqlTarget` exige autenticación integrada, de modo que el harness se ejecutaría como la identidad Windows del proceso y **no** como `kdi_test_runner`. El harness necesita `INSERT` y `DELETE` en siete tablas. **Una corrida fallaría por permisos y podría dejar sesiones y auditoría huérfanas.** Bloqueante de plan, no de código: se ha consignado como advertencia conocida en el artefacto y en la salida de cada caso, sin tocar ningún permiso.
4. **MEDIO-3** — la limpieza administrativa no es atómica. Decisión de plan pendiente: aceptar con evidencia y recuperación manual, o variante transaccional.
5. **Auth0 / HTTPS / keyring / CLI del operador** — sin entorno ni autorización.
6. **Residuo histórico** con `ActorId = S-1-5-21-SYNTHETIC-TEST`: no es alcanzable por el código nuevo por diseño (borrarlo en bloque por actor fijo está prohibido). Comprobable en A1.

## Desviación de alcance respecto de lo previsto

El plan original de este frente_fix preveía cerrar el harness y pasar a QA. Fue necesario **un ciclo adicional de implementación y revisión** porque la primera entrega no corregía F1. El ciclo completo fue: implementación → verificación del principal → corrección → code-review independiente → corrección de sus hallazgos → QA independiente → corrección de sus observaciones → verificación delta de QA.

Cada ronda congeló su propio manifiesto y conservó el anterior, de modo que existe un diff verificable en cada punto. Es la primera vez que ese diff es exhaustivo en este gate.

## Límites declarados

- **No se ejecutó ninguna prueba SQL**, ni se abrió ninguna conexión a SQL Server. La única ejecución del harness fue la excepción acotada de guardas, con las tres variables de entorno verificadas vacías **antes** de ejecutar, y sin ningún error de conexión o autenticación.
- **No se tocó** ningún permiso, rol, login, witness, certificado, `SqlTarget`, `src/`, `.csproj`, `packages.lock.json` ni los interruptores `KDI_D2B_SQL_AUTHORIZED` y `KDI_SQL_BASELINE_AUTHORIZED`.
- **No se fabricó ninguna aprobación humana.**
- No se accedió a IDI, ni a otras bases, ni a configuración global de SQL Server.

## Entregables

| Documento | Contenido |
|---|---|
| [CODE-REVIEW.md](CODE-REVIEW.md) | Revisión independiente: 0 críticos, 0 altos, 3 medios, 8 bajos; veredicto por defecto y conclusión de seguridad del alcance de los borrados |
| [CODE-REVIEW-DELTA.md](CODE-REVIEW-DELTA.md) | Resolución hallazgo por hallazgo, el defecto `AND` interceptado, y la cadena de congelados |
| [QA.md](QA.md) | QA independiente: regresión, guardas, traducción EF, veredicto por criterio y hallazgo de plan de los permisos |
