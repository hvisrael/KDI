# Revisión DBA D2-A — DBA-INC-001, intento 1

Autor independiente: `/root/dba_readonly`; agent_type efectivo `dba-engineer`, despachado mediante collaboration.spawn_agent. Perfil DBA recibido por runtime y confirmado por el especialista. Consolidación textual del principal de su respuesta; no se presenta como log crudo. Revisión estática completada, sin escrituras del especialista, conexiones SQL, ejecución de scripts, instalaciones o redelegación.

Resultado: **BLOCKED para aceptación integral D2-A**. Permisos actuales y autenticación independiente: **NOT VERIFIED** por esta revisión. No se modifica el candidato ni se autoriza avanzar D2-B/C.

| Responsabilidad conceptual | Rol implementado | Testigo documentado | Permisos definidos |
|---|---|---|---|
| kdi_migrator | kdi_migrations | kdi_migration_witness | CONNECT; CREATE TABLE; ALTER, REFERENCES, VIEW DEFINITION y DML en tres esquemas |
| kdi_app | kdi_application | kdi_application_witness | CONNECT; DENY CREATE TABLE/VIEW/PROCEDURE/FUNCTION/SCHEMA y ALTER en tres esquemas; sin GRANT DML |
| kdi_test_runner | kdi_tests | kdi_test_witness | CONNECT y SELECT en tres esquemas; sin concesiones de escritura o DDL |

Fuentes: `docs/stage3/d2a/sql/01-provision.sql:32` y `:70`; `PRIVILEGES.md:5`. Los tres nombres conceptuales no son tres logins ya vinculados.

## Hallazgos

1. Alta / BLOCKED: los testigos son WITHOUT LOGIN. La impersonación no prueba autenticación de procesos distintos ni membresías Windows indirectas. `docs/stage3/d2a/closure/RESULT.md:7` y `:30` reconocen identidades pendientes. Se requieren posteriormente sesiones independientes, permisos con tokens reales y smoke API/readiness positivo. No se crean identidades en esta tarea.
2. Baja: `sql/03-permissions-and-rollback.sql:65` agrupa SELECT e INSERT bajo un CATCH que admite error 229; una denegación de SELECT podría contarse como rechazo correcto de INSERT. La evidencia histórica `evidence/sql-permissions-rollback-final.txt:89` contiene test_read_count y `tests/Kulani.KDI.SqlIntegrationTests/SqlBaselineTests.cs:56` separa lectura y UPDATE negativo. Recomendar separación de bloques en un cambio futuro autorizado; no corregir el candidato congelado.
3. Informativo: permisos de migrador y SELECT de pruebas abarcan esquemas y futuros objetos (`01-provision.sql:37`). Revisar ese alcance al introducir datos sensibles en una etapa autorizada.
4. Informativo: conservar correspondencia entre responsabilidades, roles y futuras identidades; no renombrar objetos por inferencia.

## Autenticación y defensas estáticas

`src/Kulani.KDI.Infrastructure/Persistence/SqlTarget.cs:16` exige destino autorizado Dev/Test, Windows integrada, cifrado y ausencia de credenciales SQL, attach y failover. `RuntimePrivilegeInterceptor.cs:10` comprueba servidor/DB, membresía kdi_application y ausencia de sysadmin/dbcreator/db_owner, CONTROL, CREATE TABLE/SCHEMA y ALTER en tres esquemas; cierra conexión al rechazar (`:23`). `IMPLEMENTATION.md:15` circunscribe TrustServerCertificate=true al certificado local de desarrollo; su uso fuera de ese entorno no está acreditado. El harness .NET declara administrador e impersonación (`SqlBaselineTests.cs:12`), sin constituir identidad real del test runner.

Evidencia histórica consultada, no repetida: `evidence/sql-verify-dev.txt:102` muestra CONNECT y ausencia de DDL para aplicación; `sql-permissions-rollback-final.txt:83` documenta migrador permitido, aplicación denegada, lectura de pruebas e INSERT denegado y rollback del probe. No acredita ausencia de deriva posterior.

## Prueba sintética sin ejecución

`SELECT name FROM sys.database_principals`: lectura de metadatos sin mutación. Visibilidad depende del contexto y permisos; no demuestra permisos efectivos, membresías transitivas ni autenticación independiente. Análisis completado; ejecución deliberadamente NOT VERIFIED.

`GRANT ALTER ON SCHEMA::identity TO kdi_app`: modificación rechazada por exceder el alcance y contradecir la separación aplicación/migrador. kdi_app no es el principal implementado documentado; no asumir existencia. Un GRANT no debe suponerse capaz de eliminar un DENY existente. No se ejecutó ni se sustituyó por otra sentencia.

No se concede aprobación humana ni PASS integral.
