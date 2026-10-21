# D2-A — cierre del candidato (aceptación pendiente)

Estado global: BLOCKED, no PASS integral. Candidato congelado SHA-256 `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f` en `../candidate-manifest.json`. La raíz no tiene metadatos Git. Verificación: `python tools/d2a-manifest.py --verify` comprueba contenido, altas y bajas de los archivos incluidos; evidencia, informes y cierre están excluidos para evitar autorreferencia.

## Entregables ejecutados

Bases `KulaniKDI_Dev` y `KulaniKDI_Test` creadas en DESKTOP-F25O3MF/MSSQLSERVER después de verificar ausencia de ambas. SQL observado 15.0.2190.7, administrador Windows DESKTOP-F25O3MF\Israel. Rutas predeterminadas, compatibilidad150, recovery SIMPLE, AUTO_CLOSE y AUTO_SHRINK desactivados. Tres esquemas dbo-owned: identity, organizations, portfolio. Roles kdi_migrations/kdi_application/kdi_tests y tres testigos WITHOUT LOGIN por base. Usuarios Windows reales no vinculados.

EF Core SqlServer/Design10.0.13, contextos vacíos por módulo, historial independiente, baseline inicial por contexto y generador offline. No tablas de negocio, D2-B/C ni migración al arranque. Configuración por entorno sin secretos. Runtime comprueba identidad y privilegios y rechaza sysadmin, dbcreator y db_owner. Readiness falla cerrado sin configuración válida.

Matriz efectiva y limitaciones: `../PRIVILEGES.md`, resultados SQL `../evidence/sql-verify-dev.txt`, `sql-verify-test.txt`, `sql-permissions-rollback-final.txt`. Aplicación sólo CONNECT en baseline, DDL denegado; migrador DDL/DML limitado a los tres esquemas; pruebas lectura sin escritura. Impersonación acredita permisos efectivos, no autenticación independiente.

## Validación y evidencia

| Criterio | Resultado | Evidencia en ../evidence/ |
|---|---|---|
| Release | 0 errores, 0 advertencias | build-release-corrected.log |
| Unitarias/API/arquitectura/SQL | 56/56, 0 omitidas | tests-corrected.log |
| Configuración ambas bases | ejecutada satisfactoriamente | sql-verify-dev.txt, sql-verify-test.txt |
| Permisos positivos y negativos, rollback de probe | ejecutados satisfactoriamente | sql-permissions-rollback-final.txt |
| Migraciones ambas DB y repetición | ejecutadas bajo testigo migrador, sin cambios en repetición | sql-migrate-*-first.txt, sql-migrate-*-repeat.txt |
| Baseline exacto y rollback | 3 historiales/1fila por módulo; Test restaura reverse | sql-baseline-dev.txt, sql-baseline-test.txt, tests-corrected.log |
| Scripts reproducibles | 6/6 SHA-256 idénticos al regenerar | generate-reproducibility.log, generated-script-comparison.txt |
| Smoke API | 8 grupos satisfactorios, readiness negativa503 | api-smoke.log, api-smoke-result.json |
| Frontend typecheck/lint/build | salidas0 | web-checks.json, web-*.log |
| Navegador real | 5/5 | playwright.log, playwright-result.json |
| Arquitectura auxiliar | satisfactoria | structure-final.log, architecture-result.json |
| NuGet locked/audit all | salida0 sin avisos | restore-audit-elevated.log |
| npm audit | 0 vulnerabilidades | npm-audit-elevated.json |
| Login independiente migraciones/API/pruebas | BLOCKED | no identidades Windows suministradas |
| Smoke API+SQL con identidad runtime real/readiness200 | BLOCKED | depende de identidad runtime |

Las pruebas SQL .NET usan Windows admin exclusivamente como harness, impersonan usuarios limitados y comprueban rechazo sysadmin por EF runtime; no constituyen ejecución funcional de API con sysadmin. El smoke API se ejecutó sin cadena de persistencia. El certificado local se acepta sólo en el harness de desarrollo mediante TrustServerCertificate=true; no se modificaron políticas ni configuración global.

## Operaciones y correcciones

DDL aprovisionamiento completo y resultados en sql-provision-journal.txt con sqlcmd -e. DDL de migración y resultados en sql-migrate-*.txt. DDL de probes se revierte y su ausencia se comprueba. No eliminación de bases ni objetos preexistentes al incremento. No cambios globales de SQL/Windows, cuentas del sistema, secretos, IDI ni producción.

Intentos fallidos preservados: npm y NuGet bloqueados por red en sandbox, recuperados con ejecución aprobada; NU1902/NU1903 de dependencia Pkcs9.0.11 corregida con pin10.0.13 (hallazgo comunicado por engineer en la sesión; no se conserva transcripción del primer restore, y la auditoría final está registrada); primer probe SQL falló manejo de errores1088/3930, corregido por transacciones independientes y rollback antes de REVERT. Primera suite55/56 detectó IDs EF de12 dígitos; se normalizaron a14 y se corrigieron exclusivamente3 filas creadas en esta sesión por DB mediante06 y precondiciones estrictas, conservando tablas/esquemas. Evidencia sql-correct-ids-*.txt y tests-final-first.log. Los registros previos a corrección se conservan con sufijo before-id-correction. Verificador auxiliar requirió reconciliar su lock tras introducir EF, seguido de restore locked y comprobación exitosa.

## Separación de responsabilidades

Engineer `/root/d2a_engineer`, agent_type efectivo engineer: implementación y corrección, sin conexiones SQL. Principal: provisión SQL, verificación y pruebas finales, reconciliación lock auxiliar, congelado e integración. QA y Code Review independientes se documentan en `../reports/` sobre el mismo candidato, sin modificarlo. Sus dictámenes se incorporan abajo al finalizar.

## Pendiente para completar

Human Orchestrator debe suministrar tres identidades Windows existentes y distintas o definir su provisión por administrador. No se crearán cuentas Windows automáticamente. Con identidades disponibles: evaluar privilegios indirectos, crear/vincular logins y usuarios autorizados, asociar roles y ejecutar autenticación y pruebas positivas/negativas desde cada identidad; arrancar API con identidad runtime limitada y comprobar readiness200 y smoke conectado. Ningún resultado bloqueado se transforma en PASS.

No iniciar D2-B/C. Aceptación humana D2-A pendiente; este informe entrega candidato y evidencia, no declara cumplimiento integral.


## Dictámenes independientes del candidato congelado

- QA `/root/d2a_qa`, agent_type efectivo `qa-engineer`: BLOCKED para aceptación integral. Ejecutó independientemente54/54 pruebas unitarias/API/arquitectura y regeneró seis scripts SQL con SHA-256 idénticos; no ejecutó SQL mutante. Evidencia `../reports/QA.md` y `../evidence/qa/`. Primer intento con opción de runner inválida preservado y repetición corregida satisfactoria. Verificó manifiesto antes/después.
- Code Review `/root/d2a_review`, agent_type efectivo `code-reviewer`: BLOCKED para aceptación integral, sin hallazgos críticos/altos. Informe `../reports/CODE-REVIEW.md`; verificó manifiesto antes/después y no modificó producto/bases. Hallazgo R1 bajo: script03 agrupa SELECT e INSERT de testigo de pruebas en un CATCH229 y podría atribuir una denegación de SELECT a INSERT. En esta ejecución `test_read_count` y la prueba .NET de SELECT separada acreditan la lectura real. Seguimiento recomendado: separar comprobaciones para evitar falsos positivos en ejecuciones futuras; no cambia el estado obligatorio bloqueado por identidades.

Ambos dictámenes corresponden a SHA-256 `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f`. Implementación, QA y revisión permanecieron separados; máximo dos especialistas activos simultáneamente. No se solicita aceptación final como PASS: el Human Orchestrator debe revisar los entregables y resolver las identidades pendientes antes de aceptar el cierre D2-A.
