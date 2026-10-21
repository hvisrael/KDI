# CODE-REVIEW-DELTA — resolución de los hallazgos de la revisión

Resolución de los hallazgos de [CODE-REVIEW.md](CODE-REVIEW.md) y de las observaciones de [QA.md](QA.md). El informe original se conserva sin alterar; este documento describe **qué se cambió** y **quién lo verificó**.

Candidato final: **`d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb`**, 195 archivos. Manifiesto: `docs/stage3/d2b/preparation-20261020/candidate-manifest.json`.

## Defecto crítico interceptado antes de la revisión

La primera entrega del ingeniero **no corregía F1**. Encadenaba tres `.Where()` sucesivos, que en LINQ es una **intersección** (`AND`), no la unión (`OR`) exigida:

```csharp
var auditScope = Identity.AuditEntries.Where(x => actorIds.Contains(x.ActorId));
if (organizations.Length > 0) auditScope = auditScope.Where(x => x.OrganizationId != null && organizations.Contains(x.OrganizationId.Value));
if (users.Length > 0) auditScope = auditScope.Where(x => users.Contains(x.TargetId));
```

Dos consecuencias, ambas **reproducibles por lectura**:

1. Las filas de `user-state` y `sessions-revoke` tienen `OrganizationId = null` (`AccessAdministration.cs:49`, `OrganizationId = r.OrganizationId`), luego el término `x.OrganizationId != null` las excluía: **F1 seguía presente**.
2. En la clase de acceso el resultado era `(A OR B) AND (C OR D)`. Como `SqlAccessStore.Audit` escribe `ActorId = user.Id` y `OrganizationId` null, **la limpieza de auditoría ya no borraba ninguna fila** — una regresión respecto al original (`x.ActorId == user.Id.ToString()`).

El defecto fue detectado por el principal al releer el código, y no por el informe del implementador, que afirmaba la corrección. Se devolvió el trabajo con la reproducción.

## Cómo se resolvió

| Hallazgo | Resolución | Verificado por |
|---|---|---|
| **F7** (intersección `AND`) | Helper único `SqlAuditScope.cs` con un predicado por tabla que expresa la **unión** en un solo `Where`, y guarda de la collections vacías resuelta dentro de la expresión | QA: SQL generado por EF real — `ActorId = @p1 OR TargetId IN (…) OR (OrganizationId IS NOT NULL AND OrganizationId = @p4)`; los términos vacíos **desaparecen**; con los tres vacíos, `WHERE 0 = 1` |
| **F1** (auditoría huérfana) | La unión cubre las tres vías: actor del caso, target del caso, organización del caso | QA: las 7 categorías de fila (`User.Created`, `Session.Created`, `Session.Revoked`, `Admin.user-state`, `Admin.sessions-revoke`, `Admin.bootstrap/role/membership`) caen dentro del ámbito |
| **F2** (token del test) | Token propio con presupuesto de 120 s en las dos clases | Code review + lectura |
| **F3** (sentencia fallida aborta; IDs en memoria) | `SqlCleanup.IsolatedAsync` aísla cada sentencia; artefacto JSON con escritura atómica y GUIDs de recuperación | Code review + QA (artefacto real generado) |
| **F4** (sin verificación) | Verificación por tabla con el **mismo** alcance que el `DELETE`, y los conteos explícitos (incluido 0) en el artefacto. **La atomicidad no se implementa**: es una decisión de plan (MEDIO-3) | Code review + QA (`WHERE` de borrado == `WHERE` de residuo) |
| **F5** (interferencia de la org temporal) | Foto de la invariante **antes** de cualquier mutación, que distingue violación preexistente de introducida; mensaje accionable con los identificadores exactos; serialización intra-proceso | Code review + QA. **No se previene**: eliminar la interferencia rompería la aserción de permisos del caso de acceso. Documentado como limitación |
| **F6** (baseline sin guarda) | Guarda distinta `KDI_SQL_BASELINE_AUTHORIZED=BASELINE-PROBES`, que además **rechaza** ejecutarse si el interruptor de D2-B está presente | Code review: verificado contra el inventario de `QA-READINESS.md`, **ninguna aserción eliminada ni debilitada**. QA: 8/8 fallos con `BLOCKED` literal, sin conexión |
| **MEDIO-1** (residuo sin aislar) | Las verificaciones de residuo también se aíslan; `Clean()` se parte en envoltorio (que nunca lanza) y cuerpo; el fallo original del caso **nunca** se pierde | Code review + QA (verificación delta) |
| **MEDIO-2** (actor como clave de borrado) | El ámbito pasa a ser **por caso**: el operador de la corrida deja de ser clave de borrado. La guarda valida la **forma** `^KDI-D2B-RUN-[0-9a-f]{32}$` y **lanza** | Code review + QA: el literal del operador **no aparece en ninguna de las 11 traducciones**, tampoco con un actor fijo de forma válida |
| **MEDIO-3** (no atomicidad) | **No implementada.** Documentada de forma explícita en el punto exacto de la limpieza, con el estado parcial posible y el artefacto como evidencia de recuperación manual | Decisión de plan, pendiente de decisión humana |
| **BAJO-1** (GUIDs ajenos sin marcar) | `Origin` explícito: `run-fixture` / `preexisting-foreign` | Code review + QA |
| **BAJO-2** (tracking en la foto) | `AsNoTracking()` en las tres consultas | Code review |
| **BAJO-3** (código muerto) | `HasFailures` conectado al resultado del caso: **una limpieza fallida hace fallar el caso aunque no quede residuo**. `Note` pasa a usarse | Code review + QA |
| **BAJO-4** (ventana de registro) | El `subject` sintético se registra **antes** de crear el usuario | Code review |
| **BAJO-5** (salto de escritura inactivo) | La comparación pasa a hacerse sobre el estado sin marca de tiempo | Code review |
| **BAJO-6/7/8, INFO-1** | Documentados en el código, sin cambio de comportamiento | Code review |
| **OBS-1** (comentario que afirmaba unicidad) | Comentarios reescritos: la guarda valida la **forma**, no la unicidad; la contención se apoya en la **ausencia del operador del predicado** | QA: texto literal verificado |
| **OBS-2** (guarda perezosa en la clase de acceso) | En **las dos** clases los predicados se construyen antes de la primera sentencia de borrado (líneas 135–136 y 254–255, frente a 137 y 268) | QA: verificado con números de línea |

## Trazabilidad del congelado

Cada ronda congeló su propio manifiesto y **conservó el anterior**, de modo que existe un diff verificable en cada punto. Esto cierra la debilidad que la propia evidencia de continuación declaró: «el manifiesto se regeneró en la misma ruta; no se conservó copia anterior, por lo que no se afirma diff exhaustivo».

| Manifiesto | Hash | Archivos | Estado |
|---|---|---|---|
| `candidate-manifest.20261020-a1cbed38.json` | `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102` | 190 | Original D2-B, conservado |
| `candidate-manifest.20261020-cee627f6.json` | `cee627f617d3ce9a6d116ff61b0ff9a4756cc2865132267fb44aca82482854e4` | 194 | Tras la primera entrega (con el defecto `AND`) |
| `candidate-manifest.20261020-80ff7c4c.json` | `80ff7c4c81d24491441f54249cc0b8b5f340ae7c19a7f4a1eae4982b9a533fe4` | 195 | Tras la corrección de la unión |
| `candidate-manifest.20261020-e73f02a8.json` | `e73f02a8eca2c55044e8ccde85a5663fbb9a43c55a02baeb284045db53895691` | 195 | Tras MEDIO-1/MEDIO-2 (revisado por QA) |
| `candidate-manifest.json` (vigente) | `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` | 195 | Final, tras OBS-1/OBS-2 (verificado por QA) |

Diff acumulado frente al original de 190: **5 altas, 3 cambios, 0 bajas**, todo en `tests/Kulani.KDI.SqlIntegrationTests/`. **`src/`: 66 entradas, 0 alteradas.**

Herramienta de diff: `python docs/stage3/d2b/preparation-20261020/manifest-diff.py --old <anterior> --new <vigente>`.

## Hallazgo que sigue abierto y no es del código

El QA descubrió que **el plan de permisos de Test no permite ejecutar el harness**: el rol `kdi_tests` de `KulaniKDI_Test` solo tiene `GRANT SELECT` sobre los tres esquemas (`docs/stage3/d2a/sql/01-provision.sql`, `docs/stage3/d2a/PRIVILEGES.md`), y `SqlTarget` exige autenticación integrada, de modo que el harness se ejecutaría como la identidad Windows del proceso y no como `kdi_test_runner`. El harness necesita `INSERT` y `DELETE` en siete tablas.

Es un bloqueante de **plan**, no de código. Se ha consignado como advertencia conocida en el artefacto y en la salida de cada caso, y **no** se ha tocado ningún permiso, rol, interruptor ni `SqlTarget`. Su resolución requiere una decisión humana sobre permisos.

## Conclusión

El delta del harness SQL está **verificado de forma independiente** por un code-reviewer y por un qa-engineer en sesiones separadas, sobre el mismo candidato, con la traducción EF comprobada contra el fichero real y sin abrir ninguna conexión.

**D2-B sigue OPEN/BLOCKED.** No se declara PASS global: A1, A5, permisos efectivos, Auth0, HTTPS y keyring siguen bloqueados, y MEDIO-3 sigue abierto por decisión de plan.
