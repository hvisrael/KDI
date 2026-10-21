# QA — corrección del harness de fixtures D2-B

Rol funcional: `qa-engineer` (AGENTS.md). **agent_type efectivo: `general`** — los perfiles de Codex no son invocables en este entorno; se reporta el tipo real y el rol que ejerce. QA independiente: no se modificó el candidato, su manifiesto ni ningún archivo bajo `.local/d2b-candidate/`.

- candidateHash principal revisado: `e73f02a8eca2c55044e8ccde85a5663fbb9a43c55a02baeb284045db53895691` (195 archivos)
- candidateHash de la verificación delta: `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos)
- **Veredicto global: BLOCKED.** No PASS global. **D2-B sigue OPEN/BLOCKED.**

## Integridad, antes y después de las pruebas

| Momento | Comando | Exit | Resultado |
|---|---|---|---|
| Antes | `freeze.py --verify` | 0 | `e73f02a8…` / 195 files; verification OK |
| Antes | `identities-20261018/verify.py` | 0 | verificado |
| Después | `freeze.py --verify` | 0 | hash **idéntico** — el candidato no muta |
| Después | `identities-20261018/verify.py` | 0 | verificado; el árbol de la raíz sigue congelado e intacto |

Escrituras internas del candidato durante las pruebas, todas en regiones **ya excluidas** por el manifiesto (`bin/`, `obj/`, `.local/e2e/`, `docs/stage3/d2b/evidence/`). Confirmado por el verificador, no supuesto.

## Regresión de las tres suites backend

| Suite | total | pasan | fallan | omitidas | Referencia |
|---|---|---|---|---|---|
| UnitTests | 57 | **57** | 0 | **0** | 57/57 ✔ |
| ApiTests | 20 | **20** | 0 | **0** | 20/20 ✔ |
| ArchitectureTests | 24 | **24** | 0 | **0** | 24/24 ✔ |

Build Release: **0 errores, 0 advertencias** (igual que la referencia). Cachés locales, **sin red**.

**Los 5 ficheros nuevos no pueden violar la política de arquitectura**: `ArchitecturePolicy.Check` recorre **exclusivamente** `src/Kulani.KDI.{Domain,Contracts,Application,Infrastructure,Api}`; `tests/` está fuera de su alcance por construcción. El fichero de la propia suite de arquitectura no aparece en el diff: no se añadió ninguna regla.

## Frontend y navegador

typecheck, lint y build con Node 22.23.3 y `npm.cmd`: exit 0 en los tres, con **hashes de artefacto idénticos** a la referencia (`index-Do02OOjV.css`, `index-DY6Y8_x8.js`). Playwright Chromium: **11/11**, 0 fallos, 0 omitidas, 0 flaky, contratos equivalentes. No se ejecutó `npm ci`, no se borró `node_modules`, no se cambió la política de ejecución.

## Guardas del harness SQL — PASS (comportamiento de guardas)

Precondición verificada **antes** de ejecutar, en proceso, usuario y máquina: `KDI_SQL_TEST_CONNECTION`, `KDI_D2B_SQL_AUTHORIZED` y `KDI_SQL_BASELINE_AUTHORIZED` están **vacías**.

Resultado: 8 casos, 8 fallos, **todos** con el `BLOCKED` explícito del harness:

```
D2bAccessSqlTests…            → BLOCKED: express D2-B SQL fixture authorization required.
D2bAdministrationSqlTests (×5) → BLOCKED: express SQL fixture authorization required.
SqlBaselineTests (×2)          → BLOCKED: the baseline probes require their own express authorization,
                                 KDI_SQL_BASELINE_AUTHORIZED=BASELINE-PROBES, and a separate invocation.
```

**Confirmación de que no se intentó ninguna conexión**, por cuatro pruebas concordantes:

1. **Orden de código**: la guarda se evalúa antes de `SqlTarget.Validate(...)` y, por tanto, antes de cualquier `new SqlConnection`/`OpenAsync`. Los 8 stack traces apuntan a la línea de la guarda.
2. **Ausencia de error de conexión o autenticación**: no aparece ni una vez texto de red, instancia, TLS o login.
3. **Latencia**: 1–73 ms por caso, 2,04 s en total. Un intento de conexión habría producido timeout o error de red.
4. **Efecto lateral ausente**: no se creó ningún artefacto de corrida, lo que prueba que `SqlTestRun.Current` nunca se alcanzó.

**Esto es PASS del comportamiento de guardas, no una suite roja.** El exit code 2 es lo esperado.

## Verificación independiente del ámbito de auditoría (sin conexión)

`ToQueryString()` sobre el **fichero real** con los **contextos reales** (EF Core 10.0.13). Estado de conexión al terminar: `Closed`.

- **Es una unión, no una intersección**: `ActorId = @p1 OR TargetId IN (@p2,@p3) OR (OrganizationId IS NOT NULL AND OrganizationId = @p4)`, en las dos clases y los dos tipos de tabla. Ningún `AND` entre los tres términos.
- **Los términos con conjunto vacío desaparecen** del SQL; con los tres vacíos, `WHERE 0 = 1`. Nunca un `IN ()` vacío ni un predicado siempre verdadero.
- **El literal del operador no aparece en el SQL** en ninguna traducción. Es decir: **el operador de la corrida no es clave de borrado**, ni siquiera con un actor fijo de forma válida. La contención no depende de la guarda, sino de que el operador no entre en el predicado.
- **Cobertura de las filas de auditoría**: `User.Created`, `Session.Created` (target = id de sesión), `Session.Revoked` (store; `ActorId` = usuario, `OrganizationId` null) y `Admin.user-state` / `Admin.sessions-revoke` (`ActorId` = operador, `TargetId` = usuario, `OrganizationId` null) caen **todas** dentro de la unión. Esta última categoría es exactamente la que la intersección dejaba fuera: la corrección `OR` es **necesaria y suficiente**.
- Ninguna sentencia de borrado alcanza filas ajenas a la corrida: las 7 tablas se acotan por identificadores del caso.

## Verificación delta sobre `d106dac6…`

| Comprobación | Resultado |
|---|---|
| Diff frente a `e73f02a8…` | 190 sin cambio, **5 cambios**, 0 altas, 0 bajas, **0 fuera** de `tests/Kulani.KDI.SqlIntegrationTests/` |
| OBS-1 (comentario de la guarda) | **CERRADO** — el texto ya no atribuye unicidad a la guarda; sí afirma que la contención se apoya en la ausencia del operador del predicado |
| OBS-2 (evaluación perezosa) | **CERRADO** — en ambas clases los predicados se construyen en las líneas 135–136 y 254–255, **antes** de la primera sentencia de borrado (137 y 268) |
| Reutilización del predicado entre `DELETE` y residuo | El `WHERE` de borrado y el de residuo son **idénticos**; el texto es estable entre renders |
| Traducción EF | 11 traducciones: unión `OR`, términos vacíos eliminados, `WHERE 0 = 1` con los tres vacíos, literal del operador **ausente en las 11** |
| Regresión backend | build 0/0; **57/57, 20/20, 24/24**; 0 fallos, 0 omitidas |
| Frontend | 18/18 entradas de `src/Kulani.KDI.Web/` **bit-idénticas**: la evidencia anterior traslada sin cambio. **NOT VERIFIED en esta ronda por evidencia heredada bit-idéntica**, no PASS propio |

### OBS-3 (nuevo, severidad baja) — discrepancia con el implementador

La afirmación «la advertencia de plan es visible incluso si el caso muere pronto» es **más fuerte que lo que el código garantiza**. `WriteKnownWarnings()` está **después** de las guardas de autorización y de validación de cadena de conexión, y el artefacto solo se escribe en la primera llamada `Register*`. Comprobado empíricamente: si el caso muere en las guardas, **ni la salida ni el artefacto muestran la advertencia** (el artefacto ni siquiera se crea). Lo que el código garantiza, y lo que su propio comentario dice con exactitud, es «visible si el caso falla antes de escribir *el resto* de su evidencia». Impacto bajo, **no afecta a la contención**.

## Hallazgo de plan (severidad alta para el plan, no para el código)

**El plan de permisos de Test no permite ni los fixtures ni la limpieza del harness.** Verificado por lectura directa:

- `docs/stage3/d2a/sql/01-provision.sql` (líneas 45–48 y 83–86): al rol `kdi_tests` se le concede **únicamente `GRANT SELECT ON SCHEMA::[identity|organizations|portfolio]`**. Ningún `INSERT`, `UPDATE` ni `DELETE`.
- `docs/stage3/d2a/PRIVILEGES.md` línea 9: `kdi_tests | GRANT | Sin concesión | SELECT en los tres schemas; no escritura`.
- `tools/Kulani.KDI.Migrations/prepared-sql/KulaniKDI_Test-runtime-grants.sql`: concede `SELECT, INSERT, UPDATE` (sin `DELETE`) a `kdi_application`, **no** a `kdi_tests`; y sin permiso sobre `organizations.AuditEntries`.
- `src/Kulani.KDI.Infrastructure/Persistence/SqlTarget.cs`: el harness exige `IntegratedSecurity = true` con `UserID` y `Password` vacíos ⇒ se ejecutaría **como la identidad Windows del proceso**, no como el login `kdi_test_runner`.

**Consecuencia:** el harness necesita `INSERT` (usuarios, sesiones, membresías, grants, organizaciones, auditoría) y `DELETE` (7 tablas). Con el plan vigente **la corrida fallaría por permisos**, y un fallo de permisos en el `DELETE` de `identity.Sessions` dejaría sesiones y auditoría huérfanas, es decir **residuo real**. Es un bloqueante de **plan**, no del código, y debe resolverse antes de solicitar A5.

## Veredicto por criterio

**PASS:** integridad antes y después · árbol de la raíz intacto · alcance del delta acotado · build 0/0 · 57/57 · 20/20 · 24/24 · ficheros nuevos fuera del alcance de la política de arquitectura · typecheck/lint/build/navegador (heredado bit-idéntico) · guardas bloqueando antes de cualquier conexión · predicado de auditoría como unión · términos vacíos eliminados · guarda rechaza 11/11 operadores malformados · cobertura de las filas de auditoría · ninguna sentencia alcanza filas ajenas · OBS-1 cerrado · OBS-2 cerrado · advertencia de plan presente en el artefacto y en la salida.

**BLOCKED (no PASS):** comportamiento real de los `DELETE` contra SQL · conteos de residuo reales · invariante global de administradores en runtime · `sp_getapplock` · carrera concurrente · permisos efectivos del operador sobre `DELETE` en las 7 tablas · existencia de residuo histórico `S-1-5-21-SYNTHETIC-TEST` (fase A1) · OIDC/Auth0 · HTTPS · keyring.

**NOT VERIFIED:** CLI del operador (`tools/Kulani.KDI.AccessAdmin`); frontend en la ronda delta (evidencia heredada bit-idéntica).

**Veredicto global de D2-B: BLOCKED.** A1, A5, permisos efectivos, Auth0 y HTTPS siguen bloqueados. **No se declara PASS global.**

## Qué no hizo QA

No modificó el candidato ni su manifiesto. No abrió ninguna conexión a SQL Server (ni `SqlConnection.Open`, ni `dotnet ef`, ni `sqlcmd`, ni PSQL); la sonda usó `ToQueryString()` con estado final `Closed`. No ejecutó el harness SQL con autorización: la única ejecución fue la excepción acotada de guardas con las tres variables **verificadas vacías antes** y la ruta del artefacto fuera del candidato. No restauró con red. No instaló ni actualizó nada. No accedió a IDI. No aceptó ningún criterio bloqueado como PASS.
