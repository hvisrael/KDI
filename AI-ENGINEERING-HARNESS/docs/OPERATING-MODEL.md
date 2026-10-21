# Modelo operativo

1. **Intake:** tarea con objetivo, alcance, criterios de aceptación, rutas autorizadas y comandos de pruebas reales.
2. **Análisis:** `analyst-architect` produce plan, interfaces, riesgos y diseño.
3. **Autorización:** bandera humana requerida antes de permitir `engineer` en `workspace-write`.
4. **Implementación:** `engineer` cambia únicamente archivos autorizados; el control de rutas se inspecciona después de la ejecución y bloquea desviaciones. El operador debe revisar el diff.
5. **QA independiente:** `qa-engineer` observa el candidato sin modificarlo.
6. **Revisión:** `code-reviewer` independiente identifica defectos y riesgos.
7. **Gates reales:** ejecutar comandos de prueba definidos explícitamente y capturar código de salida.
8. **Aceptación:** PASS requiere salida estructurada válida de cada etapa, PASS de QA y reviewer, pruebas configuradas y aprobadas, integridad del candidato y rutas permitidas. Aprobación final de despliegue permanece humana.

Estados: `PASS` cuando todas las condiciones se cumplen; `FAIL` con evidencia de falla; `BLOCKED` con una dependencia, permiso o prueba no verificable. Nada significa `deployed`.

Límites: `max_parallel=2`; implementación de referencia secuencial (máximo efectivo 1). Cada rol es invocado como proceso CLI independiente; `agent_type` efectivo de `spawn_agent` no es aplicable ni se simula.
