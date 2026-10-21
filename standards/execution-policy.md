# Política de ejecución

- Prohibidos: despliegues a prod, comandos DDL/DML sobre bases reales, cambios de credenciales, lectura de secretos, acciones irreversibles y rutas IDI.
- Las tareas no pueden exigir acciones prohibidas dentro de este harness. Escalar a un humano.
- El proyecto debe ser un Git worktree local y preferentemente limpio.
- `analyst-architect`, `qa-engineer`, `code-reviewer`: `read-only` y sin autorización de escritura.
- `engineer`: `workspace-write` sólo tras `--authorize-write`.
- Un paso no exitoso bloquea los siguientes; el orquestador captura código de salida, respuesta estructurada, versión y diff.
- Las pruebas reales son una lista explícita de comandos; nunca inferir PASS a partir del texto del LLM.
- El operador revisa los cambios antes de merge, migración o despliegue.
