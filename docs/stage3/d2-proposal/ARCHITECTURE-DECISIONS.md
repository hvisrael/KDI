# D2 — Registro de decisiones propuestas, límites y riesgos

Complemento del análisis funcional y del diseño técnico del engineer. Estado íntegro: PROPUESTO PARA APROBACIÓN. No sustituye ni modifica ADR-003/004 ni el candidato D1 aprobado. No contiene instrucciones de ejecución autorizadas.

## Límites y contratos

Identity posee identidad `(issuer, subject)`, roles/permisos/concesiones con identificadores de alcance opacos. Organizations posee organizaciones, membresía empresarial y clientes. Portfolio posee proyectos. Application compone autorización efectiva y contexto sin ciclo entre Identity y Organizations; Domain no consulta IdP. `ClientLifecycle` es coordinador superior, no escritura arbitraria entre módulos. UI recibe DTO públicos, no entidades EF.

Contratos propuestos a ratificar en diseño: resolución de membresía vigente; evaluación de permiso para actor/OrgId/ProjectId; consulta de elegibilidad de cliente; alta de proyecto coordinada; archivo protegido de cliente; registro/consulta de auditoría. Fachadas versionables exponen resultados y errores estables; no retornan IQueryable, DbContext ni entidades internas. Acceso directo a endpoint no omite validación servidor.

## Decisiones candidatas y alternativas

| Referencia | Recomendación para decidir | Alternativas / efecto |
|---|---|---|
| D2-DEC01 / ADR-003 | Una DB y esquemas/DbContexts/historial separados; EF Core; cuenta migradora distinta a runtime. | Contexto único reduce coordinación pero debilita propiedad; DB por módulo requiere consistencia distribuida fuera del piloto. |
| D2-DEC02 / ADR-003 | Concurrencia optimista de edición y coordinación transaccional explícita alta/archivo; orden único de locks y pruebas SQL concurrentes. | FK compuesta refuerza vínculo organizacional, no regla Active; consulta previa sola es insuficiente. Estrategia SQL concreta debe aprobarse en diseño del engineer antes de migraciones. |
| D2-DEC03 / ADR-004 | BFF con cookie Secure/HttpOnly y defensa CSRF, tokens fuera del browser; mismo origen preferido para piloto. | SPA Authorization Code + PKCE reduce estado servidor pero aumenta superficie de tokens y decisiones CORS/renovación. Proveedor no elegido ni supuesto disponible. |
| D2-DEC04 / ADR-004 | Autoridad de permisos local con membresía local por petición; claims IdP no conceden acceso organizacional automáticamente. | Permisos solo en claims requieren política explícita de propagación y revocación; rol global resulta insuficiente. |
| D2-DEC05 | Usuario multi-org, recursos mono-org, roles locales y lector de proyectos explícitos. | Piloto mono-org o sin alcance de proyecto requiere reducción humana explícita y ajustes a pruebas. |
| D2-DEC06 | Proyecto interno mantiene cliente obligatorio mediante cliente interno explícito. | Cliente opcional + tipo Internal modifica invariantes y necesita ratificar requisitos/ADR afectados. |
| D2-DEC07 | Auditoría mínima atómica, sin cuerpos completos; sin broker/outbox mientras no haya efecto externo obligatorio. | Auditoría eventual no acredita rollback conjunto; retención y consulta deben decidirse según necesidad operativa. |

## Riesgos y límites de aceptación

P1: no existe evidencia de infraestructura SQL/IdP; no confundir propuesta con recurso disponible. P1: carrera alta/archivo exige ambos flujos bajo la misma coordinación; bloquear cualquier vía alternativa que no tome el protocolo. P1: permisos o OrgId derivados del cliente no constituyen autoridad. P2: restricciones por organización deben cubrir FK/consulta/conteos y auditoría, no solo filtros UI. P2: revocación en IdP y suspensión local son mecanismos diferentes; acordar tiempos y probarlos. P2: collation SQL y normalización de código deben coincidir con comparación de aplicación. P2: bootstrap y recuperación no deben introducir administrador permanente sin control.

El diseño deberá documentar rutas/DTO HTTP, Problem Details sin filtración, comportamiento 401/403/404 según política anti-enumeración, precondiciones de versión y errores de conflicto. Debe explicitar vida de sesión, CSRF/CORS, rotación de secretos y tratamiento de auditoría antes de implementación.

## Puerta humana

Human Orchestrator decide cada DEC y el bloque autorizado; posteriormente se registra resolución con fuente y fecha, sin sobrescribir historia. Autorizar preparación documental no autoriza conexiones, restore adicional, provisión, migración, implementación ni despliegue. Infra/credenciales ausentes, presupuesto agotado o conflicto de rutas se escalan al principal. IDI y D2 ejecutable permanecen fuera del alcance actual.
