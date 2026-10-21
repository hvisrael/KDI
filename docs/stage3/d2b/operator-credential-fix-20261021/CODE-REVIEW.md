# D2-B — revisión independiente del delta de credencial de operador

**Rol:** `code-reviewer` (perfil funcional de AGENTS.md). **agent_type efectivo del harness:** `general`.
**No se delegó.** El candidato no se modificó (verificado al final: `freeze.py --verify` → exit 0, mismo hash).

- **Candidato revisado:** `E:\INSSOFT\KDI\.local\d2b-candidate`
- **candidateHash (SHA-256 del manifiesto):** `7a057a2e8d0be09e0e2bb52d9864afa9baaeb139cf5badcbc03a02bb4e744c6f` — 196 archivos, verificación OK.
- **Candidato anterior:** `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` — 195 archivos.
- **Delta:** 1 modificado (`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs`, `745fff84…` → `b2a445c7…`), 1 nuevo (`tests/Kulani.KDI.UnitTests/OperatorCredentialTests.cs`), 0 bajas, 194 intactos.
- **D2-B sigue OPEN/BLOCKED.** Este dictamen es una entrada más; no hay PASS global.

## 1. Método y evidencia propia

| Comprobación | Cómo | Resultado |
|---|---|---|
| Integridad del candidato | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` | `SHA256 7a057a2e…`, 196 archivos, exit 0 (antes y después de la revisión) |
| Delta real | `manifest-diff.py --old candidate-manifest.20261020-d106dac6.json --new candidate-manifest.json` | 1 alta, 1 cambio, 0 bajas, 194 sin cambio |
| Archivos de la revisión | Copia íntegra del candidato a `C:\Users\Israel\AppData\Local\Temp\opencode\kdi-cr-d2b` (196/196 archivos con SHA-256 idéntico al manifiesto) | Build y pruebas sobre la copia, nunca sobre el candidato |
| Semántica de `SecureString`/`SqlCredential` | App de sonda propia (`secprobe`) con **Microsoft.Data.SqlClient 6.1.7** (versión del `packages.lock.json`), sin conexión | `Copy()` → `IsReadOnly=False`; `SqlCredential` con SecureString escribible → `ArgumentException: password debe marcarse como de solo lectura.`; `credential.Password` es **la misma referencia** (`sameRef=True`); `SqlConnection.Dispose()` **no** destruye el SecureString (`len=25` tras dispose) |
| Mutación (el test detecta el defecto) | En la copia: sobrecarga nueva con `password.Copy()` sin `MakeReadOnly()` (estado intermedio pre-corrección) | `OperatorCredentialTests`: **3 de 4 fallan** con `ArgumentException`, 1 pasa (base de datos no autorizada). Reproduce exactamente la afirmación del implementador |
| Alternativa `internal` + `InternalsVisibleTo` | En la copia: sobrecarga `internal` + `src/Kulani.KDI.Infrastructure/Properties/AssemblyInfo.cs` con `[assembly: InternalsVisibleTo("Kulani.KDI.UnitTests")]` | `dotnet build Kulani.KDI.slnx -c Release`: 0 advertencias / 0 errores; `OperatorCredentialTests` 4/4 correctas. **No hace falta tocar ningún `.csproj`** |
| Regresión | Build Release + 3 suites sin SQL sobre la copia con contenido byte-idéntico al candidato (196/196 SHA-256 verificados tras revertir la mutación) | Build 0/0; UnitTests **61/61**, ApiTests **20/20**, ArchitectureTests **24/24**; 0 omitidas |
| Claim D2-A vs D2-B | Hash del fichero en los 5 manifiestos archivados + lectura del fichero | `SqlCredentialTargetTests.cs` = `c562f97931f16dba2004e97b2441dfc682a6536a45b68e3e82bbbe19409600458` en los 5; idéntico al actual |
| Por qué no se cazó antes | `grep` en `docs/stage3/d2b/controlled-test-plan/evidence/A1/*.log` y lectura de `A3-RESULT.md` | A3 se aplicó con `sqlcmd`/SSMS, **no** con `Kulani.KDI.Migrations --apply`; N2 (`AccessAdmin`) quedó "preparado, NO ejecutado". Ningún log menciona `AccessAdmin` ni `Kulani.KDI.Migrations` |

## 2. Hallazgos por severidad

### H1 — MEDIA — Superficie pública añadida que relaja el control de «consola privada interactiva»
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:31` — `public static SqlConnection Create(string database, SecureString password)`.

La sobrecarga permite supplying el material de contraseña **programáticamente**, sin la guarda de `Console.IsInputRedirected/IsOutputRedirected` (línea 13) que protege la entrada por teclado. La guarda existe por diseño del proyecto (`docs/stage3/d2b/controlled-test-plan/TEST-MATRIX.md:16`: «Credenciales kdi_migrator/kdi_app vigentes entregadas exclusivamente por consola privada sin captura»), y el propio comentario del código (líneas 35-40) documenta que la sobrecarga existe para "poder ejercitarse sin consola".

- Por qué importa: la superficie pública de `Kulani.KDI.Infrastructure` es alcanzable desde la API y desde cualquier herramienta; una ruta que acepte la credencial por código elimina la garantía de que la contraseña la teclea una persona en una sesión privada y no un proceso automatizado. Hoy el riesgo es **latente** (los CLI siguen usando la ruta interactiva), pero es una relajación de control en la ruta más sensible del producto, y contradice la matriz de autenticación vigente.
- Mitigación probada y barata: `internal` + `InternalsVisibleTo("Kulani.KDI.UnitTests")` (verificado: build de la solución 0/0 y 4/4 tests). No requiere modificar ningún `.csproj` (los ensamblados no están firmados: `Directory.Build.props` no define `SignAssembly`).
- **Recomendación: hacer `internal` la sobrecarga y añadir el atributo de ensamblado.** Alternativa equivalente si el Human Orchestrator no acepta `InternalsVisibleTo`: devolver un tipo propio (p. ej. `OperatorSession : IAsyncDisposable`) que sea `internal` y exponga la construcción de la credencial, dejando `public` únicamente `Create(string)`.

### H2 — MEDIA — La copia entregada al `SqlCredential` queda huérfana: material sensible sin destrucción determinista
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:41-51`.

Hechos verificados (sonda propia, SqlClient 6.1.7):
1. `SqlCredential` **guarda la referencia** del `SecureString` recibido (`ReferenceEquals(credential.Password, copy) == True`).
2. `SqlCredential` **no** es `IDisposable` y `SqlConnection.Dispose()` **no** destruye ese `SecureString` (tras `conn.Dispose()`, `credential.Password.Length` sigue siendo 25 y `IsReadOnly` sigue siendo `True`).
3. `OperatorConnection` no devuelve ni expone la copia: nadie puede destruirla. Su destrucción depende del finalizador de `SecureString` (GC), es decir, **no determinista**.

Contraste con el camino ya aceptado de la API (`src/Kulani.KDI.Api/Program.cs:109-134`): `ReadRuntimePassword` entrega **el original** (`result.MakeReadOnly(); return result;`) y ese original queda bajo `using var password` en el programa de nivel superior, por lo que **sí** se destruye de forma determinista al cerrar el proceso. La divergencia es real y en contra del candidato.

- (a) Buffer tecleado: **correcto**. `Create(string)` usa `using var password = new SecureString()` (línea 14) → el buffer tecleado se destruye al salir del método, incluso en el camino de error.
- (b) Copia en el camino de éxito: **huérfana**, sin destrucción determinista.
- (c) Camino de error: el `catch { credentialPassword.Dispose(); throw; }` (líneas 47-51) destruye la copia; pero esa rama es **defensiva e inalcanzable** en la práctica (con la copia ya de solo lectura, ni `SqlCredential` ni `new SqlConnection` lanzan), de modo que en el fallo real de la construcción **no hay** `SecureString` sensible sin destruir. Correcto, aunque la rama no sea alcanzable ni testeable.

- Por qué importa (con honestidad de alcance): en Windows el contenido de `SecureString` está cifrado con DPAPI, el proceso del operador es de vida corta y termina tras la operación; el riesgo práctico es **bajo**. Pero la invariante que el propio proyecto acepta en la API («el buffer se destruye de forma determinista») queda rota en la ruta de operador, y el material sensible sobrevive más allá del uso.
- **Recomendación:** hacer explícita la propiedad de la copia: que `OperatorConnection` devuelva unTitular disposing (p. ej. `internal`/`public` `OperatorSession : IAsyncDisposable` que property `Connection` y destruya la copia en `Dispose`), o que los CLI hagan `finally { connection.Credential?.Password?.Dispose(); }` tras cerrar la conexión. Añadir un test que compruebe que tras destruir el ámbito el `SecureString` queda destruido (`Length` → `ObjectDisposedException`).

### H3 — BAJA — El camino de producto (interactivo) y el cableado de los CLI no están cubiertos por ningún test
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:10-26` y `tools/Kulani.KDI.AccessAdmin/Program.cs:33`, `tools/Kulani.KDI.Migrations/Program.cs:36`.

El test nuevo ejercita **la sobrecarga nueva**, no `Create(string database)`, que es la ruta que usan realmente los dos CLI. Consecuencias:
- El bucle de lectura por teclado y su invariante (`buffer tecleado destruido al salir`, `password.MakeReadOnly()`, rechazo de contraseña vacía) no tienen aserción.
- Un futuro cambio que hiciera que `Create(string)` entregara el buffer tecleado directamente al `SqlCredential` (sin copia) **no** sería detectado por el test actual.
- No existe test de integración del cableado de los CLI (invocación en modo no interactivo → debe fallar con «A private interactive console is required.» y no tocar SQL).

**Recomendación:** extraer la lectura de consola a un seam interno testeable (p. ej. `internal static SecureString ReadPassword(Func<ConsoleKeyInfo> readKey)`) y añadir: (i) test de que el buffer de entrada queda destruido al salir, (ii) test de que la contraseña vacía se rechaza, (iii) test del CLI en modo no interactivo que compruebe la guarda.

### H4 — BAJA — La sobrecarga acepta `SecureString` vacío (solo lectura) sin rechazarlo
`OperatorConnection.cs:31-52`. El camino interactivo rechaza la contraseña vacía («Credential required.», línea 24); la sobrecarga no comprueba `password.Length == 0`, de modo que `Create(db, SecureString vacío de solo lectura)` construye una credencial con contraseña vacía. Impacto bajo (el fallo aparecería en `Open`), pero es una asimetría entre las dos rutas.

### H5 — BAJA — Documentación de D2-B desactualizada fuera del candidato
- `docs/stage3/d2b/STATUS.md:9` declara vigente `d106dac6…` (195 archivos) — el candidato vigente es ahora `7a057a2e…` (196).
- `docs/stage3/d2b/preparation-20261020/RESULT.md:44` declara «190 archivos; SHA-256 `1ab77cd8…`».
- `docs/stage3/d2b/preparation-20261020/AUTH-SECURITY-MATRIX.md:17` y `STATUS.md:7` declaran «57 unitarias»; con este delta son **61**.
No son parte del candidato (frozen), pero el paquete de evidencia de D2-B debe reflejar el hash y los conteos vigentes antes de QA.

### H6 — INFORMATIVA — `SecureString` sin cifrar fuera de Windows
`tools/Kulani.KDI.Migrations/Program.cs` no exige Windows (`AccessAdmin/Program.cs:10` sí). Fuera de Windows `SecureString` no cifra su contenido. Impacto bajo (el objetivo es una instancia SQL Server Windows local fija) y el material no sale del proceso.

### Observaciones favorables (verificadas)
- La **causa raíz** está correctamente identificada y corregida: `password.Copy()` no es de solo lectura y `SqlCredential` exige solo lectura (sonda: `Copy().IsReadOnly=False`, `SqlCredential` → `ArgumentException: password debe marcarse como de solo lectura.`).
- El buffer entregado al `SqlCredential` es **la copia** (no el original) y queda de solo lectura antes de construir la credencial. **No queda ningún camino** en `OperatorConnection` donde el `SecureString` recibido por `SqlCredential` sea escribible.
- `src/Kulani.KDI.Api/**` es **byte-idéntico** (`Program.cs` = `400f4685…` en los 6 manifiestos); `ReadRuntimePassword` no se tocó.
- El test nuevo no contiene credenciales reales (`"synthetic-operator-secret"`), no abre conexión ni usa red (`Assert.Equal(ConnectionState.Closed, connection.State)`), y no tiene aserciones tautológicas: fallaría ante cualquier regresión de la invariante (demostrado por mutación).
- El acoplamiento a `SqlCredential.Password` (API pública y estable del driver) es **aceptable**: la alternativa (reflexión sobre internos) sería peor; y el test usa esa propiedad precisamente para comprobar la invariante de solo lectura.

## 3. Veredicto sobre la corrección
**PASS.** La corrección resuelve la causa raíz. Reproducción del fallo: estado pre-corrección (sobrecarga presente, `Copy()` sin `MakeReadOnly()`) → `System.ArgumentException: password debe marcarse como de solo lectura.` en `SqlCredential..ctor` en las 3 pruebas que construyen la credencial. Con la corrección, la copia se marca de solo lectura antes de entregarla y el `SqlCredential` se construye sin excepción; `SqlCredential` guarda esa copia, que es exactamente la que debe sobrevivir a la destrucción del buffer tecleado.

**No queda ningún camino con el defecto** dentro de `OperatorConnection`. Fuera de él, la otra construcción de credencial del producto (`src/Kulani.KDI.Api/Program.cs:130`) ya entregaba un `SecureString` de solo lectura y no se ha tocado.

## 4. Veredicto sobre ciclo de vida y superficie pública
- **Ciclo de vida: PASS parcial / hallazgo abierto (H2).** (a) buffer tecleado: determinista ✔. (c) camino de error: sin residuo sensible ✔ (rama defensiva inalcanzable). (b) copia huérfana en el camino de éxito: **no** hay destrucción determinista ✘. Divergencia respecto del camino API: **no aceptable tal cual**; se acepta si se documenta la excepciones y el proceso es de vida corta, o mejor si se corrige con propiedad explícita (H2).
- **Superficie pública: FAIL (H1).** `public Create(string, SecureString)` es una **relajación** del control de entrada de credencial, no un endurecimiento: elimina la condición de «consola privada interactiva» para la construcción de la credencial de `kdi_migrator`. Recomendación: **`internal` + `InternalsVisibleTo("Kulani.KDI.UnitTests")`** (probado: 0 errores/0 advertencias, 4/4 tests, sin tocar `.csproj`), con `public` solo para `Create(string)`.

## 5. Veredicto sobre el test nuevo
`tests/Kulani.KDI.UnitTests/OperatorCredentialTests.cs`
- (a) **¿Detecta el defecto?** Sí. Mutación aplicada → 3 de 4 fallan con `ArgumentException`; el caso restante (base de datos no autorizada) falla antes de construir la credencial, por lo que es el único que no discrimina. Afirmación del implementador **confirmada**.
- (b) **¿Credenciales reales?** No. Solo `"synthetic-operator-secret"`.
- (c) **¿Red/conexión?** No. `SqlConnection` se construye pero nunca se abre (`State == Closed`); `SqlConnectionStringBuilder` es local.
- (d) **¿Afirma lo que dice?** Sí, sin tautologías: comprueba `UserId`, que el buffer entregado **no es** el de entrada (`NotSame`), que es de solo lectura (`IsReadOnly`), que conserva la longitud tras destruir el buffer de entrada, y las propiedades del `connection string` (destino, base, `Encrypt=Mandatory`, `Persist Security Info=false`, sin `UserID`/`Password` embebidos, conexión cerrada).
- (e) **Acoplamiento a `SqlCredential.Password`:** aceptable (API pública y estable del driver).
- **¿Suficiente?** No del todo: falta el test del **camino interactivo real** y del cableado de los CLI (H3), y falta una aserción de destrucción determinista del material (H2). El test #2 («la copia sobrevive a la destrucción del buffer tecleado») es la aserción más valiosa del archivo: es la que fija la razón de ser de la copia.

## 6. Verificación de la afirmación D2-A vs D2-B
**CONFIRMADA con cuentas propias.**
- `tests/Kulani.KDI.UnitTests/SqlCredentialTargetTests.cs` tiene SHA-256 `c562f97931f16dba2004e97b2441dfc682a6536a45b68e3e82bbbe19409600458` en los **cinco** manifiestos archivados (`80ff7c4c`, `a1cbed38`, `cee627f6`, `d106dac6`, `e73f02a8`) y en el actual: **byte-idéntico**. El hash real del fichero en disco coincide.
- Ninguno de esos tests ejercita `SecureString.Copy()` ni `OperatorConnection.Create`: construyen `new SecureString(); AppendChar('x'); MakeReadOnly();` y lo entregan **original** a `new SqlCredential("kdi_app", secret)` (líneas 20-22, 30-32, 52-54, 75-77). El `grep` de `SecureString` en todo el candidato solo devuelve ese fichero, el nuevo y `Program.cs`.
- **Por qué el defecto no se cazó:** los tests de credencial nunca tocaron la ruta de operador; y la ruta de operador **nunca se ejecutó**: A3 se aplicó con `sqlcmd`/SSMS (`evidence/A1/A3-RESULT.md`), N2 (`AccessAdmin`) quedó «preparado, NO ejecutado» y ningún log de `evidence/A1` menciona `AccessAdmin` ni `Kulani.KDI.Migrations`. El defecto estaba **latente**, no detectado por un hueco de test unitario sino por un hueco de **integración de CLI**.
- **Valoración:** el test nuevo cierra el hueco *unitario* del invariante de la credencial, pero **no** cierra el hueco que realmente lo dejó pasar: la ejecución del CLI. Por eso H3 (test de integración del cableado, al menos del guard de consola en modo no interactivo) es necesario.

## 7. Regresión
Sobre copia byte-idéntica al candidato (196/196 SHA-256 verificados), `dotnet build Kulani.KDI.slnx -c Release --no-restore`: **Compilación correcta, 0 advertencias, 0 errores** (`TreatWarningsAsErrors=true`).

| Suite | Resultado |
|---|---|
| `tests/Kulani.KDI.UnitTests` | **61/61** correctas, 0 fallidas, 0 omitidas (antes 57 → +4) |
| `tests/Kulani.KDI.ApiTests` | **20/20** correctas, 0 fallidas, 0 omitidas |
| `tests/Kulani.KDI.ArchitectureTests` | **24/24** correctas, 0 fallidas, 0 omitidas |

`src/Kulani.KDI.Api/**` sin cambios (byte-idéntico). **No** se ejecutó `Kulani.KDI.SqlIntegrationTests` ni ninguna suite que abra conexión. El cambio **no altera** nada de lo ya aceptado.

## 8. Recomendación
**No pasa a la cola de QA tal cual.** Se recomienda una **ronda corta (R2) sobre el candidato** con tres cambios acotados, y después QA:

1. **H1** → sobrecarga `internal` + `[assembly: InternalsVisibleTo("Kulani.KDI.UnitTests")]` (probado viable; 1 fichero nuevo en Infrastructure, sin tocar `.csproj`).
2. **H2** → propiedad explícita y destrucción determinista de la copia (ámbito disposable propio, o `finally` en los CLI), con test que lo compruebe.
3. **H3** → test del camino interactivo (seam interno de lectura de teclado) y del guard de consola en modo no interactivo para los CLI.

Mayormente menores y no bloqueantes: H4 (rechazar contraseña vacía en la sobrecarga), H5 (actualizar `STATUS.md` / `RESULT.md` / `AUTH-SECURITY-MATRIX.md` con el hash y los conteos vigentes), H6 (documentar la dependencia de Windows en `Migrations --apply`).

La **corrección de la causa raíz, en sí, es correcta, está verificada y no debe volver a tocarse**: lo que se revisa en R2 es la **superficie** y el **ciclo de vida** alrededor de ella.

## 9. Qué NO se pudo verificar y por qué; qué NO se hizo
- **NOT VERIFIED:** autenticación efectiva de `kdi_migrator` contra SQL Server con la credencial construida por la ruta corregida, y ejecución real de `AccessAdmin plan/apply` y `Migrations --apply`. Motivo: prohibiciones explícitas de este rol (no ejecutar pruebas SQL ni abrir conexiones a SQL Server) y ausencia de autorización humana para esas operaciones; además requieren una consola privada interactiva no disponible en este entorno. No se declara FAIL: la construcción de la credencial sí está verificada sin SQL.
- **NOT VERIFIED:** comportamiento de `SecureString` en sistemas no Windows (cifrado) — no ejercitado; se cita comportamiento documentado, no medido (H6).
- **NOT VERIFIED:** que la corrección funcione con una versión de `Microsoft.Data.SqlClient` distinta de la fijada en el candidato; solo se verificó con **6.1.7**, que es la versión fijada en `packages.lock.json` del candidato.
- No se ejecutó el CLI de administración ni el de migraciones, ni ninguna suite que abra conexión.
- No se accedió al repositorio IDI.
- **No se modificó el candidato, su manifiesto ni ningún fichero bajo `.local/d2b-candidate/`**: todo el build, las pruebas y las mutaciones se hicieron sobre una copia en `%LOCALAPPDATA%\Temp\opencode\kdi-cr-d2b`. Verificación final del candidato: `freeze.py --verify` → `SHA256 7a057a2e8d0be09e0e2bb52d9864afa9baaeb139cf5badcbc03a02bb4e744c6f`, 196 archivos, exit 0 (idéntico al inicio de la revisión).
- No se delegó en ningún subagente.
