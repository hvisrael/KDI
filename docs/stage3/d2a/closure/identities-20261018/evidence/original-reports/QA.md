# ReviewResult — QA independiente D2-A

- type: `qa`
- agentId: `/root/d2a_qa`
- agent_type efectivo: `qa-engineer`
- candidateHash: `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f`
- verdict: **BLOCKED — no aceptación integral / no PASS global**
- Fecha: 2026-10-18.

TaskInput recibido del principal: autorización humana D2-A, `AUTHORIZATION.md`, AGENTS.md y `.codex/agents/qa-engineer.toml`; rutas E:/INSSOFT/KDI, evidencia D2-A y presupuesto operativo de 20 minutos. Propiedad de escritura exclusivamente este informe y evidence/qa/. Sin delegación, acceso IDI, conexiones SQL, migraciones ejecutadas por QA, cambios de producto ni producción. La autoridad de aceptación es Human Orchestrator.

## Pruebas ejecutadas por QA

| Comprobación | Resultado | Evidencia relativa a docs/stage3/d2a/ |
| --- | --- | --- |
| Inventario y hashes antes/después | Coinciden con candidateHash; sin drift | evidence/qa/manifest-before.txt; manifest-after.txt |
| Unitarias, binario Release existente | 23/23, cero fallos/omitidos | evidence/qa/unit-retry.log |
| API en proceso de pruebas | 7/7, cero fallos/omitidos | evidence/qa/api-retry.log |
| Arquitectura | 24/24, cero fallos/omitidos | evidence/qa/architecture-retry.log |
| Generación SQL offline para Dev y Test | Exit 0 en ambos; seis archivos SHA-256 idénticos al candidato | evidence/qa/generator-dev.log; generator-test.log; generator-comparison.json |

Total independiente: 54 pruebas .NET satisfactorias. Primer intento de invocación directa de los tres runners usó `--output Detailed`, opción del controlador dotnet test que el ejecutable xUnit no acepta: exit 3 antes de ejecutar pruebas. Se conservan unit.log/api.log/architecture.log y se corrigió la invocación sin cambiar producto. Las repeticiones finalizaron con exit 0. Las pruebas se ejecutaron sobre los binarios existentes; QA no recompiló y no afirma procedencia criptográfica de esos binarios, que no forman parte del manifiesto.

## Evidencia del principal examinada, sin atribuirla a ejecución propia

| Criterio | Evaluación y evidencia |
| --- | --- |
| Build Release cero advertencias/errores | build-release-corrected.log: 0/0. |
| Suite completa con SQL real | tests-corrected.log: 56 correctas, 0 errores, 0 omitidas; incluye dos pruebas SQL. SqlBaselineTests confirma uso de SqlClient y EF real, impersonación migrador, repetición, reverse generado en transacción y rollback, lectura/no escritura del testigo y rechazo runtime sysadmin. |
| Creación exclusiva y configuración | sql-preflight.txt identifica servidor 15.0.2190.7 y admin autorizado, sin filas PREEXISTING. sql-provision-journal.txt documenta ausencia de ambas DB, CREATE y configuración; sql-verify-dev.txt/sql-verify-test.txt muestran configuración, rutas, roles, schemas y permisos. 01-provision.sql bloquea si cualquiera existe y usa rutas de instancia por omisión. |
| Baselines e historiales separados | sql-baseline-dev.txt/sql-baseline-test.txt: tres tablas de historial, una migración canónica por módulo, sin tablas de negocio/probe. Código mantiene tres contextos vacíos, schemas e historiales propios. |
| Permisos mínimos positivos/negativos y rollback | sql-permissions-rollback-final.txt registra DDL/DML migrador, CONNECT aplicación, SELECT/ALTER aplicación denegados, lectura pruebas e INSERT denegado; probe ausente tras rollback. Limitado a testigos WITHOUT LOGIN. |
| Repetibilidad / corrección | sql-migrate-*-repeat.txt, tests-corrected.log y regeneración propia verifican repetición. Primer fallo 55/56 permanece en tests-final-first.log; corrección acotada de IDs de esta sesión está en 06-correct-session-baseline-ids.sql y sql-correct-ids-{dev,test}.txt. No se oculta ese fallo histórico. |
| API y frontend smoke | api-smoke-result.json registra liveness, metadata, errores, OpenAPI y readiness 503 sin identidad. playwright.log: 5/5, incluida shell real con API; web-checks.json: typecheck/lint/build exit 0. No acredita persistencia positiva runtime. |
| Auditoría dependencias | npm-audit-elevated.json: cero vulnerabilidades. restore-audit-final.log conserva NU1900 por conectividad; restore-audit-elevated.log posterior restaura los diez proyectos sin diagnóstico de vulnerabilidad. No es auditoría nueva ejecutada por QA. |
| Sin secretos / sin migración al arranque / alcance | Inspección de Program, PersistenceRegistration, SqlTarget, interceptor, contextos, scripts y documentación: configuración por entorno, Windows integrada requerida, destinos limitados, runtime rechaza sysadmin/dbcreator/db_owner/CONTROL, no Migrate/EnsureCreated en arranque. Baselines sin funcionalidad D2-B/C. |
| DDL registrado | Diarios SQL conservan texto por sqlcmd -e y resultados para provisión, permisos, migración, repetición y corrección. QA no ejecutó DDL. |

## Omisiones y bloqueos críticos

1. **BLOCKED:** no se suministraron ni vincularon tres identidades Windows reales distintas para migrador, API y pruebas. Los usuarios WITHOUT LOGIN sólo comprueban autorización dentro de la base mediante impersonación. No pueden acreditar autenticación externa ni pertenencia indirecta por grupos Windows o privilegios reales del login.
2. **BLOCKED:** no hay ejecución positiva de API/readiness con persistencia bajo identidad Windows runtime independiente y de mínimos privilegios. El rechazo de sysadmin y el 503 sin configuración son pruebas negativas válidas, insuficientes para este criterio positivo.
3. **BLOCKED:** faltan pruebas de migración y acceso desde los logins Windows independientes de migrador/pruebas, incluida matriz efectiva y negativas bajo esos tokens reales en ambas bases.
4. QA no repitió SQL, build, navegador ni auditoría de red; esos resultados se sustentan en evidencia examinada del principal. La prohibición de mutar DB en esta delegación impide repetir la suite SQL administrativa. La evidencia SQL no equivale a un test independiente ejecutado por QA.

No se identificó un fallo adicional reproducible en las 54 pruebas independientes ni en los seis scripts regenerados. El candidato conserva los bloqueos críticos de identidad y smoke positivo. Para cerrar: Human Orchestrator debe suministrar identidades existentes distintas, autorizar el binding aplicable, completar las pruebas bajo cada identidad y congelar/revisar cualquier cambio. Mantener D2-B/C detenidos y solicitar aceptación humana; este informe no la sustituye.
