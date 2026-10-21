# Registro de preparación documental D2

Autoridad: mensaje humano «KDI — PREPARACIÓN DE D2 PARA APROBACIÓN HUMANA» en esta conversación. Autoriza propuesta y delegación documental; no autoriza implementación de D2.

Base D1 aceptada: docs/stage3/d1/implementation-manifest.json, SHA-256 `e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193`. Antes de preparar, `python tools/content-manifest.py --verify` confirmó los 87 archivos. D1 y su aceptación permanecen sin cambios.

Fuentes: docs/source/kdi/REQUIREMENTS.md, ARCHITECTURE.md, MODULES.md, TEST-PLAN.md y ADR-003/004; docs/stage3/DECISIONS.md; docs/stage3/d1/acceptance/D2-FOR-HUMAN-REVIEW.md. Se conservan como fuentes históricas/propuestas; ninguna regla pendiente se convierte en decisión humana por aparecer en esta propuesta.

Delegaciones reales, máximo dos especialistas simultáneos:

| agent_type efectivo | Identificador | Tarea y propiedad documental |
|---|---|---|
| analyst-architect | /root/d2_functional_proposal | Reglas, organizaciones, membresías, permisos, criterios y decisiones; FUNCTIONAL-ANALYSIS.md y ARCHITECTURE-DECISIONS.md |
| engineer | /root/d2_technical_proposal | Diseño SQL, migraciones, transacciones, OIDC y contratos; TECHNICAL-DESIGN.md |

Presupuesto de esta preparación: una ronda documental acotada por especialista (objetivo hasta 10 minutos cada uno), con integración por principal. No es autorización de los presupuestos de implementación propuestos. Sin redelegación, acceso externo ni instalaciones; sin asumir infraestructura existente.

Solo se escriben documentos nuevos en docs/stage3/d2-proposal/. Principal conserva la responsabilidad de integración. No son revisiones QA/Code Review independientes ni pruebas del producto. Estado del entregable: PROPOSED / AWAITING_HUMAN_DECISION; estado de implementación D2: NOT_STARTED.
