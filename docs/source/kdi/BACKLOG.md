# Backlog inicial por dependencias

Orden técnico propuesto, sin estimaciones comprometidas. Cada fila se descompone al aprobar el incremento. Los predecesores deben estar aceptados, no solo iniciados.

| Orden / ID | Entregable | Depende de | Aceptación |
|---|---|---|---|
| 01 KDI-000 | Ratificar reglas iniciales, ADR y atributos de calidad | Etapa 1 revisada | Decisiones críticas y alcance explícitos |
| 02 KDI-001 | Compatibilidad real AI-ENGINEERING | Aprobación Etapa 2 | Parser, roles, delegación y permisos demostrados |
| 03 KDI-010 | Repositorio independiente, solución y frontend base | KDI-000, KDI-001, permisos/Etapa 3 | Builds y documentación reproducibles |
| 04 KDI-011 | Capas/módulos, contratos HTTP, observabilidad base y CI de validación | KDI-010 | Referencias acíclicas, pipeline validado sin deploy |
| 05 KDI-012 | Modelo base SQL, migraciones controladas y fixtures aisladas | KDI-011, ADR-003 aceptado | Persistencia, concurrencia y auditoría verificadas |
| 06 KDI-013 | Identity & Access y contexto de organización mínimo | KDI-012, ADR-004 aceptado | Autenticación y matriz de permisos/alcance probadas |
| 07 KDI-020 | Clientes UI/API/DB | KDI-013, aprobación Etapa 4 | AC01–06, AC11–13 |
| 08 KDI-021 | Proyectos UI/API/DB | KDI-020 | AC07–10 y controles transversales |
| 09 KDI-022 | Cierre del primer ciclo y release candidato | KDI-021 | E2E, QA/review, documentación y aceptación humana |
| 10 KDI-030 | Portafolio y ciclo de vida ampliado de proyectos | KDI-022, prioridades Etapa 5 | Reglas y trazabilidad aprobadas |
| 11 KDI-040 | Requirements & Intake, evolutivos y versiones | KDI-030 | Demanda → requisito con historial |
| 12 KDI-050 | Work Management y estimaciones versionadas | KDI-040 | Trabajo → demanda con estados e historial |
| 13 KDI-060 | Agile Planning: Scrum y Kanban | KDI-050 | Planificación sin duplicar autoridad del trabajo |
| 14 KDI-070 | Time Tracking | KDI-050 | Registro/corrección de horas trazables |
| 15 KDI-080 | Capacity Management | KDI-060, KDI-070 | Capacidad con fuente y periodo definidos |
| 16 KDI-090 | Quality Assurance | KDI-050, KDI-040 | Casos, ejecuciones y defectos correlacionados |
| 17 KDI-100 | Releases & Deployments | KDI-090 | Release aceptado y ejecuciones por ambiente auditables |
| 18 KDI-110 | Reporting & Analytics gradual | KDI-022; módulos fuente de cada métrica | Fórmulas, acceso y frescura visibles |
| 19 KDI-120 | Integrations | Contrato del módulo a integrar | Mapeos, idempotencia y errores probados |
| 20 KDI-130 | Notifications | Evento fuente aprobado | Preferencias, reintento y privacidad verificadas |

Reporting puede comenzar con clientes/proyectos tras KDI-022 y crecer según fuentes; no necesita esperar todos los módulos. QA, Time Tracking y planificación pueden desarrollarse como incrementos separados después de sus dependencias, sin activar todos los agentes a la vez. Integraciones/notificaciones se priorizan por casos concretos, sin crear un bus anticipadamente.
