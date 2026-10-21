# D2-A identidades SQL — fase 1 completada, autorización de ejecución pendiente

No hay PASS integral D2-A. Fase 1: PASS de verificación actual de metadatos. Fases 2/3, nuevo candidato y revisiones finales: pendientes; no se ha aprovisionado ni iniciado la API SQL. El manifiesto de este paquete identifica únicamente la propuesta preparatoria, no sustituye el manifiesto D2-A.

Delegaciones reales separadas:

| Identificador | agent_type efectivo | Responsabilidad | Evidencia |
|---|---|---|---|
| /root/d2a_sql_identity_review | dba-engineer | SQL fase 1 solo lectura, matriz y propuesta | dba/TASKRESULT.md, tres salidas metadata, PROPOSAL-NOT-EXECUTED.sql |
| /root/d2a_api_identity_prepare | engineer | preparación segura API fuera del congelado | engineer/TaskResult.md, PROPOSAL.md, sql-runtime-credential.patch |

Ambos actuaron simultáneamente; máximo dos especialistas. Implementación/preparación no sustituye QA ni Code Review final.

## Resultado SQL observado

DESKTOP-F25O3MF/MSSQLSERVER: IsIntegratedSecurityOnly=0. Sin los tres logins ni usuarios homónimos en ambas bases. Roles dbo-owned kdi_migrations/kdi_application/kdi_tests coinciden con permisos baseline; solo miembros testigos WITHOUT LOGIN actuales. Guest sin CONNECT concedido. Lectura administrativa Windows se utilizó exclusivamente para metadatos, nunca como identidad de aplicación. Un fallo sandbox de SSL/SSPI se resolvió por reintento escalado autorizado; consultas exitosas salida 0.

## Propuesta exacta de autorización

Ejecutar únicamente sobre DESKTOP-F25O3MF, con nuevas precondiciones justo antes de mutar:

| Login | Usuario/rol en KulaniKDI_Dev | Usuario/rol en KulaniKDI_Test |
|---|---|---|
| kdi_migrator | kdi_migrator / kdi_migrations | kdi_migrator / kdi_migrations |
| kdi_app | kdi_app / kdi_application | kdi_app / kdi_application |
| kdi_test_runner | Sin usuario ni acceso | kdi_test_runner / kdi_tests |

Tres CREATE LOGIN con CHECK_POLICY=ON y CHECK_EXPIRATION=ON; DEFAULT_DATABASE Dev para migrator/app, Test para runner. Cinco CREATE USER y cinco ALTER ROLE ADD MEMBER según dba/PROPOSAL-NOT-EXECUTED.sql. Sin nuevos GRANT, server roles ni permisos administrativos. La expiración requiere rotación operativa, no se desactiva implícitamente. Default schema dbo no concede ALTER en dbo.

Contraseñas solo introducidas por el humano en consola local privada no grabada: SecureString, confirmación privada y SqlCredential para autenticación. Nada en conversación, archivos, argumentos, variables de entorno persistidas o evidencia. El helper específico se implementará/revisará antes de ejecutar; no hay prompts ni helper ejecutados. CREATE LOGIN requiere literal transitorio en memoria; nunca imprimir comando, excepción con payload ni utilizar sqlcmd -e. Cancelar al cambiar precondiciones o no disponer de entrada privada. No reutilizar ni alterar identidades existentes automáticamente. Ningún cambio global TLS/SQL/Windows ni instalación de herramientas.

## Cambio API requerido y preparado

La API congelada exige Windows integrated authentication. El parche externo propone soporte SQL opt-in de kdi_app con entrada consola privada en el propio proceso, SecureString y SqlCredential, sin secretos en connection string/configuración; conserva guardas de destino y privilegios runtime. Es cambio de diseño, no mero ajuste de configuración. Parche aún no aplicado/compilado: comprobación estática de aplicabilidad y sintaxis de scripts satisfactoria según engineer. No constituye prueba de producto. El smoke preparado escribe solo fuera del congelado; el smoke histórico no debe ejecutarse sobre el original porque escribe dentro de D2-A y espera readiness negativa.

Después de autorización específica se implementará y probará el incremento fuera de la línea base original o mediante nuevo candidato autorizado, conservando manifiesto original y snapshot recuperable. No se modificará silenciosamente el candidato vigente.

## Solicitud concreta al Human Orchestrator

Confirmar tres CREATE LOGIN, cinco CREATE USER y cinco ALTER ROLE anteriores; aprobar el cambio API preparado; y autorizar probes acotados sobre objetos nuevos de esta ejecución en Test (DDL/DML de migrador y denegaciones reales de app/runner) con transacción/rollback y comprobación de limpieza. En Dev solo autenticación/lectura/metadatos; runner debe fallar acceso. No ejecutar migraciones/downgrades ni modificar historiales/objetos existentes como prueba. Probes inesperadamente exitosos son FAIL, aunque se reviertan.

La autorización específica es requerida por la instrucción humana actual: «No ejecutar CREATE LOGIN hasta confirmar modo mixto y autorización específica para esa operación». Además, el encargo limitó engineer a preparar sin modificar todavía el candidato. No se solicita ni admite contraseña por conversación.

Fase posterior: autenticación real tres identidades, permisos positivos/negativos, API kdi_app, readiness SQL 200, smoke conectado y regresiones afectadas; nuevo congelado, QA/Code Review independientes del mismo hash y solicitud de aceptación humana. ACCEPTANCE.md contiene criterios. No D2-B/C, IDI, cambios globales ni despliegue.
