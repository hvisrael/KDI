## Orquestación de AI-ENGINEERING

Este proyecto utiliza especialistas personalizados de Codex.

El agente principal coordina las tareas y conserva la
responsabilidad de integración y aceptación técnica.

Perfiles:

- analyst-architect:
  requisitos, arquitectura y criterios de aceptación.

- engineer:
  implementación de cambios autorizados.

- qa-engineer:
  validación independiente y pruebas.

- code-reviewer:
  revisión independiente de código y riesgos.

Reglas:

1. Máximo dos subagentes activos simultáneamente.
2. No simular delegaciones.
3. Identificar el agent_type efectivo de cada subagente.
4. Mantener separados implementación, QA y revisión.
5. QA y code-reviewer no deben modificar el candidato
   que están evaluando.
6. Reportar identificador, tarea, resultado y evidencia.
7. No acceder al repositorio IDI.
8. No desplegar a producción sin autorización humana.
9. No declarar PASS cuando existan pruebas bloqueadas.