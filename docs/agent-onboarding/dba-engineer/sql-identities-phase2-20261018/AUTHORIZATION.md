# Fase 2 — autorización humana y límites

Fuente: mensaje directo Human Orchestrator «KDI — AUTORIZACIÓN HUMANA D2-A / FASE 2» de esta conversación. No se atribuye fecha exacta de aprobación diferente de la registrada por la conversación.

Destino exclusivo DESKTOP-F25O3MF / MSSQLSERVER; KulaniKDI_Dev y KulaniKDI_Test. Exactamente tres CREATE LOGIN: kdi_migrator, kdi_app y kdi_test_runner; cinco CREATE USER; cinco ALTER ROLE ADD MEMBER. Migrator pertenece a kdi_migrations y app a kdi_application en ambas bases; test_runner solo a kdi_tests en Test. No GRANT nuevos, roles administrativos ni facultades fuera de esta matriz.

Condiciones: verificar inexistencia justo antes de crear; conflicto detiene únicamente operación afectada y dependientes, informando resultado. Contraseñas introducidas por humano exclusivamente mediante entrada local protegida, nunca conversación, archivos, appsettings, argumentos, logs ni manifiestos. CHECK_POLICY ON; propuesta revisada también CHECK_EXPIRATION ON. Sin alterar modo mixto, firewall, configuración global ni bases ajenas/IDI. Registrar identificador de operación y resultado sin secretos. No reutilizar automáticamente identidades conflictivas ni alterar contraseñas existentes. No eliminar identidades en rollback automático.

Se autoriza revisar y aplicar el parche preparado para autenticación SQL de API. Se implementa en .local/d2a-phase2-candidate, conservando originales y manifiesto D2-A sin cambios. Los diagnósticos del incremento y revisiones se guardan fuera del original.

No se deduce autorización para probes DDL/DML adicionales, migraciones/downgrades, cambios de historial, DROP, GRANT ni despliegue de la autorización exacta de trece operaciones. Las comprobaciones posteriores de identidad y permisos por SELECT son de solo lectura; si se requieren operaciones fuera de alcance se reportarán separadamente. No D2-B/C.

Especialistas: dba-engineer /root/d2a_phase2_sql_executor implementa helper sin ejecución inicial; engineer /root/d2a_phase2_api_implementation prepara e implementa en snapshot. Máximo dos simultáneos. QA y Code Review independientes no modificarán candidato. Un helper de provisión debe someterse a revisión independiente antes de su ejecución con credenciales humanas.
