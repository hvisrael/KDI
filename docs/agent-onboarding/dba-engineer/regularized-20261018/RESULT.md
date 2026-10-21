# Onboarding DBA regularizado — PASS técnico

Recomendación: aceptar humanamente la incorporación actual de dba-engineer con la limitación histórica documentada. No constituye aceptación humana automática ni autorización de operaciones SQL reales.

Perfil final instalado: .codex/agents/dba-engineer.toml, copia congelada candidate/profile.toml. SHA-256 0444822eceba583f5b3ac49def40692e4cc479aa5fc9cfa337390a1a99bd90d2. Controles corregidos en candidate/CHANGES.md: TaskInput, presupuesto/límites, no redelegación no autorizada, preservación del congelado, privilegio mínimo, autorización humana expresa para CREATE LOGIN/ALTER ROLE/GRANT/REVOKE/DDL/migraciones, secretos protegidos y escalamiento.

Declaración humana de procedencia preservada en candidate/HUMAN-DECLARATION.md. La falta de registros históricos completos y de fecha exacta permanece en candidate/HISTORICAL-LIMITATION.md. Conforme al encargo humano actual no bloquea por sí sola el onboarding técnico. No se inventan eventos, marcas temporales ni autores adicionales. Paquete anterior BLOCKED preservado como histórico.

Manifiesto actual: candidate/manifest.json; SHA-256 cdaaf6343589f58b7d654858020e282492253f9139ff372e96acdd43e5d29579. Once archivos más el propio manifiesto, que no se incluye a sí mismo. Ningún archivo del candidato cambió después del congelado. Informes y decisión fuera del candidato. Manifiesto separado de entrega incluye estos informes y sus hashes.

Delegaciones reales:

| Identificador | agent_type efectivo | Tarea | Resultado/evidencia |
|---|---|---|---|
| /root/dba_regularized_availability | dba-engineer | DBA-REG-AVAIL, recepción/disponibilidad sin herramientas | PASS; reports/AVAILABILITY.md |
| /root/dba_regularized_qa | qa-engineer | DBA-REG-QA, validación independiente | PASS técnico; reports/QA.md |
| /root/dba_regularized_review | code-reviewer | DBA-REG-REVIEW, revisión independiente | PASS; reports/CODE-REVIEW.md |

DBA terminó antes de despachar los dos revisores. QA y revisor evaluaron el mismo hash sin modificarlo, verificando inventario/hashes antes y después. No hay hallazgos técnicos críticos abiertos ni pruebas técnicas requeridas bloqueadas. La disponibilidad documental no se confunde con una prueba de recarga del TOML en el runtime existente; archivo instalado y sintaxis fueron comprobados independientemente.

D2-A: contenido e inventario exactos verificados con python tools/d2a-manifest.py --verify, salida 0. Manifiesto original 66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f intacto. AGENTS raíz conserva los bytes D2-A. Configuración proyecto/global sin cambios; catálogo externo solo leído.

Verificación reproducible: python docs/agent-onboarding/dba-engineer/regularized-20261018/verify.py. Sin SQL, bases, logins, permisos, conexiones, IDI, D2-B/C o despliegues. Trabajo detenido antes de persistencia SQL real. PASS corresponde exclusivamente al onboarding actual, no al estado de aceptación de D2-A.
