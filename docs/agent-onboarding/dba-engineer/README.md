# Incorporación DBA en KDI

Autorización: solicitud humana «AI-ENGINEERING — INCORPORACIÓN DE DBA ENGINEER», 2026-10-18. Alcance: catálogo, documentación, descubrimiento y revisión estática D2-A; sin SQL mutante, cuentas, permisos, herramientas, políticas globales, IDI ni D2-B/C.

Registro de proyecto complementario a AGENTS.md: se incorpora dba-engineer para ingeniería de datos y SQL Server, con responsabilidades y límites en [role.md](role.md). El flujo exige TaskInput → DBA sobre tareas de datos asignadas → integración/congelado del principal → QA y code-review independientes → decisión humana. Máximo dos especialistas simultáneos y sin redelegación. Engineer conserva implementación general; DBA no se autoevalúa como revisor independiente.

El perfil instalado previamente en `.codex/agents/dba-engineer.toml` se conserva sin sobrescribir. SHA-256 inicial: `c7b601ae6857a451238336bd2bfea727bcc7c48667445f8dd5f1a909c30f88b9`. La adaptación de catálogo [dba-engineer.toml](dba-engineer.toml) amplía los controles contractuales; no se afirma que sus instrucciones adicionales hayan sustituido las de la instalación existente.

AGENTS.md y docs/source pertenecen al candidato congelado D2-A; las herramientas de edición del principal no los modificaron. Manifiesto inicial verificado: `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f`. La comprobación final detectó deriva únicamente en AGENTS.md, que ahora incluye DBA; no se atribuye autoría sin evidencia. Se conservan archivo y manifiesto original sin rebaselinar. Estado global BLOCKED; ver [evidencias y límites](EVIDENCE.md).

Fuentes revisadas: AI-ENGINEERING/AGENTS.md; contracts/README.md y task-input.schema.json; standards/execution-policy.md, security.md y documentation.md; roles/README.md, engineer.md y database-architect.md; workflows/feature-development.md; codex/candidates/agents/engineer.toml y perfiles instalados en KDI. Las autorizaciones históricas de etapa quedan contextualizadas por la solicitud humana actual; el sandbox sigue siendo independiente.

Formato TOML: name, description y developer_instructions, compatible con los perfiles locales y la [documentación oficial de subagentes](https://learn.chatgpt.com/docs/agent-configuration/subagents). Parsear TOML no demuestra descubrimiento; se exige despacho real por agent_type.
