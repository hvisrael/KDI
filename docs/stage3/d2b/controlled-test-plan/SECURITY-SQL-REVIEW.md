# ReviewResult — seguridad y SQL del plan controlado Test

> **Actualización 2026-10-20 — alcance de este documento.** Esta revisión se emitió sobre el candidato `a1cbed38…` (190 archivos) y **sus conclusiones de seguridad siguen vigentes**: la prohibición de borrar en bloque por el SID sintético compartido, la exigencia de ámbito exacto de los borrados y la separación entre las propuestas de permisos runtime y las del operador privilegiado. El harness se corrigió después y fue revisado de forma independiente sobre el candidato vigente `d106dac6…` (195 archivos): [resultado](../harness-correction-20261020/RESULT.md) · [revisión](../harness-correction-20261020/CODE-REVIEW.md) · [QA](../harness-correction-20261020/QA.md). El texto de esta revisión se conserva sin alterar como registro de la preparación.

TaskInput `D2B-CONTROL-PLAN-REVIEW`, intento 1; agente `/root/d2b_review`, agent_type efectivo `code-reviewer`; presupuesto 20 minutos. Fuente: nueva solicitud humana de preparar ejecución controlada Test, comunicada por el principal. Autoriza preparación documental y revisión estática; no ejecución SQL. candidateHash `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102`.

**Dictamen: NO-GO para ejecutar.** El candidato y los scripts preparados son revisables; faltan autorizaciones operacionales e identidades/entorno comprobados. No se abrió conexión SQL, ejecutó SQL/GRANT, modificó candidato ni infraestructura. Única escritura de este agente: este informe. No hubo redelegación ni acceso a IDI.

## Evidencia offline observada

Leídos AGENTS.md y `continuation-20261020/RESULT.md`, `SQL-INVENTORY.md`, `AUTH-READINESS.md`. Inspeccionados los scripts Test upgrade/rollback/permisos, manifiesto SQL, generador/CLI de migraciones, OperatorConnection, validación de destino, interceptor runtime y las tres clases SqlIntegrationTests.

`python docs/stage3/d2b/preparation-20261020/freeze.py --verify`: exit 0, 190 archivos y hash coincidente. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .local/d2b-candidate/tools/Kulani.KDI.Migrations/Verify-PreparedSql.ps1`: exit 0, 14 hashes preparados, 12 reproducciones y cuatro alcances de rollback. Esta opción afecta el proceso; no cambió política global. Son comprobaciones offline, no pruebas de idempotencia SQL ni permisos efectivos.

## Gates obligatorios

| Gate | Exigencia antes de ejecutar | Estado actual |
|---|---|---|
| G1 — autorización | Registrar aprobación concreta de servidor DESKTOP-F25O3MF, solo KulaniKDI_Test, operador existente, ventana, artefactos/hashes y operaciones. Separar lectura, DDL, DML/limpieza, GRANT, recuperación y downgrade. La variable de opt-in no es autorización. | NO-GO |
| G2 — destino/baseline | En una fase conectada de lectura autorizada, comprobar instancia/base, historia y EF 10.0.13, baseline aceptado y estado real de tablas. Rechazar historia inesperada. No alcanzar Dev/Portfolio/D1/D2-A. | Pendiente |
| G3 — permisos existentes | Identificar principal efectivo de migración, fixture y runtime; inventariar permisos explícitos/efectivos y roles previos. No asumir que kdi_test_runner de lectura puede escribir ni elevarlo para hacer pasar tests. | Pendiente |
| G4 — recuperación | Operador define respaldo/recuperación verificable, ausencia de escritores y tratamiento de aplicación parcial por módulo y residuos de fixtures. Ninguna restauración/downgrade queda implícitamente autorizada. | Pendiente |
| G5 — selección de pruebas | Excluir SqlBaselineTests de la tanda D2-B; elegir seis casos D2-B y ejecutar las dos clases secuencialmente. Validar sintaxis del filtro/listado antes de habilitar conexión. | Requisito estático |
| G6 — aislamiento fixtures | Test sin otros escritores, inventario previo de sintéticos, identificación de residuos por ejecución, limpieza comprobada antes y después de cada clase; ninguna limpieza amplia por issuer/prefijo. | Pendiente |
| G7 — evidencia privada | Sin passwords en env/argumentos/JSON, ni tickets, cookies, tokens, HAR/traces/capturas autenticadas. Solo estados, conteos, tiempos e identificadores sintéticos necesarios para recuperación aprobada. | Requisito |
| G8 — cierre | Pruebas bloqueadas permanecen BLOCKED; QA independiente y revisión del resultado antes de aceptar D2-B. | Pendiente |

## Riesgos concretos del harness congelado

**H1 — No ejecutar SqlIntegrationTests completo.** `SqlBaselineTests.cs:29` incluye identity, organizations y portfolio; ejecuta MigrateAsync dos veces y en la línea 43 genera reverse hasta `0`, retirando temporalmente incluso el baseline dentro de una transacción. También usa EXECUTE AS sobre witnesses y otro caso presupone que la conexión administrativa será rechazada por runtime. Esto no equivale a ejecutar exclusivamente el paquete aditivo D2-B ni sus rollback hasta baseline. Excluir ambos casos en esta tanda; no modificar pruebas aceptadas ni otorgar impersonación/roles para adaptarlas. Cualquier prueba adicional requiere alcance y autorización propios.

**H2 — No paralelizar clases D2-B.** `D2bAccessSqlTests.cs:27-30` crea una organización activa cuyo único grant es projects.read. `AccessAdministration.cs` comprueba que TODAS las organizaciones activas conserven administrador al finalizar cada operación privilegiada. Una ejecución paralela de las dos clases puede observar esa organización temporal y fallar por el invariante. Ejecutar Access y Administration en invocaciones secuenciales, comprobar limpieza intermedia y exigir que organizaciones activas preexistentes cumplan el invariante. La concurrencia interna de los casos Administration debe conservarse: es su objeto de prueba.

**H3 — Limpieza no garantizada frente a fallo/cancelación.** Access crea el usuario antes del try/finally (`D2bAccessSqlTests.cs:23-25`) y su finally reutiliza el token de cancelación (`:48-53`); al cancelar, los DELETE pueden fallar. Las limpiezas son secuencias de operaciones, sin transacción conjunta, y los GUID de fixtures se conservan principalmente en memoria. Un fallo después de commit o durante cleanup puede dejar residuos. No asumir rollback automático del conjunto, ni que Dispose limpie datos. Antes de la ejecución se necesita un procedimiento de detección/recuperación de residuos identificados inequívocamente, aprobado y sin borrar datos previos. Si ese procedimiento no puede concretarse sin cambiar harness, mantener NO-GO y proponer un candidato posterior separado.

Existe además un residuo determinista identificable por inspección: la prueba `Disabled_user_does_not_gain_access_when_membership_is_enabled_concurrently` ejecuta `user-state` sin OrganizationId. `AccessAdministration.cs:49` registra esa acción con ActorId del operador sintético, TargetId del usuario y OrganizationId null. `D2bAdministrationSqlTests.cs:127` limpia identidad por ActorId del usuario o OrganizationId de la corrida; ninguno coincide con esa entrada. Por tanto, la limpieza actual no borra esa auditoría. Un procedimiento alternativo debe identificar exactamente TargetId/acción/corrida y conservar cualquier evidencia de auditoría que la aprobación exija; no borrar todo por el SID sintético compartido.

**H4 — Prueba de revocación acotada.** El caso Access carga la versión observada, revoca desde otro contexto y luego intenta renovar: prueba un intento obsoleto real secuencial. No ejecuta renovación y revocación simultáneamente. No rotularlo como prueba completa de carrera SQL; la carrera simultánea permanece pendiente con diseño/autoridad propios.

## Permisos y credenciales

La propuesta Test de permisos ya no contiene DELETE Sessions. SHA-256 del archivo completo: `CB34B1ACDEBCE32163E37125B3A79BD9810A0EC0C2826A0C7DD63B6CE2C3C139`. Concede Users SELECT/INSERT, catálogo y grants SELECT, Sessions SELECT/INSERT/UPDATE, identity.AuditEntries INSERT y Organizations/Memberships SELECT. No concede permisos para ejecutar fixtures completos. Nuevos GRANT siguen prohibidos bajo autorización actual; no ejecutar este script sin decisión específica. Capturar estado previo y revertir solamente el delta aprobado; no REVOKE ciego.

Los fixtures D2-B usan `KDI_SQL_TEST_CONNECTION` con SqlTarget.Validate(testOnly:true), sin SqlCredential. El validador requiere Integrated Security cuando no hay SqlCredential y rechaza User ID/Password. Por tanto, la identidad efectiva es la del proceso Windows; la consola privada kdi_migrator de las herramientas no autentica estas pruebas. No introducir contraseña en esa variable ni suponer que la credencial del CLI se propaga al runner. Debe designarse una identidad existente autorizada y comprobar permisos efectivos. Si falta, detenerse y solicitar una decisión; no crear login/usuario/GRANT ni cambiar roles globales.

El CLI de operador sí solicita kdi_migrator mediante consola privada no redirigida, mientras API usa kdi_app y secreto OIDC mediante sus flags de lectura privada. Sus valores no son argumentos. No ejecutarlos desde un runner que capture esa consola. Las herramientas de operador usan Encrypt=True y TrustServerCertificate=True hacia la instancia fija local; esto no acredita validación del certificado SQL ni autoriza un cambio global TLS. El flujo OIDC mantiene validación HTTPS del proveedor.

## Migraciones y reversión

Test identity upgrade SHA-256 `B18B5160371F20FDB8EAE6AD86E0973846E6219783F4A25348BADA35DD389CE3`; organizations upgrade `5DD317EEF1257AD60368AD04140FA7E8437E8203E89090CB5BF7FE8700BAC63A`. Guardas de destino/baseline/historia preceden el cuerpo EF idempotente. Usar ejecución SQLCMD compatible con `:ON ERROR EXIT` y abortar ante cualquier fallo; no ejecutar lotes seleccionados saltando guardas. La primera aplicación y la repetición requieren autorización explícita y evidencia propia.

Los hashes `.migration.sha256` identifican el cuerpo EF, no el archivo completo con guardas. No sustituir autorización de scripts preparados por un hash de cuerpo o afirmar que la vía CLI --apply es el mismo artefacto SQLCMD. Fijar una sola vía de ejecución revisada y su hash.

Cada módulo confirma su transacción por separado. Si identity se aplica y organizations falla, detener y evaluar; no presumir atomicidad del paquete ni ejecutar automáticamente rollback/GRANT. Preferir conservar datos y corrección hacia delante.

Rollback Test identity SHA-256 `0E68B23EA8BC482691A1FC92AE0A53540E4A85BE53CB8196F8CAAEF8A7653402`; organizations `880E6A039BF1DDF58586D69CB726650661FFFD871DE96A55B681F9C5EE15E502`. Eliminan tablas D2-B y su registro Access; conservan baseline, esquemas e historial. No son idempotentes; su ejecución destruye usuarios, organizaciones, membresías, permisos, sesiones y auditoría D2-B y exige autorización separada, respaldo y ausencia de escritores. No son pasos automáticos de cleanup de fixtures.

## Revisión documental del plan

Se leyeron `PLAN.md`, `AUTHORIZATIONS.md` y `TEST-MATRIX.md` preparados por el principal. Incorporan puertas A1–A8, primera autorización limitada a lectura Test, separación de GRANT/DDL/DML/infra/recuperación, hashes de archivo completos, exclusión de baseline/Portfolio, selección secuencial de las clases D2-B y bloqueo hasta resolver harness/recuperación. El checklist exige limpieza de Access antes de Administration; debe conservarse ese mismo requisito en la secuencia operacional detallada. La concurrencia interna de los casos Administration no se elimina.

El plan distingue COPY_ONLY/CHECKSUM/VERIFYONLY de una restauración probada, reconoce escritura de metadatos de backup y exige evidencia de recuperación suficiente acordada con DBA/humano. A7 no queda autorizado por el backup ni permite restaurar Test sobre la base aceptada por iniciativa propia. La decisión pendiente sobre TrustServerCertificate local y sus límites también queda explícita. No se realizó consulta de documentación remota adicional en esta revisión; esas referencias proceden del plan del principal.

Se verificó independientemente `TEST-ARTIFACTS.json` por lectura/hashes/tamaños: siete entradas Test, todas con prefijo KulaniKDI_Test y SHA-256/tamaño coincidentes con los archivos congelados; exit 0. Freeze repetido al final: exit 0, mismo candidateHash y 190 archivos.

**Preparación documental revisada; NO-GO conectado confirmado.** Los gates y limitaciones están identificados de forma suficiente para pedir A1 concreta al humano, no para ejecutar migraciones, fixtures o permisos. Los seis casos D2-B no deben ejecutarse todavía con la limpieza congelada actual. Resolver recuperación de GUID/residuos y la entrada Admin.user-state mediante un procedimiento alternativo revisado o un nuevo candidato autorizado antes de A5. La sintaxis del filtro y el listado exacto deben comprobarse antes de habilitar conexión, sin declarar que ya se probaron en este review.

No hay PASS global ni cierre D2-B. Siguiente acción concreta: principal presenta A1 con operador existente y consultas revisadas, conservando A2–A8 pendientes; si A1 se autoriza, capturar metadatos mínimos y decidir con esa evidencia la preparación siguiente. Este informe no concede autorización para conectar.

## Addendum documental — intento 2

TaskInput del principal: mismo alcance, intento 2, presupuesto cinco minutos. Revisados estáticamente `01-preflight-readonly-REQUIRES-A1.sql` y su referencia en `GO-NO-GO.md`; **no ejecutados**. SHA-256 del script: `60DBAD787EC5B33A421F6944549A78962314603C6FDA8F0F8A96B88683BBF79C`.

El script contiene SET NOCOUNT, guardas de servidor DESKTOP-F25O3MF/base KulaniKDI_Test y SELECT de metadatos, historias de migración, tablas y permisos DATABASE de la conexión actual. La lectura Portfolio se limita a esquema/tablas/historia para comprobar preservación; no migra ni modifica ese módulo. No contiene DDL/DML, GRANT/REVOKE, EXECUTE AS, backup/restore, contraseñas, tokens ni consultas de filas de aplicación. USER_NAME y ORIGINAL_LOGIN son metadatos de identidad del operador, no credenciales; conservarlos solo en la evidencia operacional aprobada. :ON ERROR EXIT exige cliente SQLCMD compatible; un error de permisos/visibilidad debe bloquear la continuación.

La revisión no certifica sintaxis ejecutada ni resultado del destino real. El lote no comprueba por sí solo todos los permisos efectivos por objeto/identidad, índices/restricciones, drift, capacidad de backup o restauración. GO-NO-GO explicita correctamente esas limitaciones y mantiene A1 pendiente. No hay sustitución del snapshot completo ni ampliación implícita de A1.

Freeze repetido después de revisar: exit 0, 190 archivos, candidateHash sin cambios. El script y este addendum están fuera del candidato. **Sin hallazgos adicionales confirmados en este delta documental; NO-GO conectado continúa hasta autorización humana A1 aplicable.**
