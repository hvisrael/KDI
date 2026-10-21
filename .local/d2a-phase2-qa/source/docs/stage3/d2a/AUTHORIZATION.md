# D2-A — autorización y TaskInput

Autoridad: instrucción humana «AUTORIZACIÓN DE IMPLEMENTACIÓN COMPLETA» recibida el 2026-10-18. Sustituye estados PROPOSED/NOT_STARTED únicamente para D2-A. No autoriza D2-B/C ni producción.

Destino exclusivo: DESKTOP-F25O3MF, instancia predeterminada MSSQLSERVER, SQL Server 2019, bases KulaniKDI_Dev y KulaniKDI_Test. Windows Authentication administrativa para provisión. No IDI ni otras bases de producto. Consultas de metadatos en master exclusivamente para confirmar servidor, privilegios y existencia de estos dos nombres. No configuraciones globales.

Trabajo autorizado: aprovisionamiento si ambas bases ausentes; roles separados, mínimos privilegios, identidad runtime independiente; EF Core 10 con SQL Server, contextos identity/organizations/portfolio, historial propio y migraciones iniciales sin negocio. Sin migración al arranque ni secretos.

Rutas: src/, tests/, tools/, configuración raíz de solución y docs/stage3/d2a/. Preservar evidencia histórica D1. La raíz no contiene repositorio Git; manifiesto SHA-256 sustituye identificación de commit.

Especialista de implementación: /root/d2a_engineer, agent_type efectivo engineer. Presupuesto operativo asignado por coordinador: primera implementación hasta 45 minutos; no constituye aceptación humana de estimaciones históricas ni presupuesto monetario. Revisar progreso antes de excederlo. QA y revisión separados tras congelado, máximo dos especialistas activos simultáneamente, sin modificar candidato evaluado.

Criterios: build Release cero warnings/errores; suites unitarias/API/arquitectura; SQL real integración/migración/permisos/rollback; ambas bases configuradas; identidades independientes y mínimos privilegios efectivos; reproducibilidad; smoke API/frontend; auditoría dependencias; manifiesto congelado; QA y Code Review independientes. Toda verificación bloqueada impide PASS global.

DDL: conservar SQL ejecutado y resultado por operación. Nunca sobrescribir/modificar bases encontradas preexistentes. No crear cuentas Windows. Se pidió al humano nombres de identidades existentes para migraciones/API/pruebas. Si faltan, preparar vinculación y documentar bloqueo de ejecución real con esas identidades. Usuarios WITHOUT LOGIN sólo sirven para pruebas de permisos SQL por impersonación, no acreditan autenticación independiente de la API.

Aceptación final: solicitar aceptación humana de D2-A; no continuar B/C.


## Incremento Fase 2 de identidades SQL

Autorizacion humana especifica recibida en conversacion: tres CREATE LOGIN (kdi_migrator/kdi_app/kdi_test_runner), cinco CREATE USER y cinco ALTER ROLE ADD MEMBER conforme PRIVILEGES.md; sin GRANT nuevos, roles administrativos, cambios globales ni otras bases. Revalidar ausencia justo antes de cada alta y detener operaciones afectadas por conflictos. Contraseñas solo entrada local protegida; evidencia sin secretos. Cambio API SqlCredential preparado autorizado para revisar/aplicar, en snapshot aislado del congelado original. No se deduce permiso para DDL/DML de probes, migraciones/downgrades o DROP adicionales.
