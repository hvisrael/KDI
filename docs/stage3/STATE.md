# Estado canónico de KDI

Documento de índice. Es la referencia única del estado del proyecto a partir de su creación. No sustituye ni reescribe la evidencia de los gates: los enlaza.

Fecha del documento: 2026-10-20 (sesión local). Autoridad: Human Orchestrator. Proyecto: KDI — Kulani Delivery Intelligence, independiente de AI-ENGINEERING; IDI fuera de alcance.

Los documentos que existían antes de este archivo y describen un estado distinto (por ejemplo `docs/stage3/STATUS.md` o `docs/stage3/d1/RESULT.md`) se conservan íntegros como evidencia histórica. Este documento indica cuál es la lectura vigente.

## 1. Gates

| Gate | Alcance | Estado | Evidencia ancla | Hash vigente | Última verificación |
|---|---|---|---|---|---|
| Etapa 1 | Especificación documental de producto | PROPUESTA, NO IMPLEMENTADA | [docs/source/kdi/README.md](../source/kdi/README.md) | — | Copias históricas verificadas por SHA-256 en la fundación |
| **D1** | Solución por capas, API base, shell web, verificaciones | **ACEPTADO** (2026-10-17) | [docs/stage3/d1/acceptance/HUMAN-APPROVAL.md](d1/acceptance/HUMAN-APPROVAL.md) | `e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193` | `docs/stage3/d1/closure/` (87 fuentes, 36 entradas de evidencia) |
| **D2-A** | Identidades SQL: logins, usuarios, membresías, permisos | **ACEPTADO** (registro 2026-10-19T03:38:59Z) | [docs/stage3/d2a/closure/identities-20261018/ACCEPTANCE.md](d2a/closure/identities-20261018/ACCEPTANCE.md) | candidato `c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973` (123 archivos); evidencia `80ff0d9ac23386569e7c620d237a6af401ba6b48dd5f9447859ba3090e646d68` (44 archivos); entrega `38c8b7c4f9849d58884fa05831c4a0cb90481da60130d6528578e60f420ada5e`; original `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f` | `python docs/stage3/d2a/closure/identities-20261018/verify.py` → exit 0 (2026-10-20) |
| **D2-B** | Candidato D2-B: permisos runtime, CLI de acceso, harness SQL | **ABIERTO / BLOQUEADO** | [docs/stage3/d2b/STATUS.md](d2b/STATUS.md) · [resultado del harness](d2b/harness-correction-20261020/RESULT.md) · [corrección de credenciales](d2b/operator-credential-fix-20261021/QA.md) | `91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448` (197 archivos) | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` → exit 0 (2026-10-21) |
| A1 | Preflight conectado de solo lectura sobre `KulaniKDI_Test` | **CUMPLIDA** (con desviación de operador declarada) | [evidence/A1/A1-A2-RESULT.md](d2b/controlled-test-plan/evidence/A1/A1-A2-RESULT.md) · [log](d2b/controlled-test-plan/evidence/A1/A1-run-20261020-windows-admin.log) | script `60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c` | Exit 0. Target `DESKTOP-F25O3MF`/`KulaniKDI_Test`, SQL 2019 15.0.2200.2, compat 150, historial con **solo** las 3 migraciones baseline, sin drift y sin residuo D2-B |
| A2 | Backup `COPY_ONLY`/`CHECKSUM` + `VERIFYONLY` | **CUMPLIDA** | [evidence/A1/A1-A2-RESULT.md](d2b/controlled-test-plan/evidence/A1/A1-A2-RESULT.md) · [log](d2b/controlled-test-plan/evidence/A1/A2-backup-20261020.log) | backup `b59bf8e0117f165f0aed1545a2f5ea80681bc6ee8fe3b43cd681f8d6dea38e89`, 3,4 MB, `is_copy_only=1`, `has_backup_checksums=1` | `VERIFYONLY` → *"The backup set on file 1 is valid."* · evidencia de restauración de ensayo (A7) sigue pendiente de decisión |
| A3 | Dos upgrades Test (identity, organizations) y repetición | **CUMPLIDA** | [evidence/A1/A3-RESULT.md](d2b/controlled-test-plan/evidence/A1/A3-RESULT.md) | `KulaniKDI_Test-identity.sql` `B18B5160…` · `KulaniKDI_Test-organizations.sql` `5DD317EE…` | Aplicado como `kdi_migrator` (ruta R1): `MIGRATION SUCCESS` en ambos módulos. Verificación exit 0: 8 tablas, 8 códigos de permiso, 2 filas de historial por módulo, 4 FK + 3 CHECK, `rowversion` en las 5 entidades. `portfolio` intacto |
| A4 | Siete permisos runtime por objeto | **CUMPLIDA** | [A4-RUNTIME-GRANTS-PACK.md](d2b/controlled-test-plan/A4-RUNTIME-GRANTS-PACK.md) · evidencia `evidence/A1/A4-*` | `KulaniKDI_Test-runtime-grants.sql` `CB34B1AC…` (10 permisos sobre 7 objetos) | Concedido y verificado con aserciones duras: conjunto **exacto**, sin `DELETE`, sin permiso sobre `organizations.AuditEntries`, sin concesiones de esquema, seis `DENY` de DDL intactos, `kdi_tests` sigue en solo lectura, membresías y logins sin cambios, y `kdi_app` cumple las nueve condiciones del interceptor de runtime |
| A5 | Fixtures, DML de prueba y limpieza acotada | PENDIENTE, **fuera del ciclo actual** (N1 + N2) | ídem | — | Depende de resolver OPEN-8 (permisos **y** identidad de autenticación del harness) |
| A6 | Auth0, HTTPS localhost, keyring externo, arranque del piloto | BLOQUEADO | ídem | — | — |
| A7 | Restauración de ensayo o downgrade físico | BLOQUEADO, **separada y destructiva** | ídem | — | No autorizada por A2/A3; no forma parte de la ejecución inicial |
| A8 | Probes negativos, inyección de fallos, estado operacional | BLOQUEADO | ídem | — | — |
| **D3** | CI y cierre de Etapa 3 | **NO INICIADO** | Propuesta en [docs/stage3/DECISIONS.md](DECISIONS.md) §D3 | — | Sin documento, sin script de validación portable, sin proveedor de CI designado |
| **Etapa 4** | Recorridos KDI-UC01–05, criterios AC01–13 | **NO INICIADA** | [docs/source/kdi/REQUIREMENTS.md](../source/kdi/REQUIREMENTS.md) | — | Propuestas de Etapa 1, sin estado de avance |

**No hay release aceptado de Etapa 3.** No hay despliegue autorizado. Ningún gate de D2-B está cerrado.

## 2. Qué está probado y qué no

**Probado en local, sin credenciales ni SQL (2026-10-20):**

- Backend Release: 0 errores, 0 advertencias.
- xUnit: 57/57 unitarias, 20/20 API, 24/24 arquitectura. Sin fallos ni omitidas.
- Frontend: typecheck, lint y build sin errores. Playwright Chromium 11/11.
- SQL generado offline para Test y Dev: 12 artefactos reproducidos = 14 hashes de paquete + 4 alcances de rollback; exit 0.
- Verificadores de integridad de D1, D2-A y D2-B: exit 0.
- **Runner de validación portable** (`docs/stage3/tooling/Kdi.Validate/`): ejecuta la cadena completa local con un informe JSON estructurado, sin red, sin SQL y con demostración de que el candidato no muta. Ver [notas del runner](d2b/harness-correction-20261020/RESULT.md).
- **Harness de fixtures D2-B**: los siete defectos F1–F7 del QA de preparación están corregidos o documentados, y el cambio fue revisado de forma independiente por un code-reviewer y un qa-engineer en sesiones separadas sobre el mismo hash, con la traducción EF verificada contra el fichero real. **Nada de eso se ejecutó contra SQL.**
- **A3 aplicado y verificado**: las 8 tablas de D2-B existen en `KulaniKDI_Test` con sus índices, FK, CHECK, `rowversion` y la semilla de 8 permisos. Aplicado por `kdi_migrator` (ruta de menor privilegio), con verificación de solo lectura en exit 0. Detalle en [A3-RESULT.md](d2b/controlled-test-plan/evidence/A1/A3-RESULT.md).

**No probado (BLOCKED, no FAIL):** cualquier operación contra SQL Server real; idempotencia operacional de migraciones; permisos efectivos runtime; carreras concurrentes reales; OIDC/Auth0; HTTPS real; keyring persistente; CSRF; aislamiento entre organizaciones; expiración y revocación de sesión entre peticiones; auditoría de dependencias vigente; certificación de vigencia del SDK 10.0.103.

Las pruebas API locales usan TestHost y almacenamiento doble; el navegador combina contratos sintéticos con smoke de fundación. No acreditan OIDC ni persistencia real.

## 3. Árboles de código y su autoridad

Existen tres árboles de código vivos. **Ninguno es "el producto"**: la línea canónica todavía no está fijada. Esta es una decisión pendiente del Human Orchestrator (ver §5).

| Árbol | Ruta | Contenido | Autoridad |
|---|---|---|---|
| Línea D1 | `src/`, `tests/`, `tools/` de la raíz | Solución por capas y shell web de D1. `src/Kulani.KDI.Api/Program.cs` describe el contrato como «D1: public liveness and product metadata». | Aceptado como candidato D1 (hash `e2bb22b7…`). No contiene D2-A ni D2-B. |
| Snapshot D2-A | `docs/stage3/d2a/closure/identities-20261018/candidate/` | Código del incremento D2-A aceptado (123 archivos). | **Inmutable.** Su `ACCEPTANCE.md` declara explícitamente que no se integra automáticamente sobre la línea base original. |
| Candidato D2-B | `.local/d2b-candidate/` | Código D2-B en curso (190 archivos en manifiesto). | Congelado y verificado, **no aceptado**. `.local/` es territorio no versionado: no debe limpiarse ni deempaquetarse. |

Consecuencia operativa: antes de editar, identificar en qué árbol se trabaja y verificar su hash con el verificador correspondiente. Editar el árbol equivocado es un fallo difícil de detectar después.

**No hay repositorio Git en la raíz ni en los candidatos** (`git status` → exit 128, registrado en `docs/stage3/d2b/controlled-test-plan/git-status.log`). El control de integridad se apoya en manifiestos y hashes; no hay historial ni diff. El manifiesto vigente del árbol de la raíz es el del candidato **D2-A original** (121 entradas); el de D1 (87 entradas) describe un estado anterior del mismo árbol. Véase §3.1 y §8.

### 3.1 Restricción estructural: qué archivos pueden modificarse

**Hallazgo verificado empíricamente durante la redacción de este documento.** El árbol de la raíz **no es editable en general**: está congelado por hash.

`tools/d2a-manifest.py` (invocado por `verify.py` de D2-A) construye el inventario del candidato D2-A original a partir de:

- todos los **archivos sueltos de la raíz** (`README.md`, `AGENTS.md`, `Directory.Build.props`, `Kulani.KDI.slnx`, `project.json`, …), y
- todo el contenido de `src/`, `tests/`, `tools/`, `docs/api/`, `docs/source/` y `docs/stage3/d2a/`.

Cualquier cambio en esos sitios hace que `python docs/stage3/d2a/closure/identities-20261018/verify.py` falle con `AssertionError: Candidate drift` (exit 1), y con ello se invalida la afirmación «D1/D2-A intactos» que aparece en **toda** la evidencia de D2-B (`accepted-integrity.log`).

Además, el manifiesto de D1 aceptado (`e2bb22b7…`, 87 entradas) cubre con hash el `README.md` de la raíz, `docs/stage3/STATUS.md`, `docs/stage3/DECISIONS.md` y `docs/source/kdi/adr/README.md`.

Por tanto:

| Zona | ¿Editable? | Nota |
|---|---|---|
| Archivos sueltos de la raíz | **NO** | Congelados por el manifiesto D2-A original |
| `src/`, `tests/`, `tools/`, `docs/api/`, `docs/source/`, `docs/stage3/d2a/` | **NO** | Ídem |
| `docs/stage3/` (salvo `d2a/`), `docs/agent-onboarding/` | **Sí** | Fuera del inventario congelado |
| `.local/d2b-candidate/` | **Sí**, con manifiesto nuevo | Cada cambio exige regenerar el manifiesto y un hash nuevo |

**Consecuencia directa sobre H1 (índices raíz desactualizados):** el `README.md` de la raíz afirma hoy que «D2 (SQL/identidad) permanece pendiente», y **no puede corregirse en el sitio** sin invalidar el candidato D2-A aceptado y el candidato D1 aceptado. La corrección exige una decisión formal: reemitir los manifiestos de documentación bajo un hash nuevo y someterlos a una aceptación humana nueva (decisión **OPEN-7**). Hasta entonces, el estado vigente se alcanza por `docs/stage3/STATE.md`, enlazado desde `docs/stage3/d2b/STATUS.md`.

Comprobado en la práctica: una edición de prueba del `README.md` de la raíz provocó `verify.py` exit 1 (`Candidate drift`) y dejó rotas 2 de las 87 entradas del manifiesto D1. Ambas reversiones se restauraron byte a byte desde las copias congeladas (`docs/stage3/d2a/closure/identities-20261018/candidate/README.md` para el README) y por reconstrucción verificada contra el hash del manifiesto D1 para `docs/stage3/STATUS.md`. Estado tras la restauración: `verify.py` exit 0, `freeze.py --verify` exit 0, 87/87 entradas del manifiesto D1 coincidentes.

### 3.2 Inventario de documentos congelados

De los 167 documentos Markdown bajo `docs/` a la fecha de este documento, **144 son editables** y **23 están congelados** por el manifiesto D2-A original. Los congelados son:

`docs/api/README.md` (1) · `docs/source/kdi/` (19 documentos Markdown —`README.md`, `VISION.md`, `REQUIREMENTS.md`, `ARCHITECTURE.md`, `MODULES.md`, `SOLUTION-STRUCTURE.md`, `DATA-MODEL.md`, `API.md`, `TEST-PLAN.md`, `BACKLOG.md`, `INCREMENTAL-PLAN.md`, `FIRST-MULTIAGENT-FLOW.md`, `RISKS.md`, `project.json` y los 5 ADR con su índice—; `project.json` no es Markdown pero está igualmente congelado) · `docs/stage3/d2a/AUTHORIZATION.md`, `IMPLEMENTATION.md` y `PRIVILEGES.md` (3).

Además, en la raíz están congelados por el mismo manifiesto todos los archivos sueltos (`README.md`, `AGENTS.md`, `project.json`, `Directory.*.props`, `Kulani.KDI.slnx`, `global.json`, `NuGet.Config`, `.editorconfig`, `.gitignore`) y `docs/stage3/STATUS.md`, `docs/stage3/DECISIONS.md`, `docs/stage3/FUNCTIONAL-ANALYSIS.md`, `docs/stage3/baseline.json`, `docs/stage3/task-input.json`.

**Editable** (y por tanto donde puede vivir cualquier corrección futura): `docs/stage3/` salvo `d2a/` y salvo los tres archivos listados, `docs/stage3/d2b/` completo, `docs/stage3/d2a/closure/` y `docs/stage3/d2a/reports/`, `docs/agent-onboarding/` completo, y cualquier ruta nueva que no cuelgue de `src/`, `tests/`, `tools/`, `docs/api/`, `docs/source/` ni `docs/stage3/d2a/`.

Criterio operativo para quien edite: **crear es más seguro que modificar**. Un archivo nuevo en una zona editable no altera ningún manifiesto; un archivo nuevo en una zona congelada rompe la verificación.

## 4. Glosario de estados

Se usan estos términos con este significado exacto en todo el proyecto:

| Término | Significado |
|---|---|
| **ACEPTADO** | El Human Orchestrator aprobó ese candidato por escrito. Solo aplica a D1 y D2-A, y solo a los hashes indicados. |
| **PASS** / **PASS técnico acotado** | Todas las verificaciones **previstas para ese alcance** se ejecutaron y pasaron, y ningún criterio crítico quedó sin probar **dentro de ese alcance**. No equivale a cierre de gate ni a release. |
| **FAIL** | Una comprobación prevista se ejecutó y falló. Un FAIL nunca se convierte en PASS por reinterpretación. |
| **BLOCKED** | La comprobación no se ejecutó por una condición externa (autorización, identidad, entorno, secreto). No hay resultado. BLOCKED nunca se reporta como PASS. |
| **NOT RUN** | La comprobación no se ejecutó y no hay causa externa registrada. |
| **NOT VERIFIED** | No se pudo determinar; por ejemplo, ausencia de visibilidad de un permiso. |
| **NO-GO** | Dictamen de no avance a la siguiente fase, con la condición que lo desbloquearía indicada. |
| **OPEN** | Gate iniciado y no cerrado. |
| **Cerrado** | El gate tiene aceptación humana, evidencia congelada y fin de sus pendientes. |

Regla de AGENTS.md aplicada de forma estricta: **no se declara PASS cuando existen pruebas bloqueadas**. Un gate con BLOCKED permanece OPEN aunque todo lo ejecutable haya pasado.

## 5. Decisiones abiertas

| ID | Decisión | Efecto de no resolverla | Estado |
|---|---|---|---|
| OPEN-1 | Línea base canónica de código (no integrar D2-A/D2-B a la raíz, o integrar; inicializar Git o no) | Trabajo sobre el árbol equivocado; sin historial | Pendiente del Human Orchestrator. Recomendación vigente: no integrar hasta la aceptación de D2-B, y declarar la raíz como línea D1. |
| OPEN-2 | Corrección del harness de fixtures antes de A5 | A5 con residuos y limpieza no garantizada | **CERRADA** con alcance acotado: F1, F2, F3, F6 y F7 corregidos; F4 verificado pero **no atómico**; F5 mitigado y diagnosticado, no prevenido. Revisado de forma independiente. Ver [resultado](d2b/harness-correction-20261020/RESULT.md) |
| OPEN-3 | Identidad SQL autenticada reutilizable para `KulaniKDI_Test` | Sin ella, A3 no puede ejecutarse por la ruta de menor privilegio | **RESUELTA para A1 y A2**: el proceso corre ahora como `DESKTOP-F25O3MF\Israel`, login Windows sysadmin, y conecta sin elevación. **Pendiente para A3**: la ruta R1 exige la contraseña de `kdi_migrator` en consola privada. Ver [A3-MIGRATION-PACK.md](d2b/controlled-test-plan/A3-MIGRATION-PACK.md) |
| OPEN-3b | **Suficiencia de la evidencia de recuperación** antes de migrar | El plan dice «sin evidencia de recuperación suficiente acordada por DBA/humano, NO-GO migración» | Backup con `VERIFYONLY` correcto y rollback preparado, pero **sin restauración de ensayo** (A7, destructiva y aparte). La suficiencia la confirma el humano: migración aditiva e idempotente, con rollback a baseline |
| OPEN-4 | Evidencia de restauración de ensayo antes de migrar (A2) | NO-GO a A3 aunque A2 se autorice | Requisito de `PLAN.md` |
| OPEN-5 | Proveedor de CI y repositorio para D3 | D3 no puede arrancar | No seleccionado; no elegir por suposición |
| OPEN-6 | Trazabilidad requisito → implementación → prueba (AC01–13, UC01–05) | No se puede afirmar completitud de producto | No iniciada |
| **OPEN-7** | **Reemitir la documentación raíz bajo un hash nuevo y aceptar ese hash** (el `README.md` raíz y `docs/stage3/STATUS.md` están congelados por el manifiesto D2-A original y por el manifiesto D1 aceptado) | El índice raíz sigue afirmando que D2 está pendiente | Pendiente del Human Orchestrator. Es la **única** vía para corregir el índice raíz sin romper la cadena de aceptación. |
| **OPEN-8** | **Permisos del harness de fixtures.** El rol `kdi_tests` de `KulaniKDI_Test` solo tiene `GRANT SELECT` sobre los tres esquemas, y `SqlTarget` exige autenticación Windows integrada: el harness se ejecutaría como la identidad Windows del proceso, **nunca** como `kdi_test_runner`. El harness necesita `INSERT` y `DELETE` en siete tablas | Una corrida de A5 fallaría por permisos y podría dejar sesiones y auditoría huérfanas | **Bloqueante de A5.** Conceder a `kdi_tests` (decisión tomada) declara el conjunto de permisos del fixture, pero **no** hace que el harness pueda autenticarse con ese login: requiere un `SqlCredential` propio o una identidad Windows con esos derechos. Sin tocar ningún permiso, rol ni interruptor |
| **OPEN-9** | **Atomicidad de la limpieza administrativa.** Las siete sentencias de limpieza se ejecutan en autocomit; una interrupción deja estado parcial, y puede dejar una organización sin administrador, lo que viola la invariante global | Residuo y fallos encadenados en casos posteriores | Decisión de plan: aceptar por escrito con evidencia y recuperación manual, o variante transaccional. Documentado en el código, **no implementado** |

## 6. Desviaciones declaradas

Riesgo aceptado de forma expresa por el Human Orchestrator, con su alcance. No son pendientes de resolver: son decisiones tomadas y registradas.

| ID | Desviación | Alcance y riesgo residual | Decisión |
|---|---|---|---|
| **DEV-1** | **No se rotan las credenciales** de `kdi_migrator`, `kdi_app` y `kdi_test_runner` | Las tres credenciales están en un fichero de texto plano fuera del workspace, en el almacén de sesión local de la herramienta y en el historial de la conversación. Deben considerarse **expuestas**. El daño queda acotado a una instancia local con datos sintéticos, sin exposición de red. **A3 se aplicará con una credencial ya expuesta** | Human Orchestrator, 2026-10-21. La rotación está preparada y no se ejecuta. Detalle: [evidence/A1/A3-CREDENTIAL-DEVIATION.md](d2b/controlled-test-plan/evidence/A1/A3-CREDENTIAL-DEVIATION.md) |
| **DEV-2** | **A1 se ejecutó con la identidad Windows del operador** (`DESKTOP-F25O3MF\Israel`, sysadmin) en vez del operador aprobado `kdi_test_runner` | El script de A1 es de solo lectura y solo tiene guardas de destino, no de identidad. La desviación da **más** visibilidad que la prevista, no menos. El mismo script puede ejecutarse como `kdi_test_runner` si se requiere el operador exacto | Registrada en [evidence/A1/A1-A2-RESULT.md](d2b/controlled-test-plan/evidence/A1/A1-A2-RESULT.md) |
| **DEV-3** | **El backup de A2 lo ejecutó la identidad Windows**, no `kdi_migrator` | El rol `kdi_migrations` no tiene `BACKUP DATABASE`. Es una operación de infraestructura, no de migración, y cae fuera de la ruta de menor privilegio de A3 | Registrada en [evidence/A1/A1-A2-RESULT.md](d2b/controlled-test-plan/evidence/A1/A1-A2-RESULT.md) |
| **DEV-4** | **Los GRANT de A4 los concedió la identidad del operador**, no `kdi_migrator` | Los objetos D2-B **no tienen dueño** (`sys.objects.principal_id` es NULL) y SQL Server solo permite conceder sobre un objeto sin dueño a quien tenga `CONTROL`: con `kdi_migrator` se produce `Msg 15151`. El conjunto concedido es **idéntico** por ambas vías; cambia **quién concede**, no **qué se concede**, y los `DENY` de DDL no se ven afectados. `kdi_migrator` conserva sus permisos de migración sin tocar | Registrada en [A4-RUNTIME-GRANTS-PACK.md](d2b/controlled-test-plan/A4-RUNTIME-GRANTS-PACK.md) |
| **DEV-5** | La ruta de credenciales del operador estaba **rota** (`OperatorConnection.Create` lanzaba siempre) | Se corrigió en tres rondas con revisión independiente de code-reviewer y QA, y 14 tests nuevos. El defecto **está cerrado**; se documenta porque explica por qué N2 no se había podido ejecutar | [operator-credential-fix-20261021](d2b/operator-credential-fix-20261021/QA.md) |

## 7. Notas de trazabilidad documental

**ADRs.** `docs/source/kdi/adr/README.md` sigue declarando los cinco ADR como «Propuesto», y esa es la lectura vigente. D1 fue aceptado el 2026-10-17 como un tramo acotado derivado de ADR-001, ADR-002 y ADR-005; su recibo dice expresamente que la aprobación «no se amplía a D2/D3 ni a todos los ADR». Por tanto la puerta «ADR-001–005 aceptados» de la fundación sigue **Pendiente**, y no se fabrica aprobación. Este documento no reescribe los ADR: solo deja la referencia cruzada. (El índice de ADR está congelado por hash; véase §3.1.)

**DECISIONS.md.** `docs/stage3/DECISIONS.md` está congelado y conserva el texto de las decisiones D1, D2 y D3 tal como se emitieron. Su lectura vigente está resumida en §1 de este documento. No se le añade puntero por la misma razón de §3.1.

**Documentos que contienen una etiqueta superada.** Cuando un informe histórico dice «aceptación humana pendiente» y existe un recibo posterior, el proyecto no reescribe el informe: lo marca. Casos resueltos en esta reconciliación:

| Documento | Marcado en | Recibo que resuelve |
|---|---|---|
| `docs/stage3/d1/RESULT.md` | cabecera | `docs/stage3/d1/acceptance/HUMAN-APPROVAL.md`, `STATUS.json` |
| `docs/stage3/d2a/closure/identities-20261018/RESULT.md` | cabecera | `ACCEPTANCE.md` del mismo directorio |
| `docs/stage3/STATUS.md` (fundación) | — | No editable por §3.1; su estado actual se lee aquí y en `docs/stage3/d2b/STATUS.md` |
| `README.md` (raíz) | — | No editable por §3.1; véase OPEN-7 |

## 8. Documento índice por área

| Área | Documento de entrada |
|---|---|
| Estado | Este documento |
| Decisiones de gate y alcance | [docs/stage3/DECISIONS.md](DECISIONS.md) |
| Análisis funcional de fundación | [docs/stage3/FUNCTIONAL-ANALYSIS.md](FUNCTIONAL-ANALYSIS.md) |
| D1 | [docs/stage3/d1/RESULT.md](d1/RESULT.md) · [aceptación](d1/acceptance/HUMAN-APPROVAL.md) · [toolchain](d1/TOOLCHAIN.md) |
| D2-A | [docs/stage3/d2a/closure/identities-20261018/RESULT.md](d2a/closure/identities-20261018/RESULT.md) · [aceptación](d2a/closure/identities-20261018/ACCEPTANCE.md) |
| D2-B | [docs/stage3/d2b/STATUS.md](d2b/STATUS.md) · [GO-NO-GO](d2b/controlled-test-plan/GO-NO-GO.md) · [procedimiento](d2b/controlled-test-plan/PLAN.md) · [autorizaciones](d2b/controlled-test-plan/AUTHORIZATIONS.md) · [matriz de pruebas](d2b/controlled-test-plan/TEST-MATRIX.md) · [QA de preparación](d2b/controlled-test-plan/QA-READINESS.md) |
| A1 (bloqueado) | [paquete de operador](d2b/controlled-test-plan/A1-OPERATOR-PACK.md) · [comprobación de archivos](d2b/controlled-test-plan/A1-FILE-CHECK.ps1) · [plantilla de evidencia](d2b/controlled-test-plan/A1-EVIDENCE-TEMPLATE.md) · [procedimiento local](d2b/controlled-test-plan/evidence/A1/RECOVERY-LOCAL-WINDOWS.md) · [resultado del intento 2](d2b/controlled-test-plan/evidence/A1/RECOVERY-RESULT.md) |
| Validación portable | [docs/stage3/tooling/Kdi.Validate/](tooling/Kdi.Validate/README.md) · [informe de la reconciliación](reconciliation-20261020/RESULT.md) |
| Contrato HTTP (D1) | [docs/api/README.md](../api/README.md) |
| Especificación histórica de Etapa 1 | [docs/source/kdi/README.md](../source/kdi/README.md) · [ADRs](../source/kdi/adr/README.md) |
| Rol DBA y onboarding | [docs/agent-onboarding/dba-engineer/README.md](../agent-onboarding/dba-engineer/README.md) · [equivalencia de la evidencia de fase 2](../agent-onboarding/dba-engineer/sql-identities-phase2-20261018/README.md) |

## 9. Verificación de este documento

Este archivo no forma parte de ningún manifiesto congelado y no altera hashes de D1, D2-A ni D2-B. Los verificadores se ejecutaron **después** de redactarlo y de marcar los tres RESULT.md, con salida 0:

- `python docs/stage3/d2a/closure/identities-20261018/verify.py` → exit 0. Original D2-A `66ee6991…`, incremento aceptado `c31fe8c1…`, evidencia congelada verificada.
- `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` → exit 0, **195 archivos, `d106dac6…`**.
- `docs/stage3/d2b/controlled-test-plan/A1-FILE-CHECK.ps1` → exit 0: los tres hashes esperados y los dos verificadores.
- Manifiesto D1 (`e2bb22b7…`, 87 entradas): 87/87 hashes coincidentes contra el árbol de la raíz.

Durante la redacción se editaron por error dos archivos congelados (`README.md` de la raíz y `docs/stage3/STATUS.md`). El fallo se detectó con `verify.py` (`Candidate drift`, exit 1) y ambos se restauraron byte a byte, verificados contra sus hashes esperados. Se deja constancia explícita porque el procedimiento de detección y reversión es parte del valor de la cadena de manifiestos.

**Deriva preexistente entre manifiestos (no causada por esta sesión).** De las 87 entradas del manifiesto D1 aceptado, 15 tienen un hash distinto del árbol actual. Se comprobó una por una: **las 15 coinciden exactamente con el hash del manifiesto D2-A original** (`66ee6991…`, 121 entradas). La deriva está por tanto explicada en su totalidad por el incremento D2-A aceptado, que sustituyó a D1 en el árbol de la raíz. No hay ninguna entrada inexplicada. Consecuencia: el manifiesto vigente del árbol de la raíz es el de D2-A original, y el de D1 describe un estado anterior del mismo árbol.
