# D2 — Alcance propuesto para revisión humana

Estado: PROPOSED, no autorizado ni iniciado. Este documento presenta las decisiones ya pendientes en docs/stage3/DECISIONS.md y ADR-003/004; no modifica esos documentos ni toma decisiones en nombre del humano.

Alcance a considerar: persistencia modular en SQL Server e identidad/autorización para navegador, con pruebas de integración en recursos de prueba expresamente aislados. Requiere aprobar diseño y TaskInput con rutas, recursos, comandos, presupuesto y criterios antes de implementar o conectar servicios.

| Decisión pendiente | Información o resolución requerida |
|---|---|
| SQL de pruebas | Versión, edición y compatibility level; servidor y nombre de DB aislada; responsable de crearla, migrarla y limpiarla; permisos y mecanismo seguro de acceso. No registrar credenciales. |
| Propiedad de datos | Aprobar o ajustar propuesta de una DB, esquema/DbContext/historial de migraciones por módulo, rowversion y auditoría atómica. |
| Concurrencia cliente/proyecto | Elegir estrategia de atomicidad para alta de proyecto y archivo de cliente: aislamiento, restricciones, orden estable de locks y orden de migraciones; impedir vías que omitan coordinación. |
| Identidad | Elegir IdP y entorno/cuentas de prueba, subject estable y ciclo de vida. Sin contraseñas propias ni bypass de autenticación. |
| Flujo browser | Resolver BFF/cookie segura/CSRF frente a SPA Authorization Code + PKCE; sesiones, tokens, expiración y revocación; CORS según diseño. |
| Permisos y alcance | Definir múltiples organizaciones, administradores, matriz rol × acción × organización/proyecto, pertenencia y concesiones; denegación por defecto y pruebas de acceso horizontal. |
| Límite del incremento | Acordar casos concretos de D2 y qué comportamiento se implementará; no inferir autorización de módulos completos a partir del título persistencia/identidad. |
| Ejecución | Aprobar versiones/fuentes, recursos externos, responsables, presupuesto, perfiles y criterios verificables. Mantener fuera producción y recursos ajenos. |

Criterios propuestos para aprobar antes de ejecución: migraciones reproducibles en DB aislada; concurrencia de duplicado/edición/alta-archivo; rollback conjunto de auditoría; aislamiento por OrgId; matriz de autorización, recursos ajenos, expiración y revocación; revisión independiente del candidato final. Son propuestas, no criterios aceptados automáticamente.

Fuera de esta aprobación: proveedor/adaptador de CI y cierre D3, despliegue a producción, acceso a IDI y uso de sus credenciales/sesiones. Los seguimientos D1 (SDK/runtime y fixture negativo completo) se conservan como trabajo pendiente, sin ejecutarse por esta aceptación.

Siguiente decisión humana: aprobar o ajustar este alcance y resolver los datos de SQL/IdP y estrategia arquitectónica; después autorizar explícitamente el TaskInput D2. Hasta entonces no se inicia implementación, restauración adicional, migración ni conexión SQL/IdP.
