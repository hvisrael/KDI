# Registro A1 — intento 1

Continuación intento 2: RECOVERY-RESULT.md, recovery-context.json y RECOVERY-LOCAL-WINDOWS.md documentan mecanismo D2-A localizado y alternativa de sesión local existente. No conexión SQL ni valores secretos leídos; no reemplazan el registro inicial.

Inicio observado: 2026-10-20T18:33:27.7757637-06:00. Contexto documentado: 2026-10-21T00:34:11.5786286Z (20 de octubre local). Autorización: mensaje humano «KDI — D2-B | Ejecución controlada A1». Autoriza exclusivamente el script revisado, con identidad existente autorizada; fase 4 exige detenerse si esa condición no se determina.

1. Se leyeron AGENTS.md, PLAN.md, AUTHORIZATIONS.md, TEST-MATRIX.md, GO-NO-GO.md y el script completo; se consultó evidencia de aceptación D1/D2-A mediante verificadores y documentación previa de continuidad.
2. `python docs/stage3/d2b/preparation-20261020/freeze.py --verify`: exit 0, 190 archivos, SHA-256 a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102. No se regeneró manifiesto.
3. `python docs/stage3/d2a/closure/identities-20261018/verify.py`: exit 0. Original D2-A, incremento aceptado y evidencia congelada verificados. Manifiesto D1 coincide con e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193. D1 no se presenta como árbol fuente raíz actual.
4. Get-FileHash del script: 60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c, coincide con aprobación. No se modificó script.
5. Inspección completa: directiva SQLCMD :ON ERROR EXIT, SET NOCOUNT de sesión, guardas/THROW y SELECT de funciones/catálogos/historias. Sin SQL dinámico, procedimientos almacenados, DDL/DML/DCL, impersonación, backup, configuración persistente ni consultas a datos de aplicación. Las funciones invocadas son built-in de metadatos/contexto; no hay funciones de usuario ni dependencias ejecutables externas SQL. La lectura de historias no escribe en ellas. SET NOCOUNT cambia solo emisión de conteos de sesión; no cambia seguridad ni configuración persistente.
6. `whoami` y WindowsIdentity.GetCurrent identificaron DESKTOP-F25O3MF\CodexSandboxOffline, NTLM, autenticado en Windows. No se consultaron contraseñas ni valores de configuración sensibles. Get-Command encontró SQLCMD.EXE. Se enumeraron solo nombres de cinco variables conocidas de conexión: ninguna presente.
7. Búsqueda acotada de referencias de operador en documentación controlada y autorización D2-B: no se encontró acreditación de CodexSandboxOffline como operador SQL autorizado. Windows autenticado y binario sqlcmd no demuestran mapping/permisos SQL ni autorización humana específica de esa cuenta.
8. **Detención fase 4 antes de conectar.** No se intentó sqlcmd, otro driver ni conexión exploratoria; no se cambió a cuenta privilegiada ni se solicitó elevación. No hubo SQL ejecutado, resultado SQL, warning SQL ni exit code SQL. No atribuir al servidor un rechazo de acceso que no se comprobó.

Evidencia: context.json, candidate-integrity.log, accepted-integrity.log y documentos de resultados. Los logs de verificador son evidencia de archivos; no acreditan disponibilidad SQL. Git no se inicializó. Se escribieron únicamente archivos nuevos de evidencia fuera del candidato; ninguna acción A2–A8 ejecutada.

Delegaciones reales: /root/d2b_review (code-reviewer), seguridad/alcance/evidencia; /root/d2b_qa (qa-engineer), completitud/trazabilidad/reproducibilidad. Informes CODE-REVIEW.md y QA-REVIEW.md. Principal actúa como Tech Lead e integra evaluación DBA/seguridad; no se atribuye un tercer especialista inexistente.
