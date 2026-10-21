# Riesgos y decisiones pendientes

| ID | Riesgo / decisión | Tratamiento y puerta |
|---|---|---|
| R01 | Documentación Codex más reciente que CLI 0.163.0 | Parser positivo/negativo y carga real en Etapa 2; no asumir compatibilidad |
| R02 | Rol de revisión hereda escritura | Sandbox separado si se exige read-only efectivo; comprobar permisos antes del flujo |
| R03 | Límites temporales/costo solo documentados | Supervisor y medición pendientes; no prometer enforcement TOML |
| R04 | Workspace actual sin repositorio Git reconocido | Sin worktrees/commits afirmados; crear repositorio KDI solo en Etapa 3 autorizada |
| R05 | Ruta KDI sin permiso de escritura actual | No crear ni sondear mediante escritura; habilitar workspace y aprobar fundación |
| R06 | Capas compartidas diluyen modularidad | Propiedad, contratos, pruebas de arquitectura; extraer proyectos por módulo solo con evidencia |
| R07 | Carrera archivo cliente / alta proyecto | ADR de transacción/locks y prueba concurrente SQL antes del caso funcional |
| R08 | Reglas de organización y proyectos internos ambiguas | Ratificar cliente obligatorio, pertenencia múltiple y alcance antes del esquema |
| R09 | IdP, permisos, browser auth y sesiones sin decisión | ADR-004 con OIDC, cookies/tokens, CSRF/CORS y matriz de acceso |
| R10 | Versiones SQL/frontend/tooling no fijadas | Matriz de compatibilidad, licencia y versiones exactas al fundar; sin instalar ahora |
| R11 | Volumen, SLA, RPO/RTO y retención desconocidos | Taller de atributos y criterios medibles, sin compromisos inventados |
| R12 | Copia implícita de IDI o doble fuente documental | Sin lectura/copia IDI; transferencia controlada de documentos KDI al fundar |
| R13 | Inflación del primer incremento | Limitar a clientes/proyectos y fundación mínima; resto en backlog |
| R14 | Infraestructura CI/CD, hosting y secretos no elegidos | ADR y entornos aprobados; pipeline de validación antes de publicación |

## Decisiones a resolver antes de Etapa 3

Aprobar ADR-001–005 o ajustarlos; confirmar política de organizaciones, clientes/proyectos internos y archivo; seleccionar proveedor de identidad, SQL/ambientes y versiones; definir volumen/objetivos medibles y presupuesto; resolver repositorio, licencia, proveedor CI y acceso a paquetes. La dirección de stack solicitada se conserva como base propuesta, no como instalación autorizada.

## Antes de Etapa 4

Ratificar AC01–13, matriz de permisos y alcance, normalización/unicidad, concurrencia y comportamiento HTTP; validar fundación e identidad con datos sintéticos. No hay decisiones humanas pendientes que autoricen seguir automáticamente por silencio.
