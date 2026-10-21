# ReviewResult — D2-A

> **LECTURA VIGENTE:** este dictamen se emitió el 2026-10-18 sobre el candidato `66ee6991…` y su veredicto sigue siendo **BLOCKED para aceptación integral** por las validaciones de identidades Windows que estaban pendientes en ese momento. La aceptación humana posterior de D2-A fue **acotada** a un alcance técnico concreto y no lo convierte en PASS global; véase [../closure/identities-20261018/ACCEPTANCE.md](../closure/identities-20261018/ACCEPTANCE.md). Estado vigente del proyecto: [docs/stage3/STATE.md](../../STATE.md).

- type: `code-review`
- reviewer: `/root/d2a_review`
- agent_type efectivo: `code-reviewer`
- candidateHash: `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f`
- result: **BLOCKED para aceptación integral**, por validaciones obligatorias de identidades Windows pendientes. Sin hallazgos críticos/altos ni cambios de producto exigidos por esta revisión.
- fecha: 2026-10-18

TaskInput recibido del coordinador: revisión independiente del candidato congelado, presupuesto operativo 20 minutos, sólo escritura de este informe y evidencia `evidence/review/`. Autorización y fuentes: instrucción humana D2-A, AGENTS.md, `.codex/agents/code-reviewer.toml`, `AUTHORIZATION.md`, manifiesto, código y evidencia D2-A. No fui autor del candidato; no delegué, no ejecuté SQL, no instalé dependencias, no modifiqué producto ni accedí a IDI. La evidencia de pruebas citada fue ejecutada por implementación/coordinador, no se presenta como una repetición independiente de QA.

## Hallazgos reproducibles

### R1 — Baja: la prueba SQL puede atribuir a INSERT un fallo de SELECT

Ubicación: `docs/stage3/d2a/sql/03-permissions-and-rollback.sql:66–78`.

SELECT e INSERT del testigo de pruebas comparten TRY/CATCH. Cualquier error 229 establece `@denied=1`, tras lo cual se imprime `POSITIVE PASS: test SELECT; NEGATIVE PASS: test INSERT denied.` Si el SELECT falla por falta de permiso, INSERT nunca se ejecuta y el script declara ambas comprobaciones satisfactorias.

Reproducción propuesta para un entorno de pruebas expresamente autorizado: retirar temporalmente SELECT del testigo sobre la tabla de probe dentro de una transacción y ejecutar ese bloque; el 229 del SELECT llega al mismo CATCH y produce la declaración positiva. No ejecuté esta mutación sobre el candidato o las bases entregadas. La ruta es demostrable por inspección del control de flujo.

Recomendación: separar los bloques positivos/negativos o registrar un flag de SELECT completado y exigirlo antes de aceptar el error de INSERT. No bloquea por sí solo esta entrega: el diario actual `evidence/sql-permissions-rollback-final.txt` contiene `test_read_count`, y `tests/Kulani.KDI.SqlIntegrationTests/SqlBaselineTests.cs` verifica SELECT fuera de la aserción negativa de UPDATE.

### B1 — Bloqueo de aceptación: autenticación Windows independiente sin validar

Ubicación: `PRIVILEGES.md:13–15`, `IMPLEMENTATION.md`, `sql/01-provision.sql`.

Los tres testigos son WITHOUT LOGIN; no hay identidades Windows suministradas ni binding runtime real. No acreditan autenticación del migrador, API o pruebas desde sesiones independientes, ni readiness positiva de API bajo su propia identidad. La suite SQL es explícitamente administrativa y usa EXECUTE AS USER. El interceptor sí tiene evidencia negativa de rechazo de sysadmin, pero eso no sustituye el caso positivo.

Resolución requerida: el Human Orchestrator suministra identidades existentes distintas; validar membresías directas e indirectas, vincular exclusivamente los destinos autorizados y repetir autenticación, permisos positivos/negativos y smoke readiness bajo las identidades reales. No crear cuentas Windows automáticamente. Mantener BLOCKED hasta completar estas verificaciones.

## Evaluación técnica

Los tres contextos vacíos separan esquemas e historiales; las migraciones y snapshots no incorporan funcionalidades D2-B/C. IDs de 14 dígitos coinciden entre clases, scripts y aserciones. La corrección 06 está documentada como reparación única de filas creadas en esta sesión, valida destinos/forma exacta y usa transacción; no es un paso de instalación nueva. La primera ejecución fallida está conservada.

El aprovisionamiento comprueba ausencia de ambas bases antes de CREATE, usa rutas predeterminadas y cambia opciones sólo de ambas bases. Los scripts restringen host y catálogo; no hay cambios globales ni acceso a bases de otros proyectos. Roles no conceden db_owner/sysadmin/dbcreator a ejecución; aplicación sólo recibe CONNECT y denegaciones DDL, apropiado para el baseline sin tablas de negocio. No hay contraseña persistida.

La API registra contextos con interceptor que comprueba identidad/privilegios antes de entregar cada conexión EF. Rechaza sysadmin, dbcreator, db_owner, CONTROL y DDL del baseline, valida host/base reales y cierra la conexión al rechazar. SqlTarget requiere Windows integrada, destinos autorizados y cifrado. No hay Migrate/EnsureCreated al arrancar. Readiness no divulga excepciones/configuración; su alcance documentado es conexión/identidad, no validación completa de esquema.

El camino de migraciones está separado del runtime; generación offline produce scripts idempotentes, con guarda de historial desconocido y wrapper explícito de impersonación. Down conserva infraestructura y su reverse real se ejecutó dentro de transacción reversible en Test. La ausencia de autenticación Windows real queda expresamente reconocida.

## Evidencia examinada

| Comprobación | Evidencia / resultado observado |
| --- | --- |
| Integridad independiente | `python tools/d2a-manifest.py --verify`: inventario y hashes coinciden; mismo candidateHash antes/después de revisión |
| Build Release | `evidence/build-release-corrected.log`: 0 advertencias, 0 errores |
| Suites .NET | `evidence/tests-corrected.log`: 56 correctas, 0 errores, 0 omitidas; incluye SQL/arquitectura/API/unitarias |
| SQL y ausencia inicial | `evidence/sql-preflight.txt`, `sql-provision-journal.txt`, `sql-verify-{dev,test}.txt`: servidor/version/sesión admin, ausencia inicial, CREATE y configuración registrados |
| Permisos/rollback | `sql-permissions-rollback-final.txt`, `sql-baseline-{dev,test}.txt`: resultados positivos/negativos y probes ausentes; tres historiales esperados |
| Repetición/corrección | `sql-migrate-{dev,test}-repeat.txt`, `sql-correct-ids-{dev,test}.txt`, `generated-script-comparison.txt`: migraciones repetidas, corrección exacta y diferencias de generación cero |
| Auditoría | `restore-audit-elevated.log`: restore sin advertencias de vulnerabilidad; `npm-audit-elevated.json`: total 0. Resultado limitado a feeds y momento auditados |
| API/frontend | `api-smoke-result.json`: ocho comprobaciones satisfactorias con readiness negativa; `web-checks.json`: typecheck/lint/build exit 0; `playwright.log`: 5 pruebas correctas |

La revisión verificó estáticamente el candidato y leyó los resultados archivados. No reejecutó pruebas SQL mutantes ni afirmó autenticación real. Los informes/evidencia están excluidos explícitamente del manifiesto de producto; el informe puede añadirse sin descongelarlo.

Aceptación final exclusivamente humana. No iniciar D2-B/C ni producción. Este resultado no constituye PASS global.
