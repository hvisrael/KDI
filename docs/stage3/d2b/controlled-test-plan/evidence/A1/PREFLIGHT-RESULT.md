# Resultado A1

Actualización posterior: [recuperación documental del mecanismo D2-A](RECOVERY-RESULT.md). Se identificó kdi_test_runner/Test como cuenta de lectura histórica; sus secretos fueron solo de memoria. Continúa BLOCKED por autenticación reutilizable no disponible en el sandbox; no se solicitó nueva autorización A1 ni contraseña. El intento inicial se conserva debajo.

**A1: BLOCKED. NO-GO FOR A2 AUTHORIZATION REQUEST. D2-B OPEN/BLOCKED.**

Referencia temporal: 2026-10-21T00:34:11.5786286Z (2026-10-20 local). Identidad Windows detectada: DESKTOP-F25O3MF\CodexSandboxOffline; no acreditada como operador SQL autorizado. Identidad de sesión SQL: NO VERIFICADA, porque no se estableció conexión.

Destino autorizado/propuesto: DESKTOP-F25O3MF / KulaniKDI_Test. Destino efectivo: **NO VERIFICADO**. Servidor disponible, conectividad y estado ONLINE: **NO COMPROBADOS**. Script previsto: docs/stage3/d2b/controlled-test-plan/01-preflight-readonly-REQUIRES-A1.sql, hash 60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c; **NO EJECUTADO**.

| Criterio | Resultado comprobado |
|---|---|
| Integridad candidato | Exit 0, 190 archivos, hash esperado a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102 |
| D1/D2-A | Hash D1 coincide; verificador original/aceptado/evidencia D2-A exit 0 |
| Script y seguridad estática | Completo revisado; hash aprobado; sin escrituras de datos/configuración persistente |
| Cliente SQLCMD | Binario existente identificado; no invocado |
| Operador autorizado | BLOCKED: solo contexto de proceso identificado, no aprobación/mapping SQL |
| Sesión/destino/permisos SQL | NOT RUN |
| Esquemas/historias/tablas | NOT RUN; evidencia histórica no se usa como estado actual |

Error operativo SQL: ninguno observado, porque no se intentó conexión. Bloqueo previo: no se pudo determinar autorización de identidad exigida en fase 4. No se certifica ausencia de drift, escrituras concurrentes ni compatibilidad SQL actual. Evidencia y límites detallados en EXECUTION-LOG.md, context.json y reportes independientes.

Acción concreta: identificar la cuenta existente autorizada para ejecutar A1 y el mecanismo aprobado para usarla sin contraseñas en conversación/archivos/argumentos. Si es CodexSandboxOffline, confirmar expresamente que esa identidad existente está aprobada; no crear permisos para ella ni probarlos exploratoriamente antes de resolver identificación. A1 ya está autorizada; no se pide renovarla ni ampliar alcance. Con identidad resuelta se verifica nuevamente integridad y se ejecuta exclusivamente el script.
