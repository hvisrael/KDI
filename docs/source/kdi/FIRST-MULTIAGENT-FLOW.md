# Primer flujo multiagente KDI

Diseño, no ejecución. La prueba técnica de Etapa 2 será inocua y separada del desarrollo real. Este flujo funcional requiere Etapas 2 y 3 aceptadas, escritura KDI habilitada y autorización de Etapa 4.

ID propuesto: `kdi-clients-projects-001`. Objetivo: administración de clientes/proyectos según KDI-UC01–05 y AC01–13. Principal coordinador bajo autoridad humana; cuatro perfiles preparados, ninguno activo.

| Paso | Responsable | Entrada | Entregable verificable / puerta |
|---|---|---|---|
| 0 | Humano + principal | Etapas previas, presupuesto, fuentes | TaskInput por rol; rutas, base y límites confirmados |
| 1 | analyst-architect: análisis | Visión, reglas propuestas | functional-analysis.md con reglas, escenarios, matriz rol/acción/alcance y preguntas; humano ratifica |
| 2 | analyst-architect: arquitectura | Análisis ratificado | architecture-design.md separado, ADR, modelo y contrato API; humano acepta decisiones críticas |
| 3 | engineer | Diseño aprobado y fundación | Implementación UI/API/DB, tests y TaskResult; un escritor |
| 4 | Principal | Archivos entregados | Diff, checks de rutas, manifiesto y candidateHash congelado |
| 5 | qa-engineer | Mismo candidato y AC01–13 | Pruebas independientes, evidencia y ReviewResult tipo qa |
| 6 | code-reviewer | Mismo candidato, diseño y pruebas | ReviewResult tipo code-review con hallazgos reproducibles |
| 7 | Principal | Ambos resultados | Consolidación de criterios, omisiones, riesgos y solicitud de decisión humana |
| 8 | Humano | Candidato y evidencia | Aceptación/rechazo con fuente auténtica; integración solo si está autorizada |

QA y reviewer pueden operar en paralelo después de congelar candidato si sus permisos/entornos lo permiten; nunca mientras el ingeniero modifica archivos. Los informes se escriben fuera del candidato. Si solo lectura efectiva es obligatoria, usar proceso/sandbox separado: los subagentes heredan permisos del principal.

El ingeniero no reemplaza revisión independiente ejecutando sus propias pruebas. El perfil combinado conserva análisis y arquitectura separados, con decisiones humanas entre ambos cuando corresponda. No crear orquestador de agentes dentro de KDI.

## Comunicación y consolidación

Mensajes del principal incluyen IDs, tarea, referencias, rutas y resultado esperado; respuestas estructuradas con hechos y evidencias. Usar herramientas reales disponibles y validadas del runtime; los contratos no inventan comandos de CLI. Ante bloqueo, no redefinir criterio: registrar causa y escalar. Una interrupción conserva intento y estado; reanudación crea intento nuevo.

Máximo 2 subagentes + principal, sin redelegación, 30 minutos por corrida y 2 ciclos de corrección propuestos. Presupuesto de costo/modelo debe aprobarse antes de correr. Si falla, fragmentar el caso en subtareas aceptables en lugar de prolongar sin límite. No autoejecutar deploy, merge o migraciones fuera del alcance autorizado.
