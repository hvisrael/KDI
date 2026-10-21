# Evidencia y límites de incorporación

Estado global: **BLOCKED; no PASS integral**. Registro del catálogo completado, revisión DBA entregada y perfil instalado preservado. Verificación reproducible en `verification.json` mediante `python docs/agent-onboarding/dba-engineer/verify_onboarding.py`.

## Delegaciones

| ID | agent_type efectivo | Tarea | Resultado y evidencia |
|---|---|---|---|
| /root/dba_readonly | dba-engineer | DBA-INC-001: D2-A y análisis sintético sin ejecución | Despacho real con collaboration.spawn_agent en esta conversación, respuesta final recibida; consolidada en DBA-REVIEW.md. Sin escrituras del especialista ni SQL |
| /root/dba_onboarding_qa | qa-engineer | Validación independiente del registro y evidencia | Informe separado QA.md; no autor ni editor del candidato |
| /root/dba_onboarding_review | code-reviewer | Revisión independiente de scripts, perfil y evidencia | Informe separado CODE-REVIEW.md; sin editar el candidato |

Presupuesto operativo asignado por el principal: DBA una revisión de máximo 15 minutos, QA y code-review 5 minutos cada uno; sin reintentos ni redelegación. La sesión CLI probó un especialista cuando DBA había terminado y antes de despachar QA y code-review. Nunca se solicitaron más de dos especialistas simultáneos. No se afirma enforcement técnico de ese máximo ni contabilidad de costo validada.

## Sesión nueva

CLI iniciado como proceso nuevo con exec, --ephemeral, --sandbox read-only y --skip-git-repo-check; no resume/fork ni cambios globales. Thread real `01a151fb-802b-7092-9145-c38cc108e0de`, salida de proceso 0. Archivos: new-session-prompt.txt, new-session-events.jsonl, new-session-result.md y new-session-stderr.txt. Eventos redirigidos por Windows PowerShell en UTF-16; respuesta final en UTF-8.

La respuesta del CLI declara catálogo dba-engineer, una delegación `/root/dba_inc_002` y análisis sin ejecución. **Esta declaración no se acepta como prueba completa de spawn**: JSONL contiene thread.started, mensajes, wait sin receiver_thread_ids y turn.completed, pero no un evento spawn con agent_type. Descubrimiento reportado por sesión nueva; trazabilidad independiente del despacho en esa sesión **NOT VERIFIED/BLOCKED**. No inventar el evento faltante. La delegación visible de esta conversación demuestra invocación efectiva actual, sin sustituir ese criterio pendiente.

## Registro y compatibilidad

El parser estándar tomllib acepta los cinco perfiles KDI y el perfil nuevo de catálogo con las mismas tres claves name/description/developer_instructions. SHA-256 del perfil KDI antes/después: c7b601ae6857a451238336bd2bfea727bcc7c48667445f8dd5f1a909c30f88b9; no se instaló ni sobrescribió porque existía. El catálogo tiene controles más explícitos; no afirmar que esa versión se ejecutó en KDI.

Registro AI-ENGINEERING aplicado con aprobación de escritura fuera del workspace mediante register_catalog.py: roles/dba-engineer.md, codex/candidates/agents/dba-engineer.toml y adiciones en roles/README.md y workflows/feature-development.md. Hashes en catalog-registration.json y verification.json. La aprobación técnica del comando no equivale a aceptación humana D2-A.

No se instaló jsonschema: la importación no está disponible. No se afirma validación JSON Schema automatizada de los encargos textuales. Se revisó el contrato TaskInput y se comunicaron objetivo, fuentes, rutas, restricciones, presupuesto y resultado esperado por la herramienta; no se presenta ese mensaje como instancia JSON validada.

## Integridad D2-A

Al inicio `python tools/d2a-manifest.py --verify` terminó correctamente: manifiesto SHA-256 66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f. Al final falla con Candidate drift. Comparación por ruta: únicamente AGENTS.md cambió; sin altas, bajas ni otros cambios en el alcance del manifiesto. Ahora incluye dba-engineer. Las herramientas de edición del principal y su script de registro no escribieron AGENTS.md; el especialista DBA reportó solo lectura. No se atribuye autoría a terceros sin evidencia adicional. Se conserva el archivo tal como está y el manifiesto original; no se revierte ni rebaselina para esconder la deriva.

La documentación nueva está en docs/agent-onboarding, fuera del alcance congelado. Los controles iniciales y finales no permiten declarar candidato intacto al cierre. Debe resolverse la deriva antes de aceptar cualquier revisión contra el manifiesto anterior.

## Restricciones respetadas y pendientes

Sin conexiones ni mutaciones SQL, logins/usuarios/permisos, instalaciones, políticas globales, IDI, producción o D2-B/C. La prueba SQL fue análisis estático deliberadamente; no representa un permiso efectivo observado hoy. D2-A conserva el bloqueo por autenticación real independiente y smoke API/readiness positivo. Registro final no implica aceptación global.
