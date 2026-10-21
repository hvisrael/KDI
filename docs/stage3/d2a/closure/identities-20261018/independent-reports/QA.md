# QA independiente — PASS técnico acotado

Fuente: ReviewResult real de /root/d2a_phase2_final_qa, agent_type qa-engineer; taskId D2A-P2-FINAL-QA / identities-20261018 / attempt1. Consolidado por el principal fuera del candidato/evidencia congelados.

Candidato c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973; evidencia80ff0d9ac23386569e7c620d237a6af401ba6b48dd5f9447859ba3090e646d68.

Verificaciones independientes:

- Source123 archivos y evidencia44, hashes/inventarios exactos antes/después. Original121 archivos y manifiesto66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f íntegros.
- Copia de trabajo validada antes de compilar; restore/build exitosos,0 errores/0 advertencias.
- Unit36/36, API7/7, arquitectura24/24;0 errores y0 omitidos.
- Journal recalculado:3 CREATE LOGIN,5 CREATE USER,5 ALTER ROLE confirmados;5 autenticaciones directas INSTANCE con identidades/bases correctas; runnerDev4060.
- Matriz recalculada coincide. Únicos incompletos históricos:3 serverpermNULL por firma incorrecta. Suplemento muestra0 para cada permiso/login mediante impersonación diferenciada de autenticación.
- Política/expiración activas, roles/SIDs exactos, ningún rol administrativo, runner ausenteDev.
- HTTP actual127.0.0.1:5088:6/6 checks incluyendo readiness200; liveness/metadatos/OpenAPI/404/405; repetición final exitosa. Fuente de instancia aislada idéntica a congelado.

Evidencia de ejecución en qa-evidence/ copiada por el principal desde .local/d2a-phase2-qa. Primera invocación de pruebas tuvo sintaxis posicional incompatible con runner; corregida con --project, logs preservados.

Límites: no SQL, DDL/DML ni suiteSQLbaseline mutante. Negativos acreditados por permisos efectivos y accesoDev rechazado, sin atribuir intentosDDL/DML. Consulta opcional PID fue denegada por sandbox; no constituye criterio crítico pendiente, HTTP sí comprobado. Históricos FAIL/BLOCKED preservados. Sin modificaciones a congelados, original o configuracionesglobales. Ningún criterio crítico del alcance autorizado quedó bloqueado. Aceptación humana corresponde al Human Orchestrator.
