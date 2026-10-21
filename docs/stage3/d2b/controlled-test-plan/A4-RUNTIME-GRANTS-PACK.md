# A4 — permisos runtime de `kdi_application`

**Estado: preparado, NO ejecutado.** El snapshot previo está capturado; la concesión está pendiente de la contraseña de `kdi_migrator` en consola privada.

## Qué concede

El artefacto revisado `KulaniKDI_Test-runtime-grants.sql` (SHA-256 `CB34B1ACDEBCE32163E37125B3A79BD9810A0EC0C2826A0C7DD63B6CE2C3C139`, en el manifiesto revisado de 14 artefactos) concede a `kdi_application` **10 permisos sobre 7 objetos**:

| Esquema | Objeto | Permisos |
|---|---|---|
| `identity` | `Users` | SELECT, INSERT |
| `identity` | `PermissionCatalog` | SELECT |
| `identity` | `Grants` | SELECT |
| `identity` | `Sessions` | SELECT, INSERT, UPDATE |
| `identity` | `AuditEntries` | INSERT |
| `organizations` | `Organizations` | SELECT |
| `organizations` | `OrganizationMemberships` | SELECT |

Y **nada más**: sin `DELETE` en ningún sitio, sin permiso sobre `organizations.AuditEntries`, sin concesiones de esquema, sin `ALTER ROLE`, sin crear logins ni usuarios.

## Por qué es el conjunto exacto

Es el resultado de una revisión de seguridad que **detectó y corrigió** un `DELETE` innecesario sobre `identity.Sessions` en las propuestas iniciales. Ese `DELETE` no ha vuelto, y la verificación de A4 falla si aparece.

Los permisos de solo lectura que necesita la API para resolver pertenencia y permisos son precisamente estos: `Users` (identidad local), `PermissionCatalog` (los ocho códigos), `Grants` (permisos efectivos), `Organizations` y `OrganizationMemberships` (pertenencia). `Sessions` necesita INSERT y UPDATE porque el login emite y renueva la sesión; `AuditEntries` necesita INSERT porque toda mutación se audita.

## Snapshot previo (obligatorio, capturado)

`evidence/A1/A4-snapshot-antes-20261021.log`, exit 0. Estado antes de conceder:

| Observación | Valor |
|---|---|
| `kdi_application`, permisos de **objeto** | **ninguno** — A4 sin aplicar |
| `kdi_application`, a nivel de base | `CONNECT` GRANT; `CREATE TABLE/SCHEMA/VIEW/PROCEDURE/FUNCTION` y `ALTER` de esquema: **DENY** |
| `kdi_migrations` | `CREATE TABLE` en base; `ALTER/DELETE/INSERT/REFERENCES/SELECT/UPDATE/VIEW DEFINITION` en los tres esquemas |
| `kdi_tests` | `CONNECT` + `SELECT` en los tres esquemas — **solo lectura** |
| Membresías | `kdi_app`→`kdi_application`, `kdi_migrator`→`kdi_migrations`, `kdi_test_runner`→`kdi_tests` |
| `kdi_app` efectivo | no sysadmin, no `db_owner`, `CONTROL`=0, `CREATE TABLE`=0, `CREATE SCHEMA`=0, `ALTER` en los tres esquemas=0, miembro de `kdi_application`=1 |
| `kdi_test_runner` efectivo | no sysadmin, no `db_owner`, `CONTROL`=0, `CREATE TABLE`=0, `ALTER`=0, miembro de `kdi_tests`=1 |

Dato relevante: **`kdi_app` ya cumple hoy todas las condiciones del interceptor de privilegios del runtime** (`RuntimePrivilegeInterceptor.CheckSql`): no es sysadmin, no es `db_owner`, es miembro de `kdi_application` y no tiene `CONTROL`, `CREATE TABLE`, `CREATE SCHEMA` ni `ALTER` de esquema. Es decir, A4 no abre la puerta al DDL: solo le da lectura de la identidad y la pertenencia, y escritura en las tres tablas que la propia API usa.

## Verificación posterior

`A4-VERIFY.sql` comprueba el resultado con **aserciones duras**, y falla si algo no cuadra exactamente:

- **B1** el conjunto de permisos es **exactamente** el aprobado: ni uno más ni uno menos (compara en los dos sentidos).
- **B2** `kdi_application` **no tiene `DELETE`**.
- **B3** `kdi_application` **no tiene ningún permiso** sobre `organizations.AuditEntries`.
- **B4** `kdi_application` **no tiene concesiones de esquema**.
- **B5** los seis `DENY` de DDL siguen intactos.
- **C** `kdi_tests` **sigue siendo de solo lectura** (ningún `INSERT`, `UPDATE` ni `DELETE`).
- **D** las membresías de rol siguen siendo exactamente tres, y los logins de aplicación siguen siendo tres.
- **E** `kdi_app` sigue cumpliendo todas las condiciones del interceptor de runtime.

**La verificación no es vacía: comprobada antes de aplicar.** Ejecutada con el estado actual, falla y **nombra los diez permisos que faltan**, uno por concesión aprobada:

```
FALTANTE (aprobado y no concedido) identity           AuditEntries          INSERT
FALTANTE (aprobado y no concedido) identity           Grants                SELECT
FALTANTE (aprobado y no concedido) identity           PermissionCatalog     SELECT
FALTANTE (aprobado y no concedido) identity           Sessions              INSERT
FALTANTE (aprobado y no concedido) identity           Sessions              SELECT
FALTANTE (aprobado y no concedido) identity           Sessions              UPDATE
FALTANTE (aprobado y no concedido) identity           Users                 INSERT
FALTANTE (aprobado y no concedido) identity           Users                 SELECT
FALTANTE (aprobado y no concedido) organizations      OrganizationMemberships SELECT
FALTANTE (aprobado y no concedido) organizations      Organizations         SELECT
Msg 51000: The object permissions of kdi_application are not exactly the approved set.
```

## Cómo se ejecuta

En una **ventana de PowerShell interactiva** (no redirigida):

```powershell
.\docs\stage3\d2b\controlled-test-plan\A4-Apply-Runtime-Grants.ps1
```

Para comprobarlo todo antes de teclear la contraseña:

```powershell
.\docs\stage3\d2b\controlled-test-plan\A4-Apply-Runtime-Grants.ps1 -PreflightOnly
```

El driver, en orden: verifica el hash del artefacto contra el manifiesto revisado · verifica la integridad del candidato y de D2-A · comprueba que `kdi_application` **no** tiene todavía permisos de objeto (si los tiene, se detiene: A4 ya se aplicó) · concede · ejecuta la verificación con aserciones duras · toma el snapshot posterior para comparar con el previo.

**Si la verificación falla, el permiso queda concedido**: el driver lo dice y **no revoca por su cuenta**. Una revocación requiere revisión aparte.

## Relación con OPEN-8

OPEN-8 sigue **abierto** y es independiente de A4: el harness de fixtures necesita `INSERT` y `DELETE` en siete tablas, y `SqlTarget` solo admite autenticación Windows integrada o un `SqlCredential` de `kdi_app`, **nunca** como `kdi_test_runner`. Conceder a `kdi_tests` (decisión ya tomada) declara el conjunto de permisos del fixture, pero no hace que el harness pueda autenticarse con ese login. A4 no cambia nada de eso: `kdi_tests` permanece con `SELECT` únicamente, y la verificación **C** lo comprueba.

## Archivos de este frente

| Archivo | Contenido |
|---|---|
| `A4-Apply-Runtime-Grants.ps1` | Driver: hash, integridad, estado previo, concesión, verificación, snapshot posterior |
| `A4-VERIFY.sql` | Verificación con aserciones duras del conjunto exacto de permisos |
| `A4-PERMISSION-SNAPSHOT.sql` | Snapshot de solo lectura, ejecutado antes y después |
| `evidence/A1/A4-snapshot-antes-20261021.log` | Snapshot previo, exit 0 |
