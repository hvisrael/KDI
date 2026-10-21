# Recuperación de mecanismo SQL — intento 2 A1

**A1 BLOCKED. NO-GO FOR A2 AUTHORIZATION REQUEST. D2-B OPEN/BLOCKED.** Observación 2026-10-21T01:04:27.7224473Z, 20 de octubre local. No hubo conexión SQL, prompts de credenciales ni escrituras SQL.

Se revisaron AGENTS, cierre/aceptación D2-A, README y código del provisioner SQL v2, launcher API y runtime-cleanup, más mecanismo OperatorConnection D2-B. D2-A confirma autenticaciones de kdi_migrator/kdi_app/kdi_test_runner y roles; kdi_test_runner fue lectura Test, sin acceso Dev. Las credenciales se capturaron de forma protegida en procesos y se destruyeron en finally. No se documentó almacén persistente. runtime-cleanup confirma API y listener detenidos sin detener SQL Server.

Eso distingue **cuenta SQL configurada** de **secreto reutilizable por el proceso actual**. No hace falta reconstruir cuentas, pero su existencia no suministra una SqlCredential a este sandbox. La comprobación de permisos de D2-A es histórica; el permiso efectivo vigente sigue sin comprobarse en A1.

## Configuración identificada sin secretos

- Servidor de conexión documentado DESKTOP-F25O3MF, servicio predeterminado MSSQLSERVER; Test/Dev conocidas. Versión 2019 Enterprise confirmada por antecedente humano; versión efectiva actual no consultada.
- Identidad recomendada para A1: kdi_test_runner/Test (rol kdi_tests, lectura de historias ya acreditada). No usar kdi_migrator, kdi_app ni Israel/sysadmin como sustituto automático.
- D2-A: Encrypt=true, TrustServerCertificate=true como excepción local documentada; Persist Security Info=false; SqlCredential fuera de connection string; en provisioner Pooling=false. No cambiamos autenticación/TLS ni configuraciones globales.
- Contexto actual DESKTOP-F25O3MF\CodexSandboxOffline. No es la identidad SQL kdi_test_runner y no se cambió de usuario ni se elevó proceso.
- Entorno inspeccionado por **nombres solamente**: familias KDI_, SQLCMD, ConnectionStrings__Kdi, DOTNET_USER_SECRETS y AZURE_KEYVAULT sin coincidencias. Módulos Microsoft.PowerShell.SecretManagement/SecretStore y comandos Get-Secret/Get-SecretVault no disponibles en este proceso. No se leyeron valores secretos ni se invocaron vaults.
- Búsqueda estática en código/documentación del alcance KDI no encontró integración de almacén persistente utilizada por los conectores revisados. No se afirma que Windows/SSMS del usuario Israel carezcan de credenciales guardadas; ese perfil/contexto no fue inventariado ni se extrajeron secretos. No hay una sesión local autenticada accesible/verificada con las herramientas actuales.

Evidencias actuales: recovery-context.json; recovery-candidate-integrity.log y recovery-accepted-integrity.log exit 0. Candidato conserva 190 archivos y a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102. Script intacto 60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c. D1/D2-A congelados verificados; sin modificación de candidato/manifiestos aprobados.

> **Actualización 2026-10-20 (posterior a este resultado).** El candidato D2-B cambió después de este intento: la corrección del harness de fixtures llevó el candidato de `a1cbed38…` (190 archivos) a `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos), con revisión independiente de code-reviewer y qa-engineer. **El script A1 no cambió** y su hash sigue siendo `60dbad78…`, por lo que **A1 sigue vigente y sigue bloqueada por la misma causa**: no hay autenticación SQL reutilizable acreditada en este contexto. Lo que sí cambia es el hash que hay que verificar antes de ejecutar: `A1-FILE-CHECK.ps1` ya usa el vigente. Ver [../../harness-correction-20261020/RESULT.md](../../harness-correction-20261020/RESULT.md) y [RECOVERY-LOCAL-WINDOWS.md](RECOVERY-LOCAL-WINDOWS.md).

## Resultado y acción concreta

No se ejecutó A1 porque el sandbox no dispone de autenticación reutilizable acreditada, y no está permitido solicitar contraseñas ni sustituirla por la cuenta administradora. No es error SQL ni prueba de falta de permisos del runner: no se intentó conexión. Servidor/base/identidad/permisos efectivos/esquemas actuales siguen NOT RUN.

Se preparó [procedimiento exacto local](RECOVERY-LOCAL-WINDOWS.md): ejecutar únicamente el script aprobado en ventana SSMS ya autenticada kdi_test_runner/Test cuyo contexto se compruebe, sin crear conexión/solicitar secreto ni elevar. Si no existe, continuar BLOCKED; no se inventa persistencia de credenciales por antecedentes de autenticación. A1 sigue vigente, sin nueva solicitud de autorización.

Evaluaciones independientes reales: /root/d2b_review (code-reviewer), RECOVERY-CODE-REVIEW.md; /root/d2b_qa (qa-engineer), RECOVERY-QA-REVIEW.md. Principal integra trazabilidad y fallback; no declara conexión o revisión de resultados reales inexistentes. A2–A8 siguen excluidas.
