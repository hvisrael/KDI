# A1 — paquete de operador

**Estado: A1 autorizada. Procedimiento preparado. NO EJECUTADO.**
Autorización: mensaje humano «KDI — D2-B | Ejecución controlada A1». A1 habilita **únicamente** la ejecución del script revisado `01-preflight-readonly-REQUIRES-A1.sql` con una identidad existente autorizada. No autoriza backup, GRANT, migración, fixtures, DML ni nada de A2–A8.

Este paquete existe porque el bloqueo actual no es de autorización sino de **operador**: el sandbox automatizado corre como `DESKTOP-F25O3MF\CodexSandboxOffline`, que no es la identidad SQL `kdi_test_runner` y no tiene autenticación reutilizable acreditada. Ver [evidence/A1/RECOVERY-RESULT.md](evidence/A1/RECOVERY-RESULT.md).

Procedimiento completo y su trazabilidad: [evidence/A1/RECOVERY-LOCAL-WINDOWS.md](evidence/A1/RECOVERY-LOCAL-WINDOWS.md).

---

## 0. Antes de empezar: las tres prohibiciones

1. **No pedir ni escribir contraseñas.** No en la conversación, ni en un archivo, ni en un argumento, ni en una variable de entorno, ni en evidencia.
2. **No crear cuentas, no conceder permisos, no elevar.** No `runas`, no impersonación, no `sa`/`sysadmin` como sustituto, no elevar el proceso.
3. **No modificar el script.** No añadir `USE`, ni `EXECUTE AS`, ni consultas, ni quitar sus guardas.

Si alguna de las tres condiciones no puede cumplirse, la operación **queda BLOCKED** y se reporta solo esa condición. No hay que forzar la ejecución.

---

## 1. Condición de entrada

| Requisito | Valor exigido |
|---|---|
| Servidor | `DESKTOP-F25O3MF` (instancia predeterminada `MSSQLSERVER`; no escribir `\\MSSQLSERVER`) |
| Base de datos | `KulaniKDI_Test` |
| Identidad SQL | `kdi_test_runner` (rol `kdi_tests`) |
| Tipo de sesión | Conexión **ya autenticada** en una herramienta SQL del operador (SSMS o equivalente) |

> La cuenta Windows que aloja la herramienta puede ser la del administrador del equipo. Eso **no** significa usar autenticación integrada de esa cuenta como sustituto: la identidad SQL debe ser `kdi_test_runner`.
>
> Una etiqueta genérica del explorador de objetos **no** acredita el contexto de otra ventana. El contexto se verifica en la ventana de consulta que se vaya a usar.

## 2. Verificación de archivos (sin SQL, sin conexión)

Ejecutar desde `E:\INSSOFT\KDI`, en PowerShell local **normal, sin elevación**:

```powershell
Set-Location -LiteralPath 'E:\INSSOFT\KDI'
.\docs\stage3\d2b\controlled-test-plan\A1-FILE-CHECK.ps1
```

El script solo lee archivos y calcula hashes. No abre ninguna conexión. Salida esperada: los tres hash correctos y `OK` en cada comprobación.

| Comprobación | Valor esperado |
|---|---|
| Candidato D2-B | `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos) |
| Manifiesto D2-A original | `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f` |
| Script A1 | `60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c` |

Ante cualquier diferencia: **detenerse**. No regenerar manifiestos. No continuar.

## 3. Verificación del contexto de la sesión

En la herramienta SQL, **sin ejecutar nada**:

1. Abrir una ventana de consulta nueva sobre `DESKTOP-F25O3MF` / `KulaniKDI_Test` usando la conexión ya autenticada como `kdi_test_runner`.
2. Comprobar en las propiedades o en el contexto de **esa** ventana: servidor, base de datos e identidad de conexión. Deben mostrar exactamente `DESKTOP-F25O3MF`, `KulaniKDI_Test`, `kdi_test_runner`.
3. Si la herramienta pide reconectar, solicita credenciales o cambia de identidad: **detenerse** y reportarlo. No responder al diálogo de credenciales.

## 4. Ejecución

1. Abrir el archivo completo `01-preflight-readonly-REQUIRES-A1.sql` en la ventana verificada en el paso 3.
2. Activar **Query → SQLCMD Mode**. Es obligatorio: el script usa la directiva `:ON ERROR EXIT` y el cliente debe respetarla.
3. Volver a comprobar contexto, base seleccionada y hash del archivo.
4. Ejecutar **el archivo completo, una sola vez**, sin selección parcial ni ejecución por lotes.
5. No usar el explorador de objetos para nada más.

**Qué hace el script:** consulta metadatos del servidor y de la base, esquema, historias de migración de los tres módulos, tablas y los permisos de la conexión actual. No lee tickets, handles ni identidades reales de aplicación. No hay DDL, DML, DCL, impersonación, backup ni configuración persistente.

**Guardas que deben detener la ejecución si fallan:**

| Guarda | Significado |
|---|---|
| `THROW 51000, 'Unauthorized preflight target.'` | Servidor o base distintos de los autorizados. Parar. |
| `THROW 51000, 'Baseline not visible or absent; stop.'` | No se ve la historia de alguno de los tres módulos. Parar. |
| `ORIGINAL_LOGIN` distinto de `kdi_test_runner` | Desviación de identidad. Parar y registrar, sin ejecutar más SQL. |
| `USER_NAME` que no corresponde al mapping esperado | Parar y registrar. |

Una salida parcial **no** es PASS.

## 5. Captura de evidencia

Al terminar, guardar **solo** los resultados y mensajes de este lote, en archivos nuevos bajo `evidence/A1/`, con la plantilla [A1-EVIDENCE-TEMPLATE.md](A1-EVIDENCE-TEMPLATE.md). No sobrescribir el intento anterior si lo hubiera.

## 6. Cierre

Cerrar la conexión o la ventana de consulta usada mediante el mecanismo normal de la herramienta, **sin detener SQL Server** ni otras sesiones. Si aparece una opción de guardar contraseña, no cambiarla ni guardarla.

## 7. Qué viene después

QA y Code Review revisan la salida real (identidad, baseline, errores, límites). Solo entonces se reevalúa PASS/FAIL/BLOCKED de A1 y se decide si se puede solicitar A2.

**A1 no autoriza A2.** Backup, migración, permisos, fixtures, infraestructura y restauración tienen cada uno su propia autorización y su propia puerta.

---

## Mapa de archivos

| Archivo | Contenido |
|---|---|
| `01-preflight-readonly-REQUIRES-A1.sql` | Script aprobado. No modificar. |
| `A1-FILE-CHECK.ps1` | Verificación de hashes previa, sin SQL. |
| `A1-EVIDENCE-TEMPLATE.md` | Formato de registro de evidencia. |
| `evidence/A1/RECOVERY-LOCAL-WINDOWS.md` | Procedimiento completo con su trazabilidad. |
| `evidence/A1/RECOVERY-RESULT.md` | Por qué A1 sigue bloqueada y qué se ha descartado. |
| `evidence/A1/A2-READINESS.md` | Condiciones para solicitar A2, hoy en NO-GO. |
| `AUTHORIZATIONS.md` | Matriz A0–A8. |
| `GO-NO-GO.md` | Dictamen vigente de preparación. |
