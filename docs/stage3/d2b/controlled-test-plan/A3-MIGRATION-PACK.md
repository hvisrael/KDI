# A3 — paquete de ejecución de las migraciones

**Estado: A3 autorizado por ruta R1 (aplicar como `kdi_migrator`). Preparado, NO ejecutado.**
A1 y A2 están **completados y verificados** (ver [evidence/A1/A1-A2-RESULT.md](evidence/A1/A1-A2-RESULT.md)). Falta solo este paso, y lo ejecuta una persona: la contraseña de `kdi_migrator` nunca se ha almacenado en este proyecto, por diseño.

## Qué hay que aplicar

Dos archivos, **en este orden**. Son los artefactos revisados del paquete D2-B: sus hashes son los que aprobaron la revisión de seguridad y el QA.

| # | Archivo | SHA-256 (completo en `prepared-sql/manifest.json`) | Qué crea |
|---|---|---|---|
| 1 | `E:\INSSOFT\KDI\.local\d2b-candidate\tools\Kulani.KDI.Migrations\prepared-sql\KulaniKDI_Test-identity.sql` | `B18B5160371F20FD…` | `identity.{AuditEntries, PermissionCatalog, Users, Grants, Sessions}` + 6 índices (3 únicos, 1 filtrado) + 2 CHECK + 2 FK + 8 códigos de permiso + fila de historial `20261019040513_Accessidentity` |
| 2 | `…\prepared-sql\KulaniKDI_Test-organizations.sql` | `5DD317EEF1257AD6…` | `organizations.{AuditEntries, Organizations, OrganizationMemberships}` + 3 índices (2 únicos) + 1 CHECK + 1 FK + fila de historial `20261019040514_Accessorganizations` |

**Los dos archivos tienen guardas propias** que detienen la ejecución si el destino no es `DESKTOP-F25O3MF` / `KulaniKDI_Test`, si falta el esquema, si el historial contiene una migración desconocida o una versión de EF distinta de `10.0.13`, o si falta la migración baseline. **No los modifiques**: no quites guardas, no añadas `USE` ni `EXECUTE AS`.

**No apliques** `KulaniKDI_Dev-*.sql` (otra base, sin autorización) ni `*-runtime-grants.sql` (A4, decisión separada).

## Ruta R1: la identidad y la contraseña

La identidad de migración es `kdi_migrator` (rol `kdi_migrations`), que tiene `CREATE TABLE` en la base y `ALTER/INSERT/UPDATE/DELETE/SELECT/REFERENCES` en los tres esquemas, y **no** tiene permisos de administrador. La contraseña se introduce en el **prompt de sqlcmd**: no aparece en ninguna línea de comandos, variable de entorno, registro ni evidencia.

> **Si no recuerdas la contraseña, detente y dímelo.** No la escribas en el chat. Hay un camino preparado: [ROTATE-MIGRATOR-PASSWORD.sql](ROTATE-MIGRATOR-PASSWORD.sql). Abres ese archivo en SSMS, sustituyes **el marcador** de la línea 17 por la contraseña nueva, lo ejecutas y **no lo guardas**. El archivo en disco conserva siempre el marcador: la contraseña solo existe en el búfer del editor. Además tiene una guarda que aborta si el marcador no se ha sustituido, de modo que ejecutarlo sin tocarlo no cambia nada.
>
> Contexto verificado del login: habilitado, `CHECK_POLICY=1`, `CHECK_EXPIRATION=1`, contraseña fijada el 2026-10-18 21:24:45 y **no caducada**. La rotación es por credencial perdida, no por expiración.
>
> La contraseña se creó el 2026-10-18 en un diálogo protegido y **nunca se guardó en ningún archivo**, por diseño del proyecto. Si prefieres no rotarla, la alternativa es cambiar A3 a la ruta R2 (tu cuenta Windows es sysadmin y conecta sin elevación), pero eso contradice la ruta de menor privilegio que elegiste y cambia quién aplica el DDL.

## Opción 1 · el driver (recomendada) — un solo comando

Abre una **ventana de PowerShell normal e interactiva** en `E:\INSSOFT\KDI` (no redirigida: si la entrada está redirigida, el script se niega a propósito, porque el prompt de contraseña leería de una tubería) y ejecuta:

```powershell
.\docs\stage3\d2b\controlled-test-plan\A3-Apply-Test-Migrations.ps1
```

Si quieres comprobarlo todo **antes** de teclear la contraseña:

```powershell
.\docs\stage3\d2b\controlled-test-plan\A3-Apply-Test-Migrations.ps1 -PreflightOnly
```

El preflight es de solo lectura: verifica los hashes, la integridad del candidato y de D2-A, el estado previo de las tablas, y **demuestra que la sesión de `sqlcmd` tiene `QUOTED_IDENTIFIER ON`** con un control negativo (el mismo archivo debe fallar sin `-I` y pasar con `-I`). No aplica nada.

El driver, en orden:

1. **Verifica los hashes** de los dos artefactos contra el manifiesto revisado. Si no coinciden, se detiene.
2. **Verifica la integridad** del candidato (`freeze.py --verify`) y de D2-A (`verify.py`). Si falta `python`, **falla**: no se omite en silencio.
3. **Comprueba el estado previo**: si alguna tabla D2-B ya existe, se detiene y pide evaluar (`A3-VERIFY.sql`) en lugar de aplicar a ciegas.
4. **Demuestra las opciones SET** de la sesión con control negativo y afirmativo.
5. **Pide la contraseña de `kdi_migrator`** y aplica `KulaniKDI_Test-identity.sql` completo. Si termina con error, **se detiene**: no reintenta, no edita el script y **no** aplica el segundo módulo.
6. Aplica `KulaniKDI_Test-organizations.sql` completo, con el mismo criterio de parada.
7. Crea la **semilla sintética** de usuario para N2 (se puede omitir con `-SkipSeedUser`).
8. Ejecuta la **verificación de solo lectura** y deja todas las salidas en `evidence\A1\`.

Solo tecleas la contraseña **tres** veces: `identity`, `organizations` y la semilla.

### Nota sobre `QUOTED_IDENTIFIER` (causa de un intento fallido)

El paquete D2-B crea columnas calculadas **PERSISTED** (`identity.Users.IssuerBytes`/`SubjectBytes`)
e índices **filtrados** (`identity.Grants` con `WHERE [RevokedAtUtc] IS NULL`). SQL Server exige
`QUOTED_IDENTIFIER ON` para ambos. `sqlcmd` arranca la sesión con `OFF` salvo que se invoque con
`-I`, y **SSMS lo trae en `ON` por eso**: por eso la opción 2 (SSMS) nunca habría suffered este
fallo. Sin `-I`, el `CREATE TABLE` falla con `Msg 1934` y la transacción por módulo revierte
(el intento fallido dejó 0 tablas y el historial con una sola fila por módulo).

El driver invoca `sqlcmd` con `-I` y, antes de aplicar, demuestra que la opción está activa.
Los **scripts SQL revisados no cambian** y sus hashes siguen siendo los de la tabla de arriba.

## Opción 2 · SSMS manual

1. **Abrir SSMS** y conectar a `DESKTOP-F25O3MF` con **Server Authentication: SQL Server Authentication**, login `kdi_migrator`, base `KulaniKDI_Test`. No activar «Forzar cifrado de conexión»; el servidor usa la excepción local `TrustServerCertificate` ya documentada.
2. **Activar SQLCMD Mode**: menú **Query → SQLCMD Mode**. Es obligatorio: los archivos usan `:ON ERROR EXIT` y separadores `GO`.
3. **Verificar el contexto** antes de ejecutar: base `KulaniKDI_Test`, identidad `kdi_migrator`. Si SSMS pide reconectar o cambia de identidad, **detente**.
4. **Ejecutar el archivo 1** completo: `File → Open → File…` → `KulaniKDI_Test-identity.sql` → **Execute** (no selección parcial). Debe terminar con `MIGRATION SUCCESS KulaniKDI_Test/identity`.
5. **Ejecutar el archivo 2** completo: `KulaniKDI_Test-organizations.sql` → **Execute**. Debe terminar con `MIGRATION SUCCESS KulaniKDI_Test/organizations`.
6. **Si un archivo falla**: no lo reintentes, no lo edites, no apliques el otro. Captura el error y repórtalo. El script está en transacciones por módulo, de modo que un fallo deja ese módulo sin aplicar; el estado se lee con la verificación.
7. **Guardar la evidencia**: en `evidence\A1\`, los **Results** y **Messages** de cada ejecución, con la fecha UTC en el nombre. No guardes el diálogo de conexión, ni la contraseña, ni datos de negocio.

## Qué hace el principal al terminar

Ejecuta la verificación de solo lectura y emite el dictamen:

```powershell
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
& $sqlcmd -E -S DESKTOP-F25O3MF -d KulaniKDI_Test -l 8 -t 60 -b -W -i 'E:\INSSOFT\KDI\docs\stage3\d2b\controlled-test-plan\A3-VERIFY.sql'
```

Comprueba: 2 migraciones por módulo con `ProductVersion 10.0.13`; las 8 tablas nuevas presentes y `portfolio` intacto; 8 códigos de permiso; los 6+3 índices; las FK y CHECK; los `rowversion`; y **0 filas** en las tablas nuevas.

## Después: N2 (motor de administración)

La tabla `identity.Users` queda **vacía** tras A3, porque la migración baseline solo crea el esquema. El comando `bootstrap` del CLI exige un usuario activo ya existente, de modo que hace falta **una** semilla sintética: `A3-SEED-USER.sql` — un único usuario, con Id fijo, idempotente, sin organización ni membresía ni grants. El driver la crea por defecto; si la aplicas por SSMS, ejecuta ese archivo en la misma sesión, **después** de A3.

Con la semilla, el CLI `Kulani.KDI.AccessAdmin` puede ejecutar `plan` y `apply` reales:

```powershell
# en la consola privada del operador
.\tools\Kulani.KDI.AccessAdmin\plan  KulaniKDI_Test  <request-bootstrap.json>  <plan.json>
.\tools\Kulani.KDI.AccessAdmin\apply <plan.json>  <SHA-256 del plan>
```

El CLI pide la contraseña de `kdi_migrator` en la consola privada, valida el hash del plan, comprueba que el plan no ha caducado (30 min) y que el operador Windows es el mismo, y escribe auditoría. Comandos disponibles: `bootstrap`, `membership`, `role`, `user-state`, `organization-state`, `sessions-revoke` y `purge`.

La request de arranque ya está preparada en [n2-request-bootstrap.json](n2-request-bootstrap.json), con el `UserId` de la semilla y una organización sintética de código `TEST_N2_ACME`.

## Archivos de este paquete

| Archivo | Contenido |
|---|---|
| `A3-Apply-Test-Migrations.ps1` | Driver: hashes, integridad, estado previo, opciones SET, aplicación, semilla y verificación. Modo `-PreflightOnly` |
| `A3-SET-OPTIONS.sql` | Comprobación de opciones SET de la sesión (solo lectura), con control negativo |
| `ROTATE-MIGRATOR-PASSWORD.sql` | Rotación de la credencial de `kdi_migrator`, con guarda de marcador (el archivo nunca contiene el secreto). **No ejecutada por decisión** |
| `evidence/A1/A3-CREDENTIAL-DEVIATION.md` | Desviación declarada: no se rotan las credenciales, con su alcance y riesgo residual |
| `A3-VERIFY.sql` | Verificación de solo lectura del resultado de A3 |
| `A3-SEED-USER.sql` | Semilla sintética mínima para N2 (idempotente, reversible) |
| `n2-request-bootstrap.json` | Request de arranque para el CLI de N2 |
| `evidence/A1/A1-A2-RESULT.md` | Evidencia de A1 y A2, con el backup y su hash |
| `AUTHORIZATIONS.md` | Matriz A0–A8 y su estado |
| `PLAN.md` | Secuencia de gates vigente |
