# Code Review — corrección del harness de fixtures D2-B

Rol funcional: `code-reviewer` (AGENTS.md). **agent_type efectivo: `general`** — los perfiles de Codex no son invocables en este entorno; se reporta el tipo real y el rol que ejerce. Revisión independiente: el revisor no escribió el código revisado y no lo modificó. CandidateHash revisado: `80ff7c4c81d24491441f54249cc0b8b5f340ae7c19a7f4a1eae4982b9a533fe4` (195 archivos).

## Método: evidencia propia, no el informe del implementador

| # | Comprobación | Resultado |
|---|---|---|
| E1 | Integridad del congelado (inicio y fin) | exit 0, 195 archivos, hash correcto; el candidato **no fue modificado** por la revisión |
| E2 | Diff real frente a los 190 archivos previos | 5 altas + 3 cambios + 0 bajas, **0 archivos fuera** de `tests/Kulani.KDI.SqlIntegrationTests/` |
| E3 | Compilación | 0 errores, 0 advertencias (`TreatWarningsAsErrors=true`), sobre **copia aislada** en `%TEMP%` |
| E4 | **Traducción EF real** con `ToQueryString()` sobre el fichero real y los contextos reales | Unión `OR` con `IN`; los términos con conjunto vacío **desaparecen**; con los tres vacíos, `WHERE 0 = 1`. **Ninguna conexión abierta** (cadena a un servidor inexistente) |
| E5 | Inventario de casos | Los **8 Fact** con los **nombres idénticos** al inventario de `QA-READINESS.md`: ninguno renombrado, eliminado ni añadido (descubrimiento con `-list methods`, sin ejecución) |
| E6 | Autores de auditoría en `src/` | 5 `AuditEntries.Add` en total (3 en `SqlAccessStore`, 2 en `AccessAdministration`), todos con doble cobertura en la unión |
| E7 | Orden de borrado frente a las FK | `Sessions→Grants→Audit→OrgAudit→Memberships→Orgs→Users` respeta `Restrict` |
| E8 | El artefacto no rompe el congelado | `.local` está en `EXCLUDED` de `freeze.py` |
| E9 | Destino acotado | `SqlTarget.Validate(..., testOnly:true)` exige `DESKTOP-F25O3MF` + `KulaniKDI_Test` + Windows integrada, sin `UserID`/`Password` |

## Hallazgos

**CRÍTICOS: 0. ALTOS: 0.**

### MEDIO-1 — Una excepción en la fase de residuo enmascara el fallo real y pierde la evidencia
Los 7 `DELETE` estaban aislados, pero las 7 verificaciones de residuo no: `ToArrayAsync` propaga. Y `Clean()` estaba fuera del `try/catch` del cuerpo del caso. Si la limpieza fallaba, su excepción sustituía al fallo original y `RecordCleanup` no se llamaba, de modo que el artefacto **no registraba ni las sentencias ejecutadas ni los identificadores de lo que quedó** — justo lo que promete el mensaje `BLOCKED: … the exact identifiers are in <artifact>`.

### MEDIO-2 — El actor de corrida equivale a «borrar toda la auditoría del actor»
`SECURITY-SQL-REVIEW.md` prohíbe literalmente borrar en bloque por el SID sintético compartido. El código no validaba la forma del SID antes de usarlo como clave de borrado, y el término `byActor` permitía que un caso borrara la auditoría de otro caso de la misma corrida.

### MEDIO-3 — La limpieza administrativa no es atómica
7 `DELETE` en autocommit. Una interrupción entre sentencias deja estado parcial; si el borrado de `Memberships` falla mientras el de `Organizations` se ejecuta, la organización sobrevive sin administrador y **viola la invariante global**, haciendo fallar los casos de administración siguientes. El requisito literal de `QA-READINESS.md` (evidencia de IDs y recuperación manual) **sí** se cumple; la palabra «atomicidad» del enunciado no.

### BAJOS

| ID | Hallazgo |
|---|---|
| BAJO-1 | El artefacto y el mensaje llevan GUIDs de organizaciones **ajenas** al run, sin marcar su origen |
| BAJO-2 | La foto de la invariante usa consultas con *tracking* sobre el contexto que después hace `SaveChanges` |
| BAJO-3 | `CleanupReport.HasFailures` y `CleanupStatementResult.Note` están **muertos**: un error de limpieza no hace fallar el caso si no queda residuo |
| BAJO-4 | Ventana de pérdida de identificador: el usuario se registra **después** de crearse |
| BAJO-5 | El salto de escritura por contenido inalterado **nunca se activa** (`UpdatedUtc` cambia siempre) |
| BAJO-6 | La ruta por defecto del artefacto cae dentro del árbol congelado (no rompe el hash: `.local` está excluido) |
| BAJO-7 | `ParallelMode.None` serializa **solo dentro del proceso**; el comentario sobredimensionaba la garantía |
| BAJO-8 | La insensibilidad al orden entre clases no está eliminada: es una limitación real del fixture, no un descuido |
| INFO-1 | El residuo histórico con `ActorId = S-1-5-21-SYNTHETIC-TEST` **no** es alcanzable por el código nuevo |
| INFO-2 | `SqlBaselineTests` ahora falla de forma explícita sin interruptor: una ejecución de la suite completa tendrá 2 fallos garantizados |

## Veredicto por defecto (F1–F7)

| Defecto | Veredicto | Evidencia |
|---|---|---|
| **F1** — auditoría huérfana de `user-state`/`sessions-revoke` | **CORREGIDO** | La unión cubre `TargetId`=usuario y el actor fijo compartido fue sustituido por un SID único por run; el residuo se verifica con el **mismo** alcance que el `DELETE` |
| **F2** — el `finally` reutilizaba el `CancellationToken` del test | **CORREGIDO** | Token propio con presupuesto de 120 s en las dos clases |
| **F3** — una sentencia fallida abortaba las siguientes; GUIDs solo en memoria | **CORREGIDO** | `IsolatedAsync` captura y continúa; los GUIDs se persisten con escritura atómica |
| **F4** — sin atomicidad ni verificación de resultado | **PARCIALMENTE CORREGIDO** | Verificación de resultado completa (7 conteos por tabla, mismo alcance, IDs exactos); **atomicidad no**: 7 sentencias en autocommit |
| **F5** — la organización temporal de acceso interfiere con la validación global | **PARCIALMENTE CORREGIDO** | Mitigado y diagnosticado (foto previa antes de mutar, mensaje accionable), **no prevenido**: no puede evitarse sin romper la aserción de permisos del caso de acceso |
| **F6** — `SqlBaselineTests` sin guarda propia | **CORREGIDO** | Guarda distinta y explícita; **ninguna aserción eliminada ni debilitada** (verificado contra el inventario de `QA-READINESS.md`) |
| **F7** — intersección `AND` en lugar de unión `OR` | **CORREGIDO** | Verificado con SQL generado por EF, no por lectura |

## Conclusión de seguridad: alcance de los borrados

Se enumeraron los **14 `DELETE`** del harness y su predicado. **El harness no puede tocar datos ajenos a la corrida**, bajo cuatro precondiciones verificadas en el código:

1. El `OperatorSid` es único por proceso (`Guid.NewGuid()`), nunca coincide con el actor fijo histórico.
2. Los usuarios y organizaciones de cada caso son GUIDs nuevos generados dentro del caso.
3. El destino está acotado por `SqlTarget.Validate(..., testOnly: true)`: `KulaniKDI_Test` únicamente.
4. Los identificadores se registran **antes** de crear cada organización o de ejecutar cada comando.

Lo que sí puede alcanzar el harness: filas de **otra clase del mismo run** (por el término `byActor`) y, si el proceso muere a mitad de la limpieza, un **estado parcial** de sus propias filas. Ambos dentro de la corrida; ninguno es de negocio.

## Recomendación

**El candidato puede pasar a la cola de QA.** No requiere otra ronda para los criterios estáticos. **No es admisible a ejecución conectada** hasta: (MEDIO-1) aislar las verificaciones de residuo; (MEDIO-2) guarda defensiva del `OperatorSid`; (MEDIO-3) decisión escrita sobre la no atomicidad.

Este dictamen es una entrada más de D2-B. **D2-B sigue OPEN/BLOCKED.**
