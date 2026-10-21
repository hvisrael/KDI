# Procedimiento controlado D2-B / KulaniKDI_Test

**NO-GO para ejecución conectada. D2-B OPEN/BLOCKED.** Este documento prepara operaciones; ninguna conexión SQL, backup, migración, fixture, GRANT o cambio de infraestructura se ejecutó.

## Base verificable

Se leyeron AGENTS y RESULT.md, SQL-INVENTORY.md y AUTH-READINESS.md de continuation-20261020. Candidato `.local/d2b-candidate`: 190 archivos, manifiesto `docs/stage3/d2b/preparation-20261020/candidate-manifest.json`, SHA-256 `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102`. Verificador exit 0: candidate-integrity.log. Git raíz/candidato no inicializado; `git status` no puede acreditar árbol limpio (exit 128, git-status.log). No se inicializó Git.

> **Actualización 2026-10-20.** El candidato vigente es ahora `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos), tras la corrección del harness de fixtures ([resultado](../harness-correction-20261020/RESULT.md)). El **script A1 no ha cambiado** (`60dbad78…`) y la secuencia, las puertas y los criterios de este plan **siguen vigentes sin alteración**. Lo que este documento conserva es la revisión de preparación que se emitió sobre el candidato anterior: sigue siendo válida como análisis del plan, pero debe volver a confrontarse con el candidato vigente antes de ejecutar. Dos consecuencias nuevas: (a) el hash de candidato que hay que verificar es `d106dac6…`; (b) la autorización A5 tiene un **bloqueante de plan** nuevo: el rol `kdi_tests` de `KulaniKDI_Test` solo tiene `GRANT SELECT`, y el harness necesita `INSERT` y `DELETE` en siete tablas ([QA](../harness-correction-20261020/QA.md) §10.3).

Integridad D2-A original/aceptado/evidencia exit 0 en accepted-integrity.log. Manifiesto D1 conserva `e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193`. No se modificó candidato ni D1/D2-A.

## Inventario de ejecución

Paquete: `.local/d2b-candidate/tools/Kulani.KDI.Migrations/prepared-sql/manifest.json`; hashes Test copiados en TEST-ARTIFACTS.json. Solo los siete artefactos Test (cinco SQL y dos hashes del cuerpo EF) son pertinentes; Dev queda excluido.

| Artefacto / dependencia | Alcance |
|---|---|
| KulaniKDI_Test-identity.sql | 20261019040513_Accessidentity: Users, PermissionCatalog (ocho códigos), Grants, Sessions, AuditEntries |
| KulaniKDI_Test-organizations.sql | 20261019040514_Accessorganizations: Organizations, OrganizationMemberships, AuditEntries |
| KulaniKDI_Test-runtime-grants.sql | Propuesta de siete permisos por objeto; aprobación independiente |
| Dos *.rollback.sql | Destructivos sobre ocho tablas D2-B; vuelta al baseline, no a cero; sin ejecución autorizada |
| Dos *.migration.sha256 | Hash del cuerpo EF para CLI --apply, distinto al hash del SQL completo |
| AccessAdmin plan/apply | Plan conectado con applock y versiones; apply requiere plan hash, mismo SID, menos de 30 min y estado idéntico; escribe auditoría |
| SqlIntegrationTests | Ocho casos: dos baseline, uno acceso, cinco administración. No ejecutar conjunto completo |
| Toolchain | SDK fijo 10.0.103, EF/OIDC 10.0.13; Node 22.23.3, npm 10.5.0; locks .NET/npm existentes; Playwright 1.62.1/Chromium local |

Roles fijos de aplicación, sin tabla Roles. Integridad entre módulos validada en aplicación, sin FK cruzada: requiere prueba conectada. No instalación/restore ni auditoría nueva de dependencias; no se certifica vigencia del runtime.

Revisión estática: upgrades aditivos con guardas exactas de servidor/base/esquema/baseline/historia/versión EF, transacciones por módulo y bloques IF NOT EXISTS por migración. No DROP/TRUNCATE/DELETE ni alteración destructiva encontrada en upgrades; ON DELETE NO ACTION es restricción de FK. Idempotencia real todavía NO PROBADA: depende de historia coherente y se prueba con segunda ejecución. No corrigen drift si la historia dice aplicada pero faltan objetos. Se verificaron offline 14 hashes, 12 reproducciones y cuatro alcances rollback (sql-offline.log).

## Secuencia con puertas de autorización

1. **Ahora, sin SQL:** repetir freeze --verify y verificador offline; conservar hashes, revisión y aprobación futura fuera del candidato. Primera operación segura: `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` (ya realizada, repetible).
2. **A1:** operador existente aprobado conecta solo a Test. Usar SELECT de SERVERPROPERTY/DB_NAME, versión/edición/compatibility/recovery, esquemas/historias de los tres módulos, objetos/índices/restricciones, usuarios/roles/permisos efectivos y espacio/rutas disponibles con visibilidad existente. No SELECT de tickets/handles/identidades reales. NULL o falta de visibilidad es bloqueo. Confirmar baseline aceptado, ausencia de drift, no escritores y ausencia de D2-B previo inesperado. Capturar estado anterior de permisos explícitos y efectivos, sin cambiarlo. No inferir estado actual de evidencia histórica.
3. **A2:** DBA designa ruta privada NUEVA y espacio suficiente para backup completo COPY_ONLY WITH CHECKSUM; sin INIT/FORMAT ni sobrescribir archivos. Quiescer exclusivamente procesos de prueba aprobados para una referencia consistente de la corrida. Registrar archivo, tamaño, hora, backup_set, versión y resultado. VERIFYONLY WITH CHECKSUM sobre ese archivo: abortar ante cualquier error. Respaldos contienen datos sensibles; ACL/retención no se improvisan. SQL Server escribe historial de backup: esta operación no es read-only. Preparar evidencia de ensayo de restauración del mismo backup en destino aislado expresamente aprobado; no crear esa DB ni restaurar sin A7. Sin evidencia de recuperación suficiente acordada por DBA/humano, NO-GO migración.
4. **A3:** congelar hashes; ejecutar SQL Test identity y luego organizations con cliente SQLCMD compatible que respete GO y :ON ERROR EXIT, fail-fast, autenticación privada del operador aprobado. No usar credenciales en argumentos/env/log. La CLI --apply compara hash del cuerpo y no es la misma vía que los archivos SQL: no intercambiarlas bajo una aprobación de archivo. No ejecutar ningún script Dev ni GRANT. Por módulo conservar exit code y resumen redactado; detenerse al primer fallo. Un módulo puede quedar aplicado aunque falle otro: no hay transacción conjunta.
5. **Validación A1/A3:** revisar exactamente dos historias con baseline + Access; Portfolio sin cambios; ocho tablas, índices, FK/CHECK, rowversion y ocho permisos catálogo. Comparar baseline/estado y permisos previos. Repetir ambos upgrades bajo A3 y demostrar mismo inventario/semillas/historia, sin duplicación ni cambios de datos preexistentes. Comparar schema/semillas con consultas revisadas; número de filas por sí solo no prueba igualdad.
6. **A4:** solo tras aprobación de siete GRANT y snapshot previo; ejecutar archivo Test exacto, verificar privilegios efectivos por conexión real kdi_app. Probes DDL/DML negativos necesitan A8, no introducir privilegios para que una prueba pase. Runtime sin DELETE sesiones ni escritura en organización/membresía/grants. Si el snapshot revela privilegios extra, detener y presentar remediación separada, sin REVOKE automático.
7. **A5:** aprobar manifiesto de corrida con GUID propios y limpieza exacta. Resolver primero hallazgos de harness indicados debajo. Ejecutar únicamente casos D2-B seleccionados, sin SqlBaselineTests, en invocaciones secuenciales: acceso, limpieza verificada, administración. La organización temporal de acceso carece de administrador y puede interferir con la validación global de organizaciones activas en administración; comprobar invariantes preexistentes antes de esta última. No purge global ni limpieza por prefijo/actor compartido. Registrar IDs sintéticos no secretos, conteos antes/después y fallos; limpiar únicamente IDs aprobados y verificar ausencia de residuos. Si falla cleanup, detener y presentar recuperación por ID revisada.
8. **A6 y A5:** preparar entorno de checklist, iniciar piloto mismo origen y ejecutar TEST-MATRIX.md con operador y QA independiente. No habilitar KdiAccess ni iniciar autenticación real antes de infraestructura, esquema y permisos aprobados.
9. QA y revisión independiente verifican misma fuente, hashes SQL y evidencia real. Aceptación humana posterior; ni aprobación de ejecutar ni tests locales cierran D2-B.

## Fallo y reversión

Detener escritor/piloto autorizado y preservar diagnóstico redactado. Preferir deshabilitar acceso y corrección hacia delante; no reintentar ciegamente ni aplicar downgrade automático. Evaluar qué módulo confirmó COMMIT y revisar historia/objetos por lectura autorizada.

Rollback preparado de organizations y luego identity elimina datos/auditoría D2-B; solo bajo aprobación destructiva nueva, respaldo recuperable y ventana sin escritores. Guardas exigen historia D2-B exacta; no es idempotente. Preserva esquemas/historias baseline; no ejecutar reverse a cero. Revisar eliminación de permisos por objeto al eliminar objetos y comparar con snapshot previo. Para revertir únicamente GRANT retirar solo delta demostrado y aprobado, sin retirar derechos preexistentes.

Restaurar backup sobre Test reemplaza estado completo, puede deshacer cambios ajenos y afecta baseline: NO autorizado por este plan. Ensayo en destino aislado requiere autorización de destino/archivos/recursos; no usar WITH REPLACE ni cambios de acceso globales por iniciativa propia. El procedimiento concreto de RESTORE/MOVE queda pendiente de rutas, nombres lógicos y aprobación A7.

[COPY_ONLY](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/copy-only-backups-sql-server) permite respaldo independiente de la secuencia convencional. [VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql) comprueba legibilidad/completitud, pero no acredita estructuras de datos ni sustituye restauración de ensayo. Fuentes primarias consultadas; no se afirma recuperación probada.

## Bloqueos concretos y dictamen

NO-GO conectado por falta de A1; NO-GO migración por A2/A3 y evidencia de recuperación pendiente; NO-GO runtime por A4/A6; NO-GO suite SQL completa por operaciones fuera de alcance. Harness D2-B tiene riesgos de residuos: cleanup por actor compartido no cubre toda auditoría user-state y cancelación/fallo de creación puede impedir limpieza. No ejecutar administración hasta corregir/revisar harness con nuevo congelado autorizado o preparar procedimiento alternativo de fixtures/recuperación con GUID exactos y QA independiente. La autorización de fixtures por sí sola no resuelve esos defectos.

Revisiones independientes: SECURITY-SQL-REVIEW.md y QA-READINESS.md. Matriz de autorizaciones en AUTHORIZATIONS.md; criterios de aceptación en TEST-MATRIX.md. D2-B permanece OPEN/BLOCKED.
