# ReviewResult — qa — recuperación A1

**BLOCKED para ejecutar A1; NO-GO A2.** La configuración histórica identifica los logins SQL, pero no proporciona una credencial reutilizable acreditada ni una sesión actual autenticada. No se solicita nuevamente una contraseña ni se sustituye por sysadmin. A1 conserva su autorización para el script exacto; la recuperación de contexto sigue pendiente.

TaskInput `D2B-A1-RECOVER intento1`, presupuesto 15 minutos. Agente `/root/d2b_qa`, perfil efectivo `qa-engineer`. Fuentes: AGENTS.md; cierre D2-A identities-20261018/{RESULT.md,runtime-cleanup.json}; Program.cs del candidato D2-A; helper histórico Provision-Identities.ps1; Program.cs, OperatorConnection.cs, SqlTarget.cs y PersistenceRegistration.cs del candidato D2-B; contexto A1 y documentos de recuperación del principal cuando disponibles. QA no conecta SQL, extrae secretos, inspecciona memoria, usa IDI ni modifica producto; su única escritura es este informe.

## Evidencia comprobada

`freeze.py --verify` independiente: exit 0, 190 archivos, candidateHash `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102`. Get-FileHash independiente del script A1 coincide con `60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c`. Sin regenerar manifiesto ni modificar SQL.

El RESULT D2-A registra autenticación histórica de kdi_test_runner/Test y rechazo Dev, rol kdi_tests de lectura; kdi_app y kdi_migrator tuvieron roles distintos. Declara contraseñas capturadas en entrada privada, sin persistir en archivos/configuración/argumentos/evidencia. runtime-cleanup.json registra cierre del proceso API/listener temporal; no cierre SQL Server. Esto acredita el cierre histórico, no sesiones SSMS actuales ni secreto guardado.

El helper histórico mantiene SecureString en memoria durante su proceso, usa SqlCredential y al finalizar dispone/limpia su colección de secretos. Program.cs D2-A/D2-B pide kdi_app mediante consola privada ReadKey sin eco; rechaza redirección, construye SqlCredential en memoria y no implementa recuperación desde almacén. OperatorConnection D2-B solicita kdi_migrator de igual forma; no es una vía para kdi_test_runner ni autoriza migraciones bajo A1. No volver a ejecutar esos prompts como supuesto mecanismo de recuperación sin contraseña.

SqlTarget runtime acepta exclusivamente kdi_app con SqlCredential o Windows integrada en la otra rama; el runner SQL existente exige Windows integrada. Ninguna de esas rutas es un cliente A1 ya preparado para reutilizar autenticación SQL kdi_test_runner. No modificar el candidato para acomodar la recuperación.

recovery-context.json informa UTC 2026-10-21T01:04:27.7224473Z, proceso DESKTOP-F25O3MF\CodexSandboxOffline, sin nombres de variables de conexión ni comandos/módulos de secret store encontrados en ese proceso; credentialValuesRead=false, sqlConnectionAttempted=false, sqlExecuted=false. Su alcance explícito es el sandbox actual, no el perfil Israel ni bóvedas Windows. No concluir que ningún secreto exista en otro contexto por esta búsqueda negativa acotada.

## Fallback admisible y límites

Una sesión SSMS **ya autenticada** como kdi_test_runner contra DESKTOP-F25O3MF / KulaniKDI_Test podría servir si su identidad, destino y estado se acreditan mediante contexto existente, sin ejecutar SQL adicional, introducir contraseña, reconectar automáticamente ni acceder a datos ajenos. La mera entrada en servidores registrados, un cuadro de conexión prellenado, proceso SSMS abierto, historial de conexión o captura histórica no prueba sesión actual autenticada.

Verificar que el editor que ejecutará A1 sea el de esa misma sesión/base y tenga modo SQLCMD compatible con `:ON ERROR EXIT`; abrir únicamente el archivo íntegro autorizado y verificar hash sin edición. Si habilitar el modo o seleccionar una conexión requiere configuración fuera de autorización, escalar la decisión. Una sesión Object Explorer distinta no acredita automáticamente la identidad del editor. No usar consultas auxiliares, EXECUTE AS, comandos de sistema ni comprobaciones de login adicionales; el propio script autorizado devolvería USER_NAME/ORIGINAL_LOGIN después de ejecutarse.

No leer/descifrar/exportar contraseñas guardadas, configuración protegida, memoria o almacenes de otra identidad; no mostrar secretos en capturas. Un secret store solo sería utilizable si su existencia, ámbito, identidad autorizada y mecanismo seguro ya están acreditados sin revelar el secreto. Configuración de login SQL no equivale a contraseña recuperable.

## Veredicto

ReviewResult tipo qa, candidato indicado, **BLOCKED**: todavía no hay evidencia de sesión autenticada o mecanismo reutilizable disponible. Recuperar metadatos de configuración D2-A es útil, pero no completa autenticación actual ni A1. Si no se acredita sesión/contexto seguro, mantener detención sin pedir otra vez credenciales ni usar una cuenta más privilegiada. A2 y pruebas reales siguen pendientes.

## Contraste final documental

Leídos RECOVERY-RESULT.md y RECOVERY-LOCAL-WINDOWS.md, recovery-context.json y logs de integridad. Los documentos distinguen correctamente cuenta configurada, autenticación histórica, secreto no persistido y sesión actual no acreditada. La búsqueda negativa queda limitada al sandbox actual, sin afirmar inventario de bóvedas/perfil Israel. No proponen reejecutar provisioner ni launcher API, solicitar contraseñas, usar sysadmin automáticamente, ampliar SQL ni renovar A1.

El procedimiento SSMS es condicional: exige sesión ya autenticada kdi_test_runner/Test verificable sin SQL auxiliar; mismo editor/contexto; SQLCMD Mode para el archivo completo; comprobación de hash; detención ante reconexión o prompt; resultados y Messages nuevos sin inventar exit code. Aclara que SSMS no transmite su autenticación a sqlcmd. No afirma que exista una sesión disponible y conserva BLOCKED en caso contrario. No se identifican hallazgos documentales adicionales que requieran modificar producto o el script autorizado.

Los logs del principal conservan candidateHash/190 archivos y verificación de D2-A; son integridad de archivos, no conectividad. QA verificó de forma independiente hash candidato/script y no realizó inventario UI de SSMS ni de credenciales; no se atribuye una sesión observada. **Resultado final: recuperación documental completada; recuperación de autenticación actual y ejecución A1 BLOCKED; NO-GO solicitar A2.** Siguiente acción concreta: comprobar disponibilidad de la sesión local existente bajo las condiciones del procedimiento; si no existe, registrar bloqueo sin pedir nuevamente secretos. Única escritura QA: RECOVERY-QA-REVIEW.md.
