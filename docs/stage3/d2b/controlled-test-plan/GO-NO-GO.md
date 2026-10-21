# Dictamen de preparación

Fecha documental: 2026-10-20. D2-B **OPEN/BLOCKED**. **GO únicamente para preparación offline; NO-GO para comenzar ejecución conectada.** No se ha ejecutado SQL conectado ni realizado cambios de infraestructura o datos.

Candidato: 190 archivos, SHA-256 a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102. Integridad verificada; D1/D2-A intactos. Git no inicializado, exit 128; no se afirma árbol Git limpio. Verificador SQL offline exit 0. Pruebas locales anteriores satisfactorias permanecen como evidencia local, no aceptación real.

> **Actualización 2026-10-20.** El candidato vigente es `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos) tras la corrección del harness de fixtures, revisada de forma independiente. **El dictamen NO-GO no cambia**: D2-B sigue **OPEN/BLOCKED** y GO sigue siendo **únicamente para preparación offline**. Se añade un bloqueante nuevo: el harness de fixtures necesita `INSERT` y `DELETE` que el rol `kdi_tests` de Test no tiene ([QA](../harness-correction-20261020/QA.md) §10.3), de modo que A5 no puede solicitarse hasta que se resuelva la identidad y los permisos mínimos de Test.

| Puerta | Estado actual | Evidencia necesaria para avanzar |
|---|---|---|
| Integridad y paquete offline | Comprobado | candidate-integrity.log, accepted-integrity.log, sql-offline.log y TEST-ARTIFACTS.json |
| Preflight conectado | BLOCKED | A1 explícita con operador existente aprobado |
| Backup/recuperación | BLOCKED | Ruta/espacio/operador/retención y A2; evidencia recuperación acordada; A7 para cualquier restauración |
| Migración y repetición | BLOCKED | A3 exacta sobre dos SQL Test y backup/preflight satisfactorios |
| Permisos runtime | BLOCKED | A4 por siete objetos, snapshot y medición efectivos; sin DELETE Sessions |
| Fixtures/harness | BLOCKED | A5 + resolver limpieza residual/cancelación y recuperación por IDs; clases secuenciales; baseline excluido |
| Auth0/HTTPS/keyring | BLOCKED | A6, valores públicos concretos, operador, cuentas sintéticas y entrada privada de credenciales |
| Seguridad integrada | BLOCKED | TEST-MATRIX real con evidencia, A8 para probes/fallos autorizados |

Entregables: [plan conectado y reversión](PLAN.md), [autorizaciones](AUTHORIZATIONS.md), [checklist y matriz de pruebas/aceptación](TEST-MATRIX.md), [revisión seguridad/SQL](SECURITY-SQL-REVIEW.md), [QA independiente](QA-READINESS.md). Revisión de preparación no es aceptación D2-B.

Delegaciones reales: `/root/d2b_review`, agent_type efectivo `code-reviewer`, revisión SQL/seguridad; `/root/d2b_qa`, agent_type efectivo `qa-engineer`, inventario de suites, recuperación de fixtures y aceptación. Cada uno conserva responsabilidad y evidencia en su informe, sin cambiar candidato. Principal integra procedimiento como Tech Lead; la especialidad SQL/seguridad del informe no sustituye un operador DBA autorizado. Máximo dos especialistas activos.

Autorizaciones humanas exactas: A1 conexión read-only; A2 backup y ruta; A3 upgrades/repetición identity y organizations; A4 siete GRANT mínimos; A5 fixtures/DML/limpieza/administración/pruebas de sesiones por corrida; A6 infraestructura/Auth0/arranque; A8 probes negativos/inyección de fallos. A7 restauración/downgrade es separada, destructiva y NO forma parte de la ejecución inicial. No pedir ni conceder aprobación genérica de todas las puertas.

Primera operación segura sin autorización adicional: `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` desde raíz KDI; ya comprobada y repetible. Primera operación conectada propuesta, solo después de A1: SELECT de servidor/base/versión e historias bajo operador existente designado. A1 no autoriza backup, GRANT, migración ni fixtures.

Solicitud humana siguiente: **autorizar A1 exclusivamente e identificar el operador existente para lectura de KulaniKDI_Test en DESKTOP-F25O3MF**. No enviar credenciales. Tras preflight se concretan rutas de backup, estado real y siguientes aprobaciones. Los defectos de limpieza del harness deben resolverse y revisarse antes de A5; una autorización no los convierte en pruebas seguras.

Primer lote conectado revisable: `01-preflight-readonly-REQUIRES-A1.sql`, no ejecutado. Solo destino/versión/baseline/tablas y permisos de la conexión actual; no contiene datos de aplicación ni impersonación. No acredita por sí solo snapshot completo, privilegios efectivos runtime, capacidad de backup o ausencia de drift; esas lecturas se concretan después sin ampliar A1 implícitamente.

SHA-256 del primer lote: `60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c`.
