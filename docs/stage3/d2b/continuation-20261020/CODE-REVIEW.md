# ReviewResult — code-review D2-B

- TaskInput: `D2B-CONT-20261020-REVIEW`, intento 1.
- Agente: `/root/d2b_review`; agent_type efectivo: `code-reviewer`.
- Autorización: solicitud humana actual «KDI — Continuación de Stage 3 / D2-B», preparación y validación local; asignación del principal. Presupuesto: 20 minutos.
- candidateHash: `1ab77cd831a7426ba0cdd864cbb0c3edd5479f2d81a416cfec159a12a60888f6`.
- Candidato: `.local/d2b-candidate`, 190 archivos inventariados por `docs/stage3/d2b/preparation-20261020/candidate-manifest.json`.
- Resultado: **CHANGES_REQUESTED para la propuesta de permisos; validación operacional BLOCKED. D2-B NO cerrado.**

Revisión independiente: este agente no fue autor del producto revisado. No modificó candidato, D1, D2-A ni configuraciones. No abrió conexión SQL, ejecutó GRANT, migraciones, fixtures o despliegues. Única escritura: este informe fuera del candidato. No hubo redelegación.

## Hallazgo reproducible

### R1 — P2 — Retirar DELETE de Sessions del rol runtime

Ubicación: `tools/Kulani.KDI.Migrations/prepared-sql/KulaniKDI_Test-runtime-grants.sql:15` y archivo equivalente `KulaniKDI_Dev-runtime-grants.sql:15`, dentro del candidato.

Ambos conceden `SELECT, INSERT, UPDATE, DELETE` sobre `identity.Sessions` a `kdi_application`. El runtime `src/Kulani.KDI.Infrastructure/Access/SqlAccessStore.cs` crea, consulta, renueva y revoca sesiones; la revocación actualiza `RevokedAtUtc` y elimina el ticket protegido mediante UPDATE. No necesita DELETE. El borrado físico pertenece exclusivamente a `AccessAdministration` en el comando privilegiado `purge`, utilizado por la herramienta de operador con `kdi_migrator`.

Conceder DELETE al runtime permite eliminar sesiones físicamente y eludir la retención de siete días y el camino de revocación auditado si esa identidad es utilizada indebidamente. El permiso está solamente propuesto; no hay evidencia de que se haya aplicado. El propio SQL-REVIEW del preparador ya señala que debe revisarse su necesidad, y esta inspección resuelve que no hay una operación runtime que lo requiera.

Reproducción estática, sin conexión SQL, desde la raíz KDI:

```powershell
rg -n 'GRANT.*Sessions' .local/d2b-candidate/tools/Kulani.KDI.Migrations/prepared-sql
rg -n 'ExecuteDeleteAsync|RevokeSessionAsync|ExecuteUpdateAsync' .local/d2b-candidate/src/Kulani.KDI.Infrastructure/Access
```

Corrección recomendada: retirar DELETE de ambos scripts runtime, actualizar los hashes del paquete y el manifiesto del candidato y volver a revisar el nuevo hash. No introducir REVOKE sobre bases existentes: cualquier ajuste de permisos reales exige inventario y autorización separados. No autorizar los GRANT del candidato actual sin resolver R1.

## Comprobaciones independientes ejecutadas

1. Leídos `AGENTS.md`, `docs/stage3/DECISIONS.md`, aceptación D1, aceptación D2-A en `docs/stage3/d2a/closure/identities-20261018/ACCEPTANCE.md`, `docs/stage3/d2b/AUTHORIZATION.md` y documentos de preparación D2-B.
2. `python docs/stage3/d2b/preparation-20261020/freeze.py --verify`: exit 0 antes y después de la inspección; 190 archivos y SHA-256 coincidente con candidateHash.
3. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .local/d2b-candidate/tools/Kulani.KDI.Migrations/Verify-PreparedSql.ps1`: exit 0. Salida: `OFFLINE VERIFIED: 14 artifact hashes, 12 reproduction hashes, 4 rollback target/history/baseline/table scopes. No SQL executed.` La opción afecta ese proceso; no se cambió política global.
4. Inspeccionados generador SQL, upgrades, rollback de ambos módulos y ambos destinos, verificadores, entidades y restricciones, acceso SQL, sesiones, política de permisos, administración privilegiada y launcher.
5. Inspeccionados OIDC/cookies/CSRF/origen, endpoints y frontend de acceso; suites de sesión/API y prueba SQL D2-B. Las pruebas OIDC de configuración y eventos no realizan validación criptográfica real de tokens ni intercambio con proveedor.

Los hashes de reproducción se compararon con los artefactos existentes; este agente no volvió a generar SQL ni compiló ni ejecutó suites. Los resultados de build/tests de preparación son evidencia previa del implementador; la repetición independiente corresponde al informe QA, no se atribuye a este review.

## Evaluación acotada

Los upgrades son aditivos y protegidos por servidor/base/baseline/historial; los rollback preparados eliminan únicamente tablas D2-B y el registro Access correspondiente, conservando baseline y esquemas. Los rollback no son idempotentes y son destructivos, como está documentado. Cada módulo tiene su transacción; no se presume atomicidad del paquete entre módulos. La estrategia de conservar datos/deshabilitar acceso y autorizar respaldo/downgrade por separado es explícita. La reversión de permisos requiere capturar el delta previo y no se sustituye con REVOKE ciego.

Se observan sesiones con handle aleatorio hasheado, ticket protegido, límite absoluto y de inactividad, validación de usuario activo y CAS contra renovación/revocación. El runtime verifica membresía y organización activa al consultar acceso. Las operaciones privilegiadas comparten transacción, applock y comprobación de último administrador. Ninguna de estas observaciones acredita ejecución SQL real, aislamiento transaccional o carreras efectivas.

La preparación OIDC usa Code/PKCE, issuer/audience/firma/lifetime configurados, sin SaveTokens ni refresh tokens, cookie Secure/HttpOnly y CSRF más Origin en mutaciones. Secrets requieren consola privada y el keyring externo requiere ACL/DPAPI. El flujo Auth0 real y su logout sin id_token_hint siguen pendientes. No se identifica otro defecto bloqueante confirmado mediante esta inspección; no es una garantía de ausencia de defectos.

Las dependencias tienen versiones exactas y locks inventariados. No se ejecutó auditoría vigente contra registros/advisories, restore limpio ni comprobación de actualización SDK/runtime. La evidencia histórica no se convierte en auditoría actual ni permite afirmar ausencia de vulnerabilidades conocidas a la fecha de esta revisión.

## Bloqueos y siguiente acción

- SQL conectado: autorización específica de upgrade, repetición, permisos, fixtures/limpieza, rollback y comprobación de permisos efectivos en Test. Dev requiere autorización posterior.
- Auth0/OIDC/HTTPS: tenant/aplicación/cuentas, certificado confiable y keyring aprobado, secretos mediante entrada privada y operador autorizado.
- Seguridad real: login/logout, state/nonce/replay, cookies HTTPS, aislamiento multiempresa, expiración y carreras de revocación pendientes según matriz; las pruebas sintéticas no cierran esos puntos.
- Dependencias: auditoría actual no acreditada en este review.

Siguiente acción concreta: el implementador corrige R1 exclusivamente en D2-B, regenera manifiestos y propone un nuevo hash; el revisor comprueba el delta y QA informa sobre ese mismo candidato. Solo después preparar la solicitud concreta de autorización del paquete Test y la infraestructura pendiente. Este informe no acepta ni cierra D2-B.
