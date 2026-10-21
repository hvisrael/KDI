# D2-A: incremento de identidades SQL — PASS técnico; aceptación humana pendiente

> **ESTADO VIGENTE:** D2-A fue **ACEPTADO** por el Human Orchestrator. Ver [ACCEPTANCE.md](ACCEPTANCE.md) y el recibo `acceptance-manifest.json`. El título y la frase «aceptación humana pendiente» de este documento **describen el estado en el momento de emitirse** y quedan resueltos exclusivamente por el recibo posterior, sin modificar los artefactos congelados ni la entrega revisada. Estado vigente del proyecto: [docs/stage3/STATE.md](../../STATE.md).

Fase2 autorizada completada. Tres logins SQL creados y habilitados con CHECK_POLICY/CHECK_EXPIRATION ON; cinco usuarios y cinco membresías confirmados, sin GRANT nuevos ejecutados ni roles administrativos. Recomendación: aceptar humanamente este nuevo candidato D2-A con los límites de verificación descritos. Este informe no constituye aceptación humana ni despliegue.

## Estado final observado

| Identidad | KulaniKDI_Dev | KulaniKDI_Test |
|---|---|---|
| kdi_migrator | Autenticó; kdi_migrations | Autenticó; kdi_migrations |
| kdi_app | Autenticó; kdi_application | Autenticó; kdi_application; API conectada |
| kdi_test_runner | Sin usuario, conexión rechazada4060 | Autenticó; kdi_tests, lecturas históricas permitidas |

Exactamente13 operaciones confirmadas, con inexistencia/SID/roles comprobados y sin conflictos. Journal evidence/phase2/sql-v2/execution-20261019T031842176Z.json. Contraseñas capturadas y confirmadas en diálogos protegidos, nunca transmitidas a conversación ni guardadas en archivos, configuración, argumentos, logs o manifiestos. Para API el humano introdujo kdi_app directamente en consola; SqlCredential/SecureString fuera de connection string. Procesos de captura y API ya finalizados; runtime-cleanup.json acredita cierre del listener temporal sin detener SQL Server.

Permisos de base/esquemas observados bajo cinco conexiones directas, con identidades INSTANCE y roles exactos, administración0 y DDL app0. Las tres consultas de servidor iniciales devolvieronNULL por firma incorrecta HAS_PERMS_BY_NAME(NULL,'SERVER',...). El journal conserva FAIL_PERMISSION_MATRIX. Suplemento read-only usa firma correcta NULL,NULL y registra0 CONTROL SERVER/ALTER ANY LOGIN/ALTER ANY SERVER ROLE para los tres logins, bajo impersonación expresamente diferenciada de la autenticación directa previa. NingúnNULL se convirtió silenciosamente en0; no se modificaron permisos para hacer pasar verificación. Consultas y resultados en evidence/phase2/postverify/.

API aislada kdi_app/Test en127.0.0.1:5088: readiness SQL200 y smoke6/6. QA lo repitió en vivo antes del cierre. Build Release0 errores/0 advertencias; unit36/36, API7/7, arquitectura24/24, sin omitidos, también reproducidos por QA. No ejecución separada API contraDev; sí autenticación app directa y matriz en ambas bases. Baseline sin negocio: no se concedió DML anticipado a runtime ni se afirma persistencia de negocio.

Negativos DDL/DML acreditados por permisos efectivos; no se intentaron operaciones adicionales fuera de las13 autorizadas. No se reejecutó la suiteSQLbaseline mutante de migración/downgrade, por estar fuera de esta autorización exacta. Se preserva evidencia histórica de aquella suite en original-closure/original-reports. No existen criterios críticos bloqueados dentro del incremento autorizado según ambos dictámenes; el PASS es técnico acotado al alcance revisado.

## Congelados y verificación

- Nuevo candidato fuente: candidate/. Manifiesto candidate/docs/stage3/d2a/candidate-manifest.json SHA-256 c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973;123 archivos. Delta:3 fuentes,1 test nuevo,3 documentos; sin cambio de dependencias.
- Evidencia congelada: evidence/manifest.json SHA-25680ff0d9ac23386569e7c620d237a6af401ba6b48dd5f9447859ba3090e646d68;44 archivos. freeze.json/freeze.sha256 correlacionan ambos. Ningún congelado cambió durante revisiones.
- Original en workspace principal: manifiesto66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f y121 archivos intactos. El código nuevo se entrega en candidato separado, no sobrescribió la línea base original.
- Verificación reproducible de ambos y del original: python docs/stage3/d2a/closure/identities-20261018/verify.py.
- Informes independientes y limpieza van fuera de ambos congelados; delivery-manifest.json enumera toda la entrega y sus hashes.

Históricos preservados: preflight v1 falló antes de SQL por incompatibilidad de builderPowerShell; v2 pasó51 aserciones offline independientes y preflight real. V2 ejecutado conserva el defecto diagnóstico de servidor ya suplementado y no debe reutilizarse para nuevas altas. Checkpoint congelado refleja la espera anterior y es histórico, no estado final. Apareció temporalmente sql.txt vacío en raíz; se preservó en evidence/phase2/unscoped-artifacts/sql.txt fuera del original y se restituyó su inventario exacto. No se atribuye autoría no acreditada.

## Delegaciones reales y dictámenes

| Identificador | agent_type efectivo | Tarea y resultado |
|---|---|---|
| /root/d2a_phase2_sql_executor | dba-engineer | Helper/51 pruebas offline y suplemento SQL de lectura;13 commits acreditados por ejecución del principal |
| /root/d2a_phase2_api_implementation | engineer | Snapshot SQLCredential y67 pruebas offline satisfactorias |
| /root/d2a_phase2_provisioner_review | code-reviewer | Revisión previa helper v1/v2;PASS estático, sin autoría ni escrituras |
| /root/d2a_phase2_final_qa | qa-engineer | PASS técnico acotado, pruebas/HTTP independientes; independent-reports/QA.md |
| /root/d2a_phase2_final_review | code-reviewer | PASS técnico acotado sin bloqueantes; independent-reports/CODE-REVIEW.md |

Máximo dos especialistas activos simultáneamente. Autoría, QA y revisión separados. Ambos revisores finales evaluaron los mismos hashes sin modificarlos. Principal integró, ejecutó operaciones autorizadas y solicitó aceptación; no atribuyó su trabajo a especialistas.

Sin IDI, otras bases, configuracionesglobales, firewall, nuevos GRANT, D2-B/C o despliegue. Mantener rotación supervisada de contraseñas por expiraciónactiva y autorizaciones específicas para cualquier operación futura. Solicitud al Human Orchestrator: aceptar o rechazar el nuevo candidato D2-A identificado por este manifiesto; no se continúa B/C.
