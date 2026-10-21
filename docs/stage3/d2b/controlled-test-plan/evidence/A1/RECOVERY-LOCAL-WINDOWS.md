# A1 desde sesión Windows local existente

Procedimiento preparado; NO ejecutado. A1 ya autorizada. No es reconstrucción de infraestructura ni autorización A2–A8. Requiere una conexión local previamente autenticada como kdi_test_runner con acceso Test y contexto verificable. No presupone que esa sesión esté abierta o que exista contraseña guardada.

## Identidad y mecanismo recuperados documentalmente

D2-A acredita SQL authentication para kdi_test_runner, usuario/rol kdi_tests en KulaniKDI_Test y lectura de las tres historias. Servidor utilizado por sus conexiones: DESKTOP-F25O3MF (instancia predeterminada MSSQLSERVER). No sustituir por una instancia nombrada diferente ni añadir `\\MSSQLSERVER` por interpretar el nombre del servicio como instancia nombrada. Israel es cuenta Windows administradora; no implica que deba ser el login SQL de A1.

Las credenciales D2-A eran SecureString/SqlCredential del proceso, no una contraseña exportada a archivo, entorno o la API. Provisioner/API ya finalizaron. No reutilizar sus ejecutables: provisioner puede crear logins/usuarios/membresías y requiere sysadmin; launcher API inicia servicio y pide contraseña. Ninguno ejecuta exclusivamente A1.

## Pasos exactos del operador local

1. En Windows local, usar la herramienta SQL ya disponible y una conexión SSMS **ya autenticada** a DESKTOP-F25O3MF como **kdi_test_runner**, base **KulaniKDI_Test**. La cuenta Windows que aloja SSMS puede ser Israel; no usar autenticación integrada de Israel ni una sesión sa/sysadmin como sustituto. No abrir/configurar una nueva conexión, extraer una contraseña guardada o volver a introducirla por este procedimiento. Si no hay conexión autenticada utilizable, mantener BLOCKED y reportar solo esa condición.
2. Verificar en las propiedades/contexto de la ventana de consulta actual, sin SQL auxiliar, servidor, base e identidad de conexión. Deben mostrar DESKTOP-F25O3MF / KulaniKDI_Test / kdi_test_runner. Una etiqueta genérica de Object Explorer no acredita el contexto de otra ventana. Si login/contexto no son inequívocos, detenerse. Registrar metadatos no secretos; no capturar pantallas de diálogos de conexión ni credenciales.
3. Desde PowerShell local normal, sin elevación ni conexión SQL, ejecutar:

```powershell
Set-Location -LiteralPath 'E:\INSSOFT\KDI'
python docs/stage3/d2b/preparation-20261020/freeze.py --verify
python docs/stage3/d2a/closure/identities-20261018/verify.py
Get-FileHash -LiteralPath 'docs/stage3/d2b/controlled-test-plan/01-preflight-readonly-REQUIRES-A1.sql' -Algorithm SHA256
```

Exigir exit 0 de ambos verificadores, 195 archivos y SHA-256 de candidato d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb. Script: 60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c. No regenerar manifiestos. Ante diferencia, detenerse.

> **Actualización 2026-10-20.** El candidato D2-B cambió tras la corrección del harness de fixtures (`a1cbed38…`, 190 archivos → `d106dac6…`, 195 archivos; ver [../../harness-correction-20261020/RESULT.md](../../harness-correction-20261020/RESULT.md)). El **script A1 no ha cambiado** (hash `60dbad78…` intacto) y este procedimiento sigue siendo válido íntegro contra el candidato vigente. La comprobación automatizada de estos tres hashes es [A1-FILE-CHECK.ps1](../A1-FILE-CHECK.ps1). Los documentos de revisión de la preparación (`QA-READINESS.md`, `SECURITY-SQL-REVIEW.md`) se emitieron sobre el candidato anterior; su alcance y su veredicto NO-GO siguen vigentes y se anotan allí.

4. Abrir el archivo SQL aprobado completo en la ventana cuyo contexto se verificó. Activar **Query → SQLCMD Mode** para respetar `:ON ERROR EXIT`. Volver a comprobar contexto, base seleccionada y hash/archivo; si la herramienta exige reconectar, pide credenciales o cambia de identidad, detenerse. No cambiar el script, añadir USE/EXECUTE AS/consultas ni quitar sus guardas.
5. Ejecutar **una vez el archivo completo**, sin selección parcial. El script solo consulta metadatos/historias y permisos de conexión; no usar Object Explorer para ejecutar otras acciones. Revisar los primeros resultados de destino/contexto; guardas o errores de permisos detienen, sin correcciones ni reintento elevado. ORIGINAL_LOGIN debe ser kdi_test_runner; si difiere, detener y registrar desviación sin ejecutar más SQL. USER_NAME debe corresponder a su mapping esperado; cualquier discrepancia requiere revisión.
6. Guardar Results y Messages exclusivamente de este lote en archivos nuevos bajo evidence/A1/, junto con UTC, contexto de ventana y hash. No guardar configuración de conexión, contraseñas, procesos completos ni datos ajenos al lote. Son resultados de metadatos autorizados, no tickets/handles/datos de aplicación. Si SSMS no produce exit code de proceso, registrar error/success observado en Messages, no inventar exit 0. Una salida parcial no es PASS.
7. Cerrar la conexión/ventana de consulta usada al finalizar mediante el mecanismo normal de SSMS, sin detener SQL Server ni otras sesiones. Si aparece opción de guardar contraseña, no cambiar configuración ni guardarla. Conservar evidencias sin sobrescribir intento inicial.
8. QA y Code Review revisan salida real, identidad, baseline, errores y límites. Solo entonces reevaluar PASS/FAIL/BLOCKED A1 y GO/NO-GO para solicitar A2. No ejecutar backup ni ninguna otra fase.

No se incluye comando sqlcmd `-U/-P`: requeriría un secreto nuevo disponible en ese proceso o un prompt prohibido por la solicitud. Una sesión SSMS no transfiere automáticamente su autenticación a PowerShell/sqlcmd. No lanzar runas, impersonación ni elevación para obtener acceso. Tampoco se inspeccionan almacenes de otros usuarios, memoria de procesos o archivos privados para recuperar contraseñas.

Si no existe sesión ya autenticada o mecanismo seguro disponible, la operación permanece BLOCKED. El dato faltante es disponibilidad de un contexto autenticado existente, no nueva autorización A1 ni nueva credencial. Cualquier método alternativo debe conservar kdi_test_runner/Test, autorización y límites; no se presume permitido por este documento.
