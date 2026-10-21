# QA independiente — incorporación DBA

Autor: `/root/dba_onboarding_qa`; agent_type efectivo `qa-engineer`. Consolidación de su respuesta por el principal; el especialista no editó archivos ni conectó SQL, instaló herramientas, accedió a IDI o redelegó.

Veredicto: **BLOCKED, sin PASS global**.

| Criterio | Resultado |
|---|---|
| Compatibilidad TOML | PASS: cinco instalados y candidato, mismas tres claves, tomllib |
| Perfil KDI preservado | PASS: c7b601ae6857a451238336bd2bfea727bcc7c48667445f8dd5f1a909c30f88b9 |
| Responsabilidades/límites/entregables | PASS documental |
| Registro catálogo y flujo | PASS: cuatro hashes coinciden con catalog-registration.json |
| Sesión CLI nueva | PASS: thread.started 01a151fb-802b-7092-9145-c38cc108e0de |
| Despacho DBA y prueba sintética en sesión nueva | BLOCKED: sin evento spawn ni agent_type exportado; solo wait con receptores vacíos y declaraciones textuales |
| Reporte DBA | PASS documental: distingue evidencia histórica, testigos y autenticación pendiente |
| Integridad D2-A actual | FAIL: únicamente AGENTS.md difiere; sin altas ni bajas |
| SQL efectivo y autenticación actuales | NOT VERIFIED deliberadamente |

Hallazgos altos: la respuesta cruda CLI afirma eventos spawn no presentes en JSONL; no aceptarla como prueba. La integridad D2-A actual difiere del manifiesto histórico. AGENTS.md esperado b2acee1150c594bae3383114ea65f8b270ca722a1d1a8b1fffa6de3db8bcc2a6; observado c7876cc096362d0e5ba3abffd05a04efe572813eda47917fa9df9c65a507c2e1. No atribuir autor ni revertir/rebaselinar.

Hallazgo bajo: contextualizar README sobre deriva; el principal corrigió esa redacción después del dictamen. Esta corrección documental no resuelve bloqueos ni cambia el candidato.

Omisiones: sin reproducción CLI adicional, pruebas SQL actuales, batería histórica ni auditoría retrospectiva completa de concurrencia. Importación jsonschema no disponible; no se instaló. Registro JSON de PowerShell UTF-16 comprobado mediante PowerShell tras fallo del lector UTF-8.

Los artefactos crudos new-session-result.md y new-session-events.jsonl se conservan como evidencia de lo observado, incluso su afirmación no sustentada; EVIDENCE.md y este dictamen la rechazan expresamente.
