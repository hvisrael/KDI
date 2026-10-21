# ReviewResult — recuperación del mecanismo local A1

TaskInput `D2B-A1-RECOVER`, intento 1; presupuesto 15 minutos. Agente `/root/d2b_review`, agent_type efectivo `code-reviewer`. Fuente: petición humana comunicada por el principal de recuperar el mecanismo D2-A sin pedir credenciales, sin uso automático de sysadmin y preparando sesión local si el sandbox bloquea. Revisión independiente de lectura; única escritura: este informe. No SQL, credenciales, secretos, IDI ni cambios de candidato/infraestructura.

candidateHash verificado independientemente: `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102`, 190 archivos, freeze exit 0. Script A1 SHA-256 verificado: `60DBAD787EC5B33A421F6944549A78962314603C6FDA8F0F8A96B88683BBF79C`.

## Qué acredita D2-A

Leídos completos cierre D2-A RESULT.md, evidence/phase2/sql-v2/README.md, evidence/phase2/api/Start-KdiSqlApi.ps1 y runtime-cleanup.json. Se inspeccionaron también puntos de entrada/uso/destrucción de credenciales del provisioner congelado, sin ejecutarlo ni leer almacenes privados.

| Mecanismo histórico | Evidencia y límite actual |
|---|---|
| Provisioner fase 2 | Get-Credential protegido, confirmación humana y SecureString/SqlCredential en memoria del proceso; BSTR liberados y secrets Dispose/Clear en finally. Contiene operaciones CREATE LOGIN/CREATE USER/ALTER ROLE. No es un ejecutor A1 reutilizable ni debe invocarse, tampoco su preflight que exige conexión administrativa sysadmin y otras consultas. |
| API kdi_app | Launcher transmite solo metadatos de destino; API solicita contraseña mediante consola local y SqlCredential. No hereda ni exporta las credenciales del provisioner. Arranca un proceso/listener y no ejecuta el script A1. Reutilizarlo implicaría prompt de password y operaciones ajenas al lote; queda excluido. |
| Cierre de procesos | RESULT informa procesos de captura/API finalizados; runtime-cleanup.json registra apiProcessStopped=true y loopbackTestListenerClosed=true el 2026-10-19T03:36:22.3482871Z, sin detener SQL Server. Es evidencia histórica, no inventario de sesiones actuales. |
| kdi_test_runner | Autenticación directa y lectura de historiales Test acreditadas históricamente; sin acceso Dev. No demuestra que hoy exista una sesión autenticada vigente ni que el password no haya expirado. |

**No hay evidencia de un secreto persistido y reutilizable.** El diseño y la evidencia D2-A declaran explícitamente que no se guardaron contraseñas en archivos/configuración/env/argumentos/logs. Una SqlCredential histórica era un objeto en memoria, no un vault ni un archivo recuperable. La revisión no pretende demostrar que ningún almacén externo exista: no los inspeccionó y no autoriza buscar/exportar secretos. Tampoco permite recuperar memoria de procesos, dumps, cachés, contraseñas guardadas de SSMS o Windows Credential Manager.

## Ruta local propuesta y condiciones

Una ventana de consulta SSMS **ya autenticada y conectada** como `kdi_test_runner` a DESKTOP-F25O3MF / KulaniKDI_Test es una alternativa compatible con ejecutar solo A1 sin volver a capturar contraseña, si su contexto existente y su uso autorizado pueden acreditarse antes de ejecutar. Esta es una opción condicionada, no evidencia de que tal sesión esté disponible.

Antes de cualquier ejecución, verificar por la UI local el servidor, base y usuario/autenticación de la ventana de consulta concreta; Object Explorer conectado o un nombre de archivo no prueban el contexto de la ventana. No usar `Israel` integrado/administrador, cambiar a sysadmin, impersonar, crear conexiones con secretos o extraer una credencial hacia sqlcmd. sqlcmd no se conecta a una sesión SSMS existente por el mero hecho de que esa aplicación esté abierta.

Si hay sesión exacta vigente, abrir el archivo íntegro cuyo hash se verificó y no editarlo ni ejecutar una selección parcial. La directiva `:ON ERROR EXIT` exige SQLCMD mode compatible en esa ventana; no quitarla para acomodar el editor ni cambiar opciones globales. Si se requiere reconectar, el contexto no es inequívoco, aparece prompt de contraseña o la UI no permite determinar la identidad: **detenerse y registrar BLOCKED**, sin solicitar o automatizar credenciales. Preparar pasos locales revisables para el operador no equivale a ejecución realizada.

La salida del lote aprobado contiene USER_NAME/ORIGINAL_LOGIN y permisos de la conexión. Debe compararse con la identidad esperada si finalmente se ejecuta; un resultado discrepante detiene cualquier continuación. No añadir una consulta previa de identidad porque el humano autorizó exclusivamente el script A1 exacto. No lanzar otro SQL, helper de D2-A, backup, migración, permiso, fixture o prueba negativa.

## Dictamen de evidencia

Recuperación de credenciales persistidas: **NO ACREDITADA**, y no necesaria para la alternativa de sesión ya autenticada. Ejecución A1: **BLOCKED hasta demostrar contexto local existente autorizado**. A1 permanece autorizada; no se solicita renovarla. A2–A8 y aceptación D2-B no derivan de recuperar un mecanismo local.

Se contrastaron completos RECOVERY-LOCAL-WINDOWS.md, RECOVERY-RESULT.md, recovery-context.json y los addenda de EXECUTION-LOG.md/PREFLIGHT-RESULT.md. Son coherentes: revisión documental del mecanismo histórico, capacidades limitadas al proceso sandbox, sin valores secretos ni conexiones SQL, y preparación de un fallback local todavía no ejecutado. Timestamp `2026-10-21T01:04:27.7224473Z` corresponde al 20 de octubre a las 19:04:27.7224473 UTC−06:00; no se atribuye a una sesión SQL.

La ausencia de nombres de variables/módulos/comandos de secretos en recovery-context.json solo describe ese proceso. RECOVERY-RESULT no la extiende al perfil Israel, SSMS o vaults externos, ni afirma que se hayan inspeccionado. El runbook mantiene la ventana de consulta con identidad existente verificada, SQLCMD mode, guardas, archivo completo/hash, detención ante prompts/reconexión y captura de resultados sin inventar exit code de SSMS. No incluye otro script ni habilita privilegios adicionales. La verificación de archivo no sustituye comprobar que el editor ejecuta su contenido íntegro y correcto.

**Resultado final: mecanismo D2-A identificado; secreto persistido reutilizable NO ACREDITADO; fallback local preparado; ejecución A1 BLOCKED.** No se identifican defectos adicionales confirmados en el runbook revisado. El obstáculo pendiente es una sesión autenticada existente accesible/verificable o un mecanismo seguro ya disponible y aplicable, no renovar A1 ni entregar una nueva contraseña. No hay salida SQL en esta revisión; no se afirma acceso concedido/denegado, conexión intentada, permisos efectivos ni baseline actual. NO-GO para pedir A2; D2-B OPEN/BLOCKED.
