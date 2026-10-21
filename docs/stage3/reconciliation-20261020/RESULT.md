# Reconciliación documental y paquete A1 — TaskResult

TaskInput: `KDI-STAGE3-RECONCILIATION-20261020`. projectId `kdi`. Autoridad: Human Orchestrator. Fecha de sesión: 2026-10-20 (local).

## Objetivo

Cerrar los hallazgos de auditoría H1, H4, H5, H8 y H9 (índices desactualizados, evidencia duplicada sin nota, tablas de puertas divergentes, glosario de estados ausente, referencia ADR rota), preparar el paquete de operador para la puerta A1 y corregir los defectos de limpieza del harness de fixtures que bloquean A5.

## Fuentes leídas

`AGENTS.md` · `README.md` (raíz) · `docs/stage3/{STATUS,DECISIONS,FUNCTIONAL-ANALYSIS}.md` · `docs/stage3/d1/**` · `docs/stage3/d2a/closure/**` · `docs/stage3/d2b/**` (STATUS, preparation-20261020, continuation-20261020, controlled-test-plan con su evidencia A1) · `docs/api/README.md` · `docs/source/kdi/**` · `docs/agent-onboarding/**` · `tools/d2a-manifest.py` · `docs/stage3/d2b/preparation-20261020/freeze.py` · código del candidato `.local/d2b-candidate` (`tests/Kulani.KDI.SqlIntegrationTests/**`, `src/Kulani.KDI.Infrastructure/Access/**`, `src/Kulani.KDI.Infrastructure/Modules/Identity/Persistence/**`).

No se consultó IDI. No se abrió ninguna conexión SQL. No se ejecutó ninguna prueba SQL. No se modificaron manifiestos congelados.

## Delegaciones

| Identificador | agent_type efectivo | Tarea | Resultado |
|---|---|---|---|
| `/root` | principal | Integración, reconciliación documental, paquete A1, congelados, revisión del código entregado | Secciones 1–3 y 6 |
| (sesión de ingeniería) | `general` bajo rol `engineer` | Corrección del harness de fixtures (F1–F7), en cuatro rondas | Sección 4 |
| (sesión de revisión) | `general` bajo rol `code-reviewer` | Revisión independiente del harness sobre `80ff7c4c…` | [CODE-REVIEW.md](../d2b/harness-correction-20261020/CODE-REVIEW.md) |
| (sesión de QA) | `general` bajo rol `qa-engineer` | Validación independiente sobre `e73f02a8…` y verificación delta sobre `d106dac6…` | [QA.md](../d2b/harness-correction-20261020/QA.md) |
| (sesión de tooling) | `general` bajo rol `engineer` | Runner de validación portable | Sección 5 |

**Sobre el agent_type efectivo.** Los perfiles de Codex (`analyst-architect`, `engineer`, `qa-engineer`, `code-reviewer`, `dba-engineer`) están declarados en `.codex/agents/` pero **no son invocables como tal en este entorno**: el único tipo de subagente disponible es `general`. Conforme a AGENTS.md («no simular delegaciones», «identificar el agent_type efectivo»), cada delegación se registra con su tipo efectivo real (`general`) junto al rol funcional que ejerce. Ninguna salida de subagente se presenta como dictamen de QA o de revisión independiente: esos dictámenes pertenecen a sesiones separadas, y aquí constan como tales.

Máximo dos subagentes activos simultáneamente, respetado.

## 1. Frente 0 — Reconciliación documental

### Hallazgo previo, verificado empíricamente

Al examinar los manifiestos se comprobó que **el árbol de la raíz está congelado por hash**: `tools/d2a-manifest.py` construye el inventario del candidato D2-A original a partir de todos los archivos sueltos de la raíz y de todo el contenido de `src/`, `tests/`, `tools/`, `docs/api/`, `docs/source/` y `docs/stage3/d2a/`. Cualquier escritura ahí hace fallar `verify.py` con `AssertionError: Candidate drift`, y con ello invalida la afirmación «D1/D2-A intactos» que aparece en toda la evidencia de D2-B.

Esto **redefine el alcance de P0**: el `README.md` de la raíz, que afirma que «D2 (SQL/identidad) permanece pendiente», no puede corregirse en el sitio sin invalidar dos candidatos aceptados. La corrección pasa a ser una decisión de gobernanza (**OPEN-7**), no una edición.

### Incidente y reversión

Durante la redacción se editaron por error dos archivos congelados: el `README.md` de la raíz y `docs/stage3/STATUS.md`.

- **Detección:** `python docs/stage3/d2a/closure/identities-20261018/verify.py` → `AssertionError: Candidate drift`, exit 1. El fallo fue visible de inmediato porque el verificador precede a toda publicación.
- **Reversión del `README.md`:** copia byte a byte desde `docs/stage3/d2a/closure/identities-20261018/candidate/README.md`, verificada contra el hash esperado `65a33b554534…` del manifiesto D1. Sin copia previa del archivo en la raíz, la recuperación dependía de que el snapshot D2-A contuviera el mismo archivo: lo contenía.
- **Reversión de `docs/stage3/STATUS.md`:** no existe copia congelada en disco. Se reconstruyó el texto original y se verificaron cuatro variantes de finales de línea contra el hash esperado `690e499900a8…` del manifiesto D1. Coincidió la variante CRLF con salto final. La forma de recuperación fue iterar variantes y comprobar el hash, no suposiciones.
- **Resultado:** `verify.py` exit 0, `freeze.py --verify` exit 0, 87/87 entradas del manifiesto D1 coincidentes. No quedó ningún cambio no deseado.

**Derivación preexistente, no causada por esta sesión.** De las 87 entradas del manifiesto D1, 15 tienen un hash distinto del árbol actual. Se comprobó una a una: **las 15 coinciden exactamente con el hash del manifiesto D2-A original** (121 entradas). La deriva está explicada en su totalidad por el incremento D2-A aceptado, que sustituyó a D1 en la raíz. Cero entradas inexplicadas. El manifiesto vigente del árbol de la raíz es el de D2-A original.

### Cambios aplicados

Todos dentro de zonas editables. Ningún archivo congelado alterado.

| Archivo | Acción | Contenido |
|---|---|---|
| `docs/stage3/STATE.md` | **nuevo** | Estado canónico: tabla única de gates (D1, D2-A, D2-B, A1–A8, D3, Etapa 4) con estado, hash vigente, última verificación y bloqueo; qué está probado y qué no; los tres árboles de código y su autoridad; restricción estructural de edición (§3.1–§3.2); glosario de estados; decisiones abiertas; notas de trazabilidad documental; índice por área |
| `docs/stage3/d1/RESULT.md` | cabecera | Marca de que D1 fue aceptado, con puntero al recibo. Cuerpo intacto |
| `docs/stage3/d2a/closure/identities-20261018/RESULT.md` | cabecera | Marca de que D2-A fue aceptado, con puntero al recibo. Cuerpo intacto |
| `docs/stage3/d2a/reports/QA.md` | cabecera | Aclara que el veredicto BLOCKED sigue vigente para aceptación integral y que la aceptación posterior fue acotada |
| `docs/stage3/d2a/reports/CODE-REVIEW.md` | cabecera | Ídem |
| `docs/stage3/d2b/STATUS.md` | cabecera | Enlace al estado canónico |
| `docs/agent-onboarding/dba-engineer/sql-identities-phase2-20261018/README.md` | **nuevo** | Nota de equivalencia: esta copia de la evidencia de fase 2 no es la fuente normativa; la fuente es `docs/stage3/d2a/closure/identities-20261018/evidence/phase2/`. Regla de trabajo y verificación |

### Hallazgos resueltos

| ID | Hallazgo | Resolución |
|---|---|---|
| H1 | Índices raíz desactualizados | Parcial por restricción estructural: `STATE.md` es la referencia única y está enlazado desde `docs/stage3/d2b/STATUS.md`; el `README.md` raíz requiere **OPEN-7** (reemitir manifiestos y nueva aceptación humana) |
| H4 | Evidencia duplicada sin nota | Resuelto con nota de equivalencia; la fuente normativa queda declarada |
| H5 | Cuatro tablas de puertas divergentes | Resuelto: `STATE.md` §1 es la tabla canónica; las demás se conservan como detalle de su área |
| H8 | Taxonomía de estados no normalizada | Resuelto: glosario en `STATE.md` §4 |
| H9 | ADR siguen «Propuesto» sin referencia cruzada | Resuelto en `STATE.md` §6, sin reescribir los ADR (están congelados) |
| H2 | Tres árboles de código sin política | **Documentado** en `STATE.md` §3 con la autoridad de cada uno y la decisión **OPEN-1** pendiente |
| H10 | `.local/` sin política de retención | Parcial: `STATE.md` advierte que `.local/d2b-candidate` no debe limpiarse |
| H3 | Sin Git ni historial | **No resuelto**: OPEN-1. El control por hash funciona, pero no hay diff; se añade `manifest-diff.py` como sustituto parcial (§4) |

## 2. Frente 2 — Paquete de operador A1

A1 está **autorizada** y vigente. El bloqueo es de operador, no de autorización: el sandbox automatizado corre como `DESKTOP-F25O3MF\CodexSandboxOffline`, que no es la identidad SQL `kdi_test_runner` y no tiene autenticación reutilizable acreditada.

| Archivo | Acción | Contenido |
|---|---|---|
| `docs/stage3/d2b/controlled-test-plan/A1-OPERATOR-PACK.md` | **nuevo** | Paquete de una página: tres prohibiciones (no pedir contraseñas, no crear cuentas ni elevar, no modificar el script), condición de entrada, verificación de archivos, verificación de contexto de sesión, ejecución con SQLCMD Mode, tabla de guardas que detienen, captura de evidencia, cierre y qué viene después |
| `docs/stage3/d2b/controlled-test-plan/A1-EVIDENCE-TEMPLATE.md` | **nuevo** | Formato de registro por intento: identificación, valores observados, guardas y desviaciones, archivos y hashes, dictamen del operador y límites de la evidencia |
| `docs/stage3/d2b/controlled-test-plan/A1-FILE-CHECK.ps1` | **nuevo** | Comprobación previa **solo de archivos**: tres hashes esperados y los dos verificadores de integridad. Sin SQL, sin conexión, sin escritura |

### Verificación del script de comprobación

Ejecutado dos veces. Primera ejecución: exit 1 con 5 fallos por una ruta mal calculada (tres niveles en lugar de cuatro) — el script **detectó y detuvo correctamente**. Corregido, segunda ejecución: exit 0 con las seis comprobaciones en OK.

Tercera ejecución, con el candidato D2-B siendo modificado concurrentemente por el frente 3: exit 1 con `freeze.py --verify → exit 1 :: AssertionError: Candidate changed`, mientras `verify.py` de D2-A seguía en exit 0. El comportamiento es el correcto: la puerta se cierra mientras el árbol no sea íntegro. Se corrigió además el manejo de `stderr` nativo, que abortaba el script antes de mostrar el código de salida.

## 3. Herramienta de trazabilidad añadida

`docs/stage3/d2b/preparation-20261020/manifest-diff.py` — compara dos manifiestos y reporta altas, bajas y cambios con sus hashes. Cierra la debilidad que la propia evidencia de continuación declaró: «el manifiesto se regeneró en la misma ruta; no se conservó copia anterior, por lo que no se afirma diff exhaustivo entre los 190 hashes de ambos intentos».

Verificado: exit 0; detecta correctamente un árbol sin cambios.

## 4. Frente 3 — Corrección del harness de fixtures

Detalle completo en [docs/stage3/d2b/harness-correction-20261020/RESULT.md](../d2b/harness-correction-20261020/RESULT.md).

Candidato vigente: **`91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448`**, 197 archivos. Diff acumulado frente al original de 190: **7 altas, 6 cambios, 0 bajas**, todo en `tests/` y `src/Kulani.KDI.Infrastructure/Access/`. **`src/Kulani.KDI.Api/`: byte-idéntico en los 8 manifiestos.**

F1, F2, F3, F6 y F7 corregidos; F4 verificado pero **no atómico**; F5 mitigado y diagnosticado, no prevenido. Revisado de forma independiente por un code-reviewer (0 críticos, 0 altos, 3 medios, 8 bajos) y un qa-engineer (regresión limpia: frontend y navegador idénticos, guardas bloqueando antes de cualquier conexión, traducción EF verificada contra el fichero real).

**El defecto que solo apareció al releer el código.** La primera entrega afirmaba corregir F1 y no lo corregía: encadenaba tres `.Where()` sucesivos, que en LINQ es intersección (`AND`), no unión (`OR`). Eso dejaba sin limpiar exactamente las filas de `user-state` y `sessions-revoke` — las que tienen `OrganizationId` null — y en la clase de acceso dejaba de borrar **ninguna** fila de auditoría, una regresión respecto al original. El defecto fue detectado por el principal al releer el código y devuelto con la reproducción, no por el informe del implementador.

### 4.b Defecto de la ruta de credenciales del operador — encontrado y corregido

**Síntoma:** el CLI `AccessAdmin plan` terminaba siempre con `Administrative operation failed; transaction rolled back. Category=ArgumentException`.

**Causa:** `OperatorConnection.Create` entregaba a `SqlCredential` una **copia** del `SecureString` que no estaba marcada como de solo lectura, y el constructor la rechaza siempre (`ArgumentException: password debe marcarse como de solo lectura`). El CLI de administración y el `Migrations --apply` eran **inejecutables**.

El mensaje exacto se obtuvo **reproduciendo el camino del CLI** fuera del candidato, porque el `catch` del CLI se come el texto de la excepción. La hipótesis inicial (semilla EF con clave no generada) **era falsa** y se descartó al construir el modelo real.

**Corrección en tres rondas**, con revisión independiente en cada una:

| Ronda | Cambio | Revisión |
|---|---|---|
| 1 | La copia se marca de solo lectura y se entrega al `SqlCredential` | R1: PASS de la causa raíz; 2 MEDIOS (superficie pública, copia huérfana) |
| 2 | `Session` como *lease* explícito propietario del `SecureString`, con destrucción determinista en `Dispose`/`DisposeAsync`; la sobrecarga pasa a `internal` con `InternalsVisibleTo`; seams internos de consola y teclado | R2: H1/H2/H4 cerrados; H3 parcial (2 BAJOS) |
| 3 | Test del punto de entrada público (hook unidireccional que solo endurece la guarda) y test de la destrucción determinista del buffer tecleado | QA final: **0 FAIL**, N1/N2/N4 cerrados |

Resultado: **71 unitarias** (57 + 14), 20 API, 24 arquitectura, 0 omitidas.

### 4.c A4 — permisos runtime concedidos

Detalle en [A4-RESULT.md](../d2b/controlled-test-plan/evidence/A1/A4-RESULT.md).

Los 10 permisos runtime sobre 7 objetos están concedidos y verificados con aserciones duras: conjunto **exacto**, sin `DELETE`, sin permiso sobre `organizations.AuditEntries`, sin concesiones de esquema, seis `DENY` de DDL intactos, `kdi_tests` sigue en solo lectura, membresías y logins sin cambios, y `kdi_app` cumple las nueve condiciones del interceptor de runtime.

**La concesión la ejecutó la identidad del operador, no `kdi_migrator`**: los objetos D2-B no tienen dueño (`sys.objects.principal_id` es NULL) y con `kdi_migrator` se produce `Msg 15151`. El conjunto concedido es idéntico; cambia quién concede, no qué se concede.

**Cadena de congelados, cada uno con su manifiesto anterior conservado.** Es la primera vez que el diff entre dos estados de este gate es exhaustivo:

| Manifiesto | Hash | Archivos |
|---|---|---|
| `candidate-manifest.20261020-a1cbed38.json` | `a1cbed38…a4102` | 190 (original) |
| `candidate-manifest.20261020-cee627f6.json` | `cee627f6…e4` | 194 (entrega con el defecto `AND`) |
| `candidate-manifest.20261020-80ff7c4c.json` | `80ff7c4c…fe4` | 195 (unión corregida) |
| `candidate-manifest.20261020-e73f02a8.json` | `e73f02a8…5691` | 195 (MEDIO-1/MEDIO-2) |
| `candidate-manifest.20261020-d106dac6.json` | `d106dac6…9cb` | 195 (revisado por QA) |
| `candidate-manifest.20261020-7a057a2e.json` | `7a057a2e…4c6f` | 196 (fix de credencial, ronda 1) |
| `candidate-manifest.20261020-79d63864.json` | `79d63864…ccb0` | 197 (H1–H4) |
| `candidate-manifest.json` (vigente) | `91dce7d9…448` | 197 (N1/N2 cerrados) |

**Hallazgo de plan que no es del código:** el rol `kdi_tests` de `KulaniKDI_Test` solo tiene `GRANT SELECT` sobre los tres esquemas, y `SqlTarget` exige autenticación integrada, de modo que el harness se ejecutaría como la identidad Windows del proceso y no como `kdi_test_runner`. El harness necesita `INSERT` y `DELETE` en siete tablas: una corrida fallaría por permisos y podría dejar sesiones y auditoría huérfanas. Queda como decisión **OPEN-8**, consignada como advertencia conocida en el artefacto del harness, **sin tocar ningún permiso**.

## 5. Frente 4 — Runner de validación portable

`docs/stage3/tooling/Kdi.Validate/validate.ps1` y su README. 17 pasos: integridad previa → build → tres suites xUnit → StructureCheck → typecheck/lint/build web → Playwright → integridad posterior, con informe JSON estructurado y demostración de que el candidato no muta.

**Restricción de ubicación discoveries durante la ejecución:** el runner **no puede vivir en `tools/`** de la raíz, porque ese directorio está cubierto por el manifiesto D2-A original y crear ahí un archivo rompe `verify.py`. Vive en `docs/stage3/tooling/Kdi.Validate/`.

**Tres defectos del runner encontrados y corregidos al ejecutarlo** (el runner no se había ejecutado nunca de extremo a extremo):

1. **Orden de pasos.** Intentaba compilar `StructureCheck` **antes** de restaurarlo; como el tool está fuera de la solución y no tiene assets en el candidato, fallaba con `NETSDK1004`/`NETSDK1004`-style `project.assets.json` ausente y el resultado global era FAIL. Ahora: restore offline **primero**, build después.
2. **`NuGet.offline.config` malformado.** La sección `packageSourceMapping` usaba la forma inválida `<add key=… pattern=…/>`; NuGet la rechazaba con «Falta el atributo obligatorio "value" en el elemento "add"» y el restore no llegaba a empezar. Corregida: con una única fuente local tras `<clear/>` no hace falta mapeo.
3. **Dependencias no bloqueantes.** Un paso se ejecutaba aunque su dependencia se hubiera **omitido**, produciendo un fallo engañoso (intentar ejecutar un ejecutable inexistente). Ahora `SKIPPED` también bloquea al dependiente.

**Estado final de la ejecución:** 13 pasos correctos, 1 omitido por política (`sql-integration-tests`, que abre conexiones a SQL Server), 1 omitido con motivo explícito (restore de StructureCheck bloqueado por `NU1004`, inconsistencia **preexistente** del `packages.lock.json` del tool en el candidato congelado, que el runner no corrige porque corregirlo mutaría el candidato) y 2 omitidos en cascada. Resultado global **PARTIAL**, que es el valor honesto: el criterio de StructureCheck queda NOT VERIFIED, mientras que la política de arquitectura **sí** está verificada por la suite `Kulani.KDI.ArchitectureTests` (24/24), que forma parte de la solución. **Candidato intacto: True** (manifiesto estable y digest de contenido idéntico antes y después).

## 6. Verificación de esta entrega

```powershell
python docs/stage3/d2a/closure/identities-20261018/verify.py   # exit 0
python docs/stage3/d2b/preparation-20261020/freeze.py --verify   # exit 0 (197 archivos, 91dce7d9…)
.\docs\stage3\d2b\controlled-test-plan\A1-FILE-CHECK.ps1         # exit 0
```

Las tres verificaciones se ejecutaron al cierre de cada frente. El estado exacto tras la corrección del harness se registra en §7.

## 7. Cierre y siguiente acción

### Cierre de esta sesión

| Frente | Estado | Evidencia |
|---|---|---|
| 0 · Reconciliación documental | **CERRADO** | `docs/stage3/STATE.md` + 7 documentos marcados + nota de equivalencia de evidencia |
| 2 · Paquete de operador A1 | **CERRADO** | 3 documentos; `A1-FILE-CHECK.ps1` ejecutado con exit 0 y con exit 1 en condición real de fallo |
| 3 · Corrección del harness | **CERRADO con alcance acotado** | Revisión independiente + QA independiente + verificación delta; ver §4 |
| 4 · Runner de validación portable | **CERRADO** | `docs/stage3/tooling/Kdi.Validate/`; ejecución completa con informe JSON |

**Cadena de integridad al cierre:** `verify.py` (D2-A) exit 0 · `freeze.py --verify` exit 0 (`d106dac6…`, 195 archivos) · `A1-FILE-CHECK.ps1` exit 0 · runner: 13 pasos correctos, 0 fallidos, candidato intacto (manifiesto estable y digest de contenido idéntico).

**Manifiestos de candidato conservados**, uno por ronda, para que el diff entre estados sea exhaustivo: `a1cbed38…` (190) → `cee627f6…` (194) → `80ff7c4c…` (195) → `e73f02a8…` (195) → `d106dac6…` (195, vigente).

### Siguiente acción

**No hay ninguna acción técnica ejecutable por el agente.** Todo lo que queda depende de una decisión o de un entorno humano:

1. **A1** — ejecutar el preflight desde una sesión Windows ya autenticada como `kdi_test_runner` sobre `KulaniKDI_Test`. Procedimiento y comprobación de archivos listos: [paquete de operador](../d2b/controlled-test-plan/A1-OPERATOR-PACK.md). A1 **ya está autorizada**; no requiere nueva autorización. Si no existe sesión autenticada utilizable, A1 sigue BLOCKED y no hay sustituto.
2. **OPEN-8** — decidir identidad y permisos mínimos de Test. El harness necesita `INSERT` y `DELETE` en siete tablas y hoy el rol `kdi_tests` solo tiene `SELECT`. **Es bloqueante de A5** y no se resuelve sin decisión humana.
3. **OPEN-9** — decidir si la limpieza administrativa se acepta sin atomicidad (con evidencia y recuperación manual) o si se exige variante transaccional.
4. **OPEN-7** — decidir si se reemite la documentación raíz bajo un hash nuevo para poder corregir el índice raíz, o si se acepta que el estado vigente se alcance por `docs/stage3/STATE.md`.
5. **OPEN-1** — fijar la línea base canónica de código y decidir si se inicializa Git.
6. **D3** — no iniciado. El runner de validación portable es la base del adaptador de CI, pero el proveedor y el repositorio siguen sin seleccionar.

## 8. Límites declarados

- No se ejecutó ninguna operación SQL, ni conexión, ni migración, ni backup, ni GRANT.
- No se ejecutó ninguna prueba SQL. `Kulani.KDI.SqlIntegrationTests` no se ejecutó en ningún momento.
- No se declaró PASS de ningún gate. D2-B sigue **ABIERTO / BLOQUEADO**; A1 sigue **BLOQUEADO**.
- No se accedió a IDI, ni a otras bases, ni a configuración global de SQL Server.
- No se fabricaron aprobaciones humanas.
