# D2-B — revisión independiente R2 del delta de credencial de operador

**Rol:** `code-reviewer` (perfil funcional de AGENTS.md). **agent_type efectivo del harness:** `general`.
**No se delegó.** No se modificó el candidato ni su manifiesto (verificado al inicio y al final).

- **Candidato revisado:** `E:\INSSOFT\KDI\.local\d2b-candidate`
- **candidateHash (SHA-256 del manifiesto):** `79d638649b2dbb338a8f9e8b37f7298add8f8454c3fbf07586e5ff807ae9ccb0` — **197 archivos**, `freeze.py --verify` → exit 0.
- **Candidato anterior (R1):** `7a057a2e8d0be09e0e2bb52d9864afa9baaeb139cf5badcbc03a02bb4e744c6f` — 196 archivos.
- **Delta verificado (propio):** 1 alta, 4 cambios, 0 bajas, 192 intactos.
- **Dictamen de esta ronda:** **el delta de código es correcto; H1, H2 y H4 cerrados, H3 parcial (solo cobertura).** Pasa a QA con dos tests adicionales recomendados (no bloqueantes). **D2-B sigue OPEN/BLOCKED: no hay PASS global.**
- **Informe anterior (no modificado):** `docs/stage3/d2b/operator-credential-fix-20261021/CODE-REVIEW.md`.

---

## 1. Método y evidencia propia

| Comprobación | Cómo | Resultado |
|---|---|---|
| Integridad del candidato (inicio y fin) | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` | `SHA256 79d638649b2dbb338a8f9e8b37f7298add8f8454c3fbf07586e5ff807ae9ccb0`, `197 files; verification OK`, exit 0 **antes y después** de la revisión |
| Delta real | `manifest-diff.py --old candidate-manifest.20261020-7a057a2e.json --new candidate-manifest.json` | 1 alta, 4 cambios, 0 bajas, 192 sin cambio — **coincide exactamente con lo declarado** |
| Aislamiento | Copia íntegra del candidato a `C:\Users\Israel\AppData\Local\Temp\opencode\kdi-cr-d2b-r2` + verificación SHA-256 de los 197 ficheros contra el manifiesto | **197/197 byte-idénticos** (0 faltan, 0 sobran, 0 discrepancias). Todo el build, las pruebas y las mutaciones se hicieron sobre copias, nunca sobre el candidato |
| Semántica del driver y del `Session` | App de sonda propia (`probe`, `AssemblyName=Kulani.KDI.UnitTests` para usar el `InternalsVisibleTo`), `Microsoft.Data.SqlClient 6.1.7`, **sin conexión** | Ver §3 |
| Detección de defectos por los tests | 10 mutaciones sobre el fichero `OperatorConnection.cs` **en copia** (`kdi-cr-d2b-r2-mut`), reconstrucción y ejecución filtrada de `OperatorCredentialTests` | Ver §6 |
| Regresión | `dotnet build Kulani.KDI.slnx -c Release --no-restore` + las tres suites sin SQL | Ver §7 |
| `src/Kulani.KDI.Api/**` sin cambios | Comparación de hashes de los dos manifiestos, fichero a fichero | **10/10 ficheros idénticos**, incluido `Program.cs` = `400f4685…` — ver §5 |

Restauración usada: `dotnet restore --locked-mode` **offline** (todas las paquetes presentes en `E:\INSSOFT\KDI\.local\nuget\packages`, 109 paquetes; exit 0, sin red). A partir de ahí, todo con `--no-restore`.

---

## 2. Delta verificado fichero a fichero

| Fichero | Cambio | Hash (pre → post) |
|---|---|---|
| `src/Kulani.KDI.Infrastructure/Properties/AssemblyInfo.cs` | **NUEVO** — `[assembly: InternalsVisibleTo("Kulani.KDI.UnitTests")]` | — |
| `src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs` | MODIFICADO — `Session`, seams, sobrecarga `internal` | `b2a445c7…` → `ae6e3139…` |
| `tests/Kulani.KDI.UnitTests/OperatorCredentialTests.cs` | MODIFICADO — 4 → **12** tests | `275c4be1…` → `0fed0eaf…` |
| `tools/Kulani.KDI.AccessAdmin/Program.cs` | MODIFICADO — `await using var session` | `fd92e6aa…` → `0e71cd81…` |
| `tools/Kulani.KDI.Migrations/Program.cs` | MODIFICADO — `await using var session` | `d46a4541…` → `a7f6d46f…` |

192 intactos, 0 bajas. **Ningún otro fichero del candidato ha cambiado.**

---

## 3. Hechos medidos por la sonda propia (independientes del implementador)

App `probe` (referencia al `Kulani.KDI.Infrastructure` **de la copia**, `Microsoft.Data.SqlClient 6.1.7`, sin abrir conexión):

```
SqlConnection is sealed                                        True
SqlCredential implements IDisposable                           False
SqlConnection.Credential is public                             True
SqlCredential.Password is public                               True
copy.IsReadOnly (Copy() no copia el flag)                      False
SqlCredential(writable copy)                                   ArgumentException: password debe marcarse como de solo lectura.
SqlCredential guarda la MISMA referencia                       True
cred.Password.Length tras conn.Dispose()                       25        <- SqlConnection.Dispose() NO destruye el SecureString
reachable.Length tras session.Dispose()                        ObjectDisposedException -> DETERMINISTA OK
reachable2.Length tras DisposeAsync()                         ObjectDisposedException -> DETERMINISTA OK
Dispose() posterior a DisposeAsync()                           OK (idempotente)
public static Session Create(String)                           publico
Create(string, SecureString) es internal                       True
tipo anidado Session   IsNestedPublic=True IsSealed=True
Session.Connection / Session.Dispose / Session.DisposeAsync   public
PasswordPrompt IsAssembly (internal)                          True
ConsoleRedirected() (efectivo)                                 True
Create(TestDatabase) publico con consola redirigida            InvalidOperationException: A private interactive console is required.
credencial Password IsReadOnly                                 True
ReferenceEquals(entrada, copia)                                False
connection string (sin UserID/Password embebidos)              Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Encrypt=True;TrustServerCertificate=True;Persist Security Info=False
```

**Conclusión:** las tres justificaciones del implementador son **empíricamente correctas**: `SqlConnection` es *sealed* (no se puede heredar para colgar el `Dispose`), `SqlCredential` no es `IDisposable` y **guarda la referencia** del `SecureString`, y `SqlConnection.Dispose()` **no** destruye el `SecureString`. El diseño de `Session` es, por tanto, la respuesta técnicamente correcta al problema, no un apaño.

---

## 4. Veredicto por hallazgo previo (H1–H4)

### H1 — MEDIA — Superficie pública relajada → **CERRADO**

- `OperatorConnection.cs:42` — la **única** entrada pública es `public static Session Create(string database)`.
- `OperatorConnection.cs:66` — la sobrecarga que acepta el `SecureString` del llamador pasa a **`internal`**.
- Sonda: `public static Session Create(String)` es la única pública; `Create(string, SecureString) es internal = True`.
- Test de reflexión (`OperatorCredentialTests.cs:155-167`): `Assert.Single(publicCreates)`, `Assert.Equal(typeof(OperatorConnection.Session), publicCreates[0].ReturnType)`, y `Assert.True(credentialOverloads[0].IsAssembly)` / `Assert.False(IsPublic)`.
- **Mutación M1** (volver a `public` la sobrecarga de credencial) → **DETECTADA** por `Operator_public_surface_exposes_only_the_interactive_entry_point` (12 tests → 11 ok / 1 error).
- La guarda de consola **sigue vigente en la ruta pública**: sonda `Create(TestDatabase)` con consola redirigida → `InvalidOperationException: A private interactive console is required.`.

### H2 — MEDIA — Copia huérfana sin destrucción determinista → **CERRADO**

- `OperatorConnection.cs:16-40` — `OperatorConnection.Session` es el propietario explícito de la copia; `Dispose()` (29-33) y `DisposeAsync()` (35-39) la destruyen en un `finally`, de modo que **ambas rutas** la destruyen incluso si `SqlConnection.Dispose()` lanzara.
- Sonda: `ObjectDisposedException` en **ambos** caminos tras el `Dispose`; doble `Dispose` idempotente.
- Tests `Operator_session_destroys_the_credential_password_when_disposed` y `..._disposed_asynchronously` (`:87-107`), que además comprueban que la instancia alcanzable por `session.Connection.Credential.Password` **es la misma** que el `Session` destruye (si no, la copia seguiría viva y el test no fallaría).
- **Mutaciones M2a** (sin `_password.Dispose()` en `Dispose`) y **M2b** (sin `_password.Dispose()` en `DisposeAsync`) → **DETECTADAS** por el test correspondiente (1 error cada una).
- **¿Queda algún `SecureString` con la contraseña que dependa del GC?**
  - Buffer tecleado (camino interactivo): destruido por `using var password` (`OperatorConnection.cs:50`) al salir del método, incluidos los caminos de error. **Determinista.** (Ver N2: no está *asertado* por ningún test.)
  - Copia entregada al `SqlCredential`: propiedad del `Session`, destruida en `Dispose`/`DisposeAsync`. **Determinista.**
  - `SqlCredential` conserva una referencia a la copia **ya destruida** → no queda material vivo.
  - Rama de error de la construcción (`:86-90`): `catch { credentialPassword.Dispose(); throw; }` → sin residuo sensible.
  - **En el camino de producto no queda ninguna dependencia del GC.** Única salvedad: si un consumidor futuro no destruyese el `Session`, la copia quedaría a la espera del finalizador de `SecureString` (ver N6).
- **¿La superficie pública expone material de contraseña por alguna vía?** No en texto plano. Ver **N4**: el objeto `SecureString` **sí** es alcanzable por `session.Connection.Credential.Password` porque `SqlConnection.Credential` es público — pero eso ya era cierto antes del cambio (`Create` devolvía el `SqlConnection` directamente), y no permite leer el texto plano sin `Marshal.SecureStringToGlobalAllocUnicode`. No es una regresión.

### H3 — BAJA — Camino de producto no testeado → **PARCIAL (cerrado en código, con huecos de cobertura)**

Lo que **sí** está hecho y verificado:

- Se extrajeron dos seams `internal`: el estado de consola (`Func<bool> consoleRedirected`) y la lectura de teclado (`Func<ConsoleKeyInfo> readKey`), en `OperatorConnection.cs:46`.
- **Prompt y orden de comprobaciones: verificados por cuentas propias** (no por la afirmación del implementador). Contrastando la numeración de líneas que mi informe R1 dejó registrada para el fichero original (guarda en la **línea 13**, `using var password` en la **14**, `"Credential required."` en la **24**) con la secuencia actual de sentencias (48→61), la reconstrucción del original queda **exactamente** determinada y coincide sentencia a sentencia con la actual:

| Orden | Original (7a057a2e, reconstruido desde las citas de línea de mi informe R1) | Actual (líneas 48-61) | ¿Idéntico? |
|---|---|---|---|
| 1 | `if (database is not (Dev or Test)) throw new ArgumentException("Unauthorized database.");` (línea 12) | idem (48) | ✔ |
| 2 | `if (Console.IsInputRedirected \|\| Console.IsOutputRedirected) throw new InvalidOperationException("A private interactive console is required.");` (línea 13) | `if (consoleRedirected())` → `ConsoleRedirected()` = misma expresión (49, 93) | ✔ |
| 3 | `using var password = new SecureString();` (línea 14) | idem (50) | ✔ |
| 4 | `Console.Write("kdi_migrator password (protected input): ");` (línea 15) | `Console.Write(PasswordPrompt);` con la **misma cadena literal** (51, 10) | ✔ |
| 5 | bucle `while (true)` con `Console.ReadKey(intercept: true)`, `Enter`, `Backspace` (`RemoveAt`), `!char.IsControl` (líneas 16-22) | idem con `readKey()` → `ReadKey()` → `Console.ReadKey(intercept: true)` (52-58, 95) | ✔ |
| 6 | `Console.WriteLine(); password.MakeReadOnly();` (línea 23) | idem (59) | ✔ |
| 7 | `if (password.Length == 0) throw new InvalidOperationException("Credential required.");` (línea 24) | idem (60) | ✔ |
| 8 | `return Create(database, password);` (línea 25) | idem (61) | ✔ |

  Los **textos literales** citados literalmente en mi informe R1 (`"Unauthorized database."`, `"A private interactive console is required."`, `"Credential required."`) son **idénticos** en el fichero actual. El prompt pasa de literal en línea a `internal const` con **el mismo valor**; el comportamiento de `Console.Write` es idéntico (sin formateo). La numeración 13/14/24 encaja de forma exacta con la reconstrucción, lo que confirma que no se ha movido ninguna comprobación.

- **Mutaciones que demuestran que el prompt y el flujo se detectan:**
  - **M3a** (eliminar la guarda de consola) → **DETECTADA** por `Operator_console_guard_refuses_a_redirected_console`.
  - **M3b** (eliminar el tratamiento de `Backspace`) → **DETECTADA** por `Operator_protected_read_keeps_the_prompt_and_the_key_flow`.
  - **M5** (cambiar el texto del prompt) → **DETECTADA** por `Operator_protected_read_keeps_the_prompt_and_the_key_flow`.
  - **M6** (invertir el orden de las comprobaciones: guarda antes que autorización de base de datos) → **DETECTADA** por `Operator_console_guard_still_rejects_an_unauthorized_database_first`.
  - El test de la guarda usa `() => throw new InvalidOperationException("The keyboard must not be read.")` como `readKey`: si la guarda desapareciera, la excepción sería otra y el mensaje no coincidiría. La aserción discrimina de verdad.

Lo que **queda abierto** (es lo que impide marcar H3 como cerrado):

- **N1 — el punto de entrada público `Create(string)` no lo ejercita ningún test.** El seam interno sí, pero la ruta que usan los CLI no. **Mutación M9** (cambiar `Create(string database) => Create(database, ConsoleRedirected, ReadKey)` por `=> Create(database, () => false, ReadKey)`, es decir, **eliminar la guarda de consola del camino público**) → **NO DETECTADA**: 12/12 tests siguen en verde. El código está correcto hoy (verificado por sonda), pero la protección más importante del fichero —"la contraseña la teclea una persona en una consola privada"— **no está protegida por ningún test**.
- **N2 — la destrucción determinista del buffer tecleado no está asertada.** **Mutación M8** (quitar el `using` de `using var password = new SecureString();`, dejando que el buffer tecleado dependa del GC) → **NO DETECTADA**: 12/12 en verde. El código es correcto, pero nada lo fija.
- **N3 — el cableado de los CLI sigue sin cobertura.** No existe ningún test que invoque `AccessAdmin`/`Migrations` en modo no interactivo y compruebe que fallan con `"A private interactive console is required."` sin tocar SQL (recomendación (iii) de mi informe R1, no ejecutada). Agrava N1: el hueco que dejó pasar el defecto original era de **integración de CLI**, no unitario.

### H4 — BAJA — Buffer vacío aceptado → **CERRADO**

- `OperatorConnection.cs:70` — `if (password.Length == 0) throw new InvalidOperationException("Credential required.");` en la sobrecarga de credencial. El mensaje coincide con el del camino interactivo (línea 60): ya no hay asimetría entre las dos rutas.
- Test `Operator_empty_credential_buffer_is_rejected` (`:146-153`).
- **Mutación M4** (eliminar el rechazo del buffer vacío) → **DETECTADA** por ese test.

### H5 — BAJA — Documentación de D2-B desactualizada → **SIGUE ABIERTO** (ver N7)

---

## 5. Contrato de retorno: `Create(string)` → `Session`

**Impacto real medido:**

- **Consumidores en el repositorio: exactamente dos** (`grep` de `OperatorConnection` en todo el candidato): `tools/Kulani.KDI.AccessAdmin/Program.cs:34` y `tools/Kulani.KDI.Migrations/Program.cs:37`. Ambos actualizados a `await using var session = OperatorConnection.Create(...); var connection = session.Connection;`. Ningún otro fichero del candidato lo usa.
- **La API no lo consume** (`src/Kulani.KDI.Api/**` byte-idéntico, `Program.cs` = `400f4685…`); la credencial de la API (`kdi_app`) sigue por su propio camino (`Program.cs:109-134`), intacto.
- **No es un paquete NuGet distribuible**: `src/Kulani.KDI.Infrastructure/Kulani.KDI.Infrastructure.csproj` no declara `IsPackable`, `PackageId` ni `dotnet pack`. El cambio de tipo de retorno es, por tanto, **interno al producto**, no una ruptura de un artefacto publicado.
- **Coherencia con el resto del diseño:** la forma `Session`/`await using` es la que hace **expresable** la propiedad del material sensible. Devolver `SqlConnection` (como antes) obligaba a que **nadie** destruyera la copia; el problema de H2 era *precisamente* que la firma no tenía dónde expressar la propiedad. El cambio resuelve la causa, no un síntoma.
- **Riesgos medidos y no severos:**
  1. *Cierre explícito de la conexión en los CLI.* Antes la `SqlConnection` no se cerraba nunca (vivía hasta el fin del proceso); ahora se cierra al salir del ámbito. **Es una mejora.** El orden de destrucción es correcto en ambos CLI: en `AccessAdmin` las declaraciones son `session` → `i` → `o` → `tx`, luego el `await using` libera en orden inverso (`tx` → `o` → `i` → `session`); en `Migrations` es `session` → `context` (`await using (context)`), luego `context` → `session`. EF Core no cierra una `DbConnection` externa al disposing del `DbContext`, de modo que no hay conflicto.
  2. *Obligación de `using` para los consumidores futuros.* `Session` no tiene finalizador de reserva (N6) y no protege frente a un uso sin `Dispose`. Hoy no hay tal consumidor.
  3. *Superficie pública creciente.* `OperatorConnection.Session` es un **tipo anidado público** (`IsNestedPublic=True`), con `Connection` público. La superficie de `Kulani.KDI.Infrastructure` gana un tipo, pero **no pierde ninguna capacidad**: `Create` ya devolvía un `SqlConnection` y `Session.Connection` sigue devolviéndolo. No es una relajación de control.
  4. *Acoplamiento a `SqlConnection.Credential` en los tests.* Es API pública y estable del driver; la alternativa (reflexión sobre internos) sería peor. **Aceptable.**

**Veredicto sobre el cambio de contrato:** **coherente y justificado.** Es la forma mínima que permite cerrar H2 sin tocar `.csproj` ni perder funcionalidad. No introduce un riesgo que el implementador no haya medido, salvo el residual de N6 (ausencia de finalizador de reserva) y la nueva obligación de `await using` para futuros consumidores.

---

## 6. Los seams `internal` y el `InternalsVisibleTo`

- **¿Relajan algo?** `InternalsVisibleTo("Kulani.KDI.UnitTests")` abre **todos** los internos de `Kulani.KDI.Infrastructure` al ensamblado de tests, no solo a los seams de credencial. Los internos de esa librería son: `OperatorConnection.PasswordPrompt`, las dos sobrecargas `internal` de `Create`, el ctor `internal` de `Session` y `ConsoleRedirected`/`ReadKey` privados. Ninguno contiene capacidad de producto: **la superficie pública de la librería no cambia por causa del atributo**, y la guarda de consola sigue siendo la misma para todo el mundo. No hay firma fuerte (`Directory.Build.props` no define `SignAssembly`), por lo que **no hizo falta tocar ningún `.csproj`** — verificado: el build de la solución es **0 advertencias / 0 errores**.
- **¿Puede el seam de lectura de teclado cambiar el comportamiento observable?** **No.** La costura sustituye *solo* la fuente de teclas (`readKey`) y la fuente del estado de consola (`consoleRedirected`); el prompt, el bucle, el tratamiento de `Backspace`, el `MakeReadOnly`, el `WriteLine` final, los mensajes y el **orden** de las comprobaciones se mantienen idénticos (tabla del §4). La ruta pública (`Create(string)`) usa `ConsoleRedirected` y `ReadKey`, que **son exactamente** `Console.IsInputRedirected || Console.IsOutputRedirected` y `Console.ReadKey(intercept: true)`: el comportamiento por defecto es bit a bit el de antes.
- **Riesgo residual del `InternalsVisibleTo` (N5):** la única puerta es el **nombre simple** del ensamblado. Lo comprobé empíricamente: mi sonda se compiló con `AssemblyName=Kulani.KDI.UnitTests` y **pudo llamar a la sobrecarga interna de credencial**, es decir, **construir una credencial de `kdi_migrator` saltándose la guarda de consola**. Práctica habitual y riesgo práctico bajo (cargar un ensamblado con ese nombre en el proceso del operador ya implica ejecución de código), pero el comentario del fichero (`AssemblyInfo.cs:3`, «so that only the unit test project can exercise them») **sobrevalora** la garantía.

---

## 7. Calidad y suficiencia de los tests

`tests/Kulani.KDI.UnitTests/OperatorCredentialTests.cs` — **12 tests** (antes 4).

| Criterio | Veredicto | Evidencia |
|---|---|---|
| ¿Detecta los defectos? | **Sí, para todos los defectos declarados** | 10 mutaciones; §8 |
| ¿Aserciones tautológicas? | **No** | Las 4 aserciones del test de superficie (`:158-166`) son estructurales y fallan ante cualquier cambio de visibilidad; las de credencial comprueban `NotSame`, `IsReadOnly`, longitud, `UserId` y `ConnectionString` |
| ¿Depende de APIs del driver que podrían cambiar? | **Riesgo bajo** | `SqlConnection.Credential` y `SqlCredential.Password` son API **públicas y estables** de `Microsoft.Data.SqlClient` (6.1.7, la versión fijada en `packages.lock.json`); `SqlCredential.Password` es además imprescindible para asertar la invariante de solo lectura |
| ¿Contiene credenciales reales? | **No** | Solo `"synthetic-operator-secret"` y `"forged"` (sonda) |
| ¿Abre conexión o red? | **No** | `SqlConnection` se construye pero nunca se abre (`Assert.Equal(ConnectionState.Closed, connection.State)`); `SqlConnectionStringBuilder` es local; ninguna suite abierta |
| ¿Aislamiento de estado global? | **Adecuado** | `Capture` sustituye `Console.Out`; se mitiga con `[CollectionDefinition("KdiOperatorConsole", DisableParallelization = true)]` (xUnit v3 ejecuta esa colección aislada). La ejecución real fue limpia: 69/69, 0 omitidas |
| ¿Suficiente? | **Casi** | Cierra H3 en lo que se refiere a *contenido*, pero deja **N1** (punto de entrada público) y **N2** (buffer tecleado) sin aserción, y **N3** (cableado de CLI) sigue sin cobertura |

### Tabla de mutaciones (hecha sobre copia, nunca sobre el candidato)

| # | Mutación | Resultado | Test que la detecta |
|---|---|---|---|
| M1 | Sobrecarga de credencial de nuevo `public` (regresión de H1) | **DETECTADA** (1 error) | `Operator_public_surface_exposes_only_the_interactive_entry_point` |
| M2a | `Session.Dispose` ya no destruye el `SecureString` (regresión de H2) | **DETECTADA** (1 error) | `Operator_session_destroys_the_credential_password_when_disposed` |
| M2b | `Session.DisposeAsync` ya no destruye el `SecureString` (regresión de H2) | **DETECTADA** (1 error) | `Operator_session_destroys_the_credential_password_when_disposed_asynchronously` |
| M3a | Eliminada la guarda de consola (regresión de H3) | **DETECTADA** (1 error) | `Operator_console_guard_refuses_a_redirected_console` |
| M3b | Eliminado el tratamiento de `Backspace` (regresión de H3) | **DETECTADA** (1 error) | `Operator_protected_read_keeps_the_prompt_and_the_key_flow` |
| M4 | Buffer vacío ya no se rechaza (regresión de H4) | **DETECTADA** (1 error) | `Operator_empty_credential_buffer_is_rejected` |
| M5 | Texto del prompt cambiado | **DETECTADA** (1 error) | `Operator_protected_read_keeps_the_prompt_and_the_key_flow` |
| M6 | Orden de comprobaciones invertido (guarda antes que autorización de BD) | **DETECTADA** (1 error) | `Operator_console_guard_still_rejects_an_unauthorized_database_first` |
| M7 | `MakeReadOnly()` sobre la copia eliminado (causa raíz de R1) | **DETECTADA** (6 errores) | los 6 tests que construyen la credencial |
| **M8** | El buffer tecleado deja de destruirse (`using` eliminado) | **NO DETECTADA** (12/12 verdes) | — → **N2** |
| **M9** | El punto de entrada **público** se salta la guarda de consola | **NO DETECTADA** (12/12 verdes) | — → **N1** |

Las cuatro mutaciones que el implementador declaraba (M1, M2, M3a, M3b, M4) están **confirmadas de forma independiente**. Añadí seis más, dos de ellas detectadas y dos **no detectadas** (N1, N2).

**Restauración:** el fichero mutado se restauró a su contenido original (idéntico al del candidato salvo terminación de línea, que Python normalizó a CRLF al escribir; **igualdad de contenido normalizado verificada = True**), se reconstruyó y la suite completa volvió a **69/69**.

---

## 8. Hallazgos nuevos por severidad

### N1 — BAJA — El punto de entrada público `Create(string)` no está cubierto por ningún test
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:42`

**Reproducción:** en la copia de mutación, sustituir
`public static Session Create(string database) => Create(database, ConsoleRedirected, ReadKey);`
por
`public static Session Create(string database) => Create(database, () => false, ReadKey);`
→ `dotnet build` OK y `OperatorCredentialTests` **12/12 correctas, 0 errores**. La guarda de consola —el control de seguridad central del fichero— puede desaparecer del camino público sin que ningún test lo note.

**Evaluación:** el código **es correcto hoy** (verificado por sonda: `Create(TestDatabase)` con consola redirigida lanza `"A private interactive console is required."`). El hallazgo es de **cobertura**, no de defecto funcional.

**Corrección propuesta (≈8 líneas, no bloqueante):**
```csharp
[Fact]
public void Operator_public_entry_point_applies_the_console_guard()
{
    if (!Console.IsInputRedirected && !Console.IsOutputRedirected) return; // sin consola redirigida que guardar
    var error = Assert.Throws<InvalidOperationException>(() => OperatorConnection.Create(SqlTarget.TestDatabase));
    Assert.Equal("A private interactive console is required.", error.Message);
}
```
(En el host de tests `Console.IsInputRedirected` e `IsOutputRedirected` son `True` —medido en la sonda—, por lo que la guarda es determinista; y si algún día no lo fueran, el test se autoexcluye en lugar de leer el teclado real.)

### N2 — BAJA — La destrucción determinista del buffer tecleado no está asertada
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:50`

**Reproducción:** sustituir `using var password = new SecureString();` por `var password = new SecureString();` → 12/12 tests verdes. El buffer tecleado (el material que el operador teclea) pasa a depender del GC sin que nada lo detecte. Contrasta con la copia, que sí está asertada (`Operator_credential_copy_survives_the_disposal_of_the_typed_buffer`).

**Corrección propuesta:** exponer el buffer del camino interactivo para asertar su destrucción tras `Create` (p. ej. un seam `internal` adicional que devuelva el buffer tecleado, o una aserción sobre `SecureString` liberado) — o, alternativamente, documentar explícitamente en el test que la invariante la cubre laера `using` del propio método.

### N3 — BAJA — El cableado de los CLI sigue sin cobertura (arrastre de H3-iii)
`tools/Kulani.KDI.AccessAdmin/Program.cs:34`, `tools/Kulani.KDI.Migrations/Program.cs:37`

No existe ningún test que ejercite los CLI en modo no interactivo. Fue precisamente el hueco de **integración de CLI** (y no el unitario) el que dejó pasar el defecto original (así lo documenté en R1 §6: A3 se aplicó con `sqlcmd`/SSMS, y `AccessAdmin` nunca se ejecutó). N1 agrava este punto: la ruta exacta que usan los CLI no está asertada por ninguna prueba.

### N4 — INFORMATIVA — El material sigue siendo alcanzable por la superficie pública; el comentario es inexacto
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:15`

El comentario afirma «The material is never exposed: only the connection is public». Medido: `SqlConnection.Credential` es **público** y `SqlCredential.Password` es **público**, de modo que `session.Connection.Credential.Password` devuelve el `SecureString` vivo (sonda: `reachable.Length = 25` antes del `Dispose`). No se obtiene el texto plano sin `Marshal.SecureStringToGlobalAllocUnicode`, y **no es una regresión** (antes `Create` devolvía el `SqlConnection` directamente, con la misma propiedad). Pero el comentario debe ajustarse: el objeto `SecureString` es alcanzable y, por tanto, un tercero podría **disponerlo prematuramente** (rompiendo el inicio de sesión) o conservar la referencia. Recomendación: reescribir el comentario como «el texto plano nunca se expone; el `SecureString` permanece accesible a través de `SqlConnection.Credential`, como antes del cambio».

### N5 — INFORMATIVA — `InternalsVisibleTo` sin firma fuerte: la puerta es el nombre simple
`src/Kulani.KDI.Infrastructure/Properties/AssemblyInfo.cs:5`

Comprobado empíricamente: un ensamblado compilado con `AssemblyName=Kulani.KDI.UnitTests` (mi sonda) accede a los internos de `Kulani.KDI.Infrastructure` y **construye una credencial de `kdi_migrator` sin la guarda de consola**. Práctica habitual y riesgo práctico bajo, pero el comentario «so that only the unit test project can exercise them» sobrevalora la garantía. Sin acción requerida; si se quisiera endurecer, la opción sería firmar ambos ensamblados con la misma clave.

### N6 — INFORMATIVA — `Session` no tiene finalizador de reserva
`src/Kulani.KDI.Infrastructure/Access/OperatorConnection.cs:16`

Si un consumidor futuro no destruyese el `Session`, la copia del `SecureString` sobreviviría hasta el finalizador de GC. Hoy los dos únicos consumidores usan `await using`, así que **no hay dependencia del GC en el camino de producto**. Un finalizador de reserva en `Session` (o una nota en el comentario) convertiría el «debería» en un «puede».

### N7 — BAJA — Documentación de D2-B desactualizada (H5 de R1, sin cerrar)
- `docs/stage3/d2b/STATUS.md:9` sigue declarando `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb`, **195 archivos** y **«57 unitarias»** → lo vigente es `79d638649b2dbb338a8f9e8b37f7298add8f8454c3fbf07586e5ff807ae9ccb0`, **197 archivos** y **69 unitarias**.
- `docs/stage3/d2b/preparation-20261020/RESULT.md` sigue declarando «190 archivos; SHA-256 `1ab77cd8…`» y «UnitTests 57/57».
- No son parte del candidato (congelado) y **no afectan al hash**, pero el paquete de evidencia de D2-B debe reflejar el hash y los conteos vigentes antes del cierre.

### N8 — INFORMATIVA — H6 de R1 sigue abierto
`tools/Kulani.KDI.Migrations/Program.cs` no exige Windows (`AccessAdmin/Program.cs:10` sí). Fuera de Windows `SecureString` no cifra su contenido. Impacto bajo (instancia local fija, el material no sale del proceso), no verificado por este rol.

---

## 9. Regresión (sobre copia byte-idéntica al candidato, 197/197 SHA-256 verificados)

`dotnet build Kulani.KDI.slnx -c Release --no-restore` → **Compilación correcta. 0 advertencias, 0 errores** (`TreatWarningsAsErrors=true`).

| Suite | Resultado | Omitidas |
|---|---|---|
| `tests/Kulani.KDI.UnitTests` | **69/69 correctas**, 0 fallidas (antes 61 → +8; los 12 de `OperatorCredentialTests` son +8) | 0 |
| `tests/Kulani.KDI.ApiTests` | **20/20 correctas**, 0 fallidas | 0 |
| `tests/Kulani.KDI.ArchitectureTests` | **24/24 correctas**, 0 fallidas | 0 |

- `tests/Kulani.KDI.SqlIntegrationTests`: **NO ejecutado** (prohibido).
- CLI de administración y CLI de migraciones: **NO ejecutados** (prohibido).
- `src/Kulani.KDI.Api/**`: **10/10 ficheros byte-idénticos** respecto de `7a057a2e…` (`Program.cs` = `400f4685…`), incluido en el build de la solución.
- Integridad del candidato al terminar: `freeze.py --verify` → `SHA256 79d638649b2dbb338a8f9e8b37f7298add8f8454c3fbf07586e5ff807ae9ccb0`, `197 files; verification OK`, exit 0.

**Nota sobre `ArchitectureTests`:** sus 24 reglas **no cubren** nada del camino de operador (ninguna menciona `Operator`, `SecureString`, `Credential`, `Console` ni `Password`). La garantía de este delta descansa **enteramente** en `OperatorCredentialTests`.

---

## 10. Recomendación

**El delta pasa a QA.** No requiere otra ronda de revisión de código: los cuatro hallazgos previos están **cerrados en el código** (H1, H2, H4) o **cerrados en el comportamiento con un residuo de cobertura** (H3), la corrección es coherente con el diseño, no hay regresión, y la API no se toca.

Condiciones **no bloqueantes** pero **recomendadas antes del cierre de D2-B** (≈15-20 líneas de test, sin cambios de producción):

1. **N1** — test del punto de entrada público con la guarda de consola (cierra el hueco de M9).
2. **N2** — aserción de la destrucción determinista del buffer tecleado (cierra el hueco de M8).
3. **N7** — actualizar `STATUS.md` y `preparation-20261020/RESULT.md` con el hash `79d63864…`, 197 archivos y 69 unitarias.
4. **N4** — corregir el comentario de `OperatorConnection.cs:15` (inexacto, no defectuoso).
5. **N3** — si el Human Orchestrator lo autoriza, un test de integración de CLI en modo no interactivo (requiere decidir si es admisible "ejecutar" el CLI en modo no interactivo, que en esta sesión está prohibido).

**D2-B sigue OPEN/BLOCKED.** Este dictamen no es un PASS global: SQL conectado, Auth0 y HTTPS real permanecen bloqueados, y las pruebas de las categorías 4 y 5 del plan controlado no se han ejecutado.

---

## 11. Qué NO se pudo verificar y qué NO se hizo

### No verificado
- **Autenticación efectiva de `kdi_migrator`** con la credencial construida por la ruta corregida, y ejecución real de `AccessAdmin plan/apply` y `Migrations --apply`: prohibiciones explícitas de este rol (no abrir conexiones a SQL Server, no ejecutar los CLI) y ausencia de autorización humana y de consola privada interactiva. **No se declara FAIL**: la construcción de la credencial sí está verificada sin SQL.
- **Igualdad byte a byte del prompt con la versión previa (`7a057a2e…`)**: el contenido de esa versión **no se conserva** en disco (solo su hash en el manifiesto archivado) y ningún artefacto archivado (logs, matrices, `evidence/A1`) contiene el literal. Lo verifiqué por otras dos vías: (a) la numeración de líneas registrada en mi informe R1 (13, 14, 24) **encaja exactamente** con la secuencia de sentencias actual (§4), y (b) los tres literales citados textualmente en aquel informe son idénticos en el fichero actual. No es una comparación byte a byte.
- **Que disponer del `SecureString` antes de `OpenAsync` rompa o no el inicio de sesión**: requeriría abrir una conexión.
- **`SecureString` en sistemas no Windows** (cifrado) — no ejercitado; H6 (N8).
- **Comportamiento con una versión de `Microsoft.Data.SqlClient` distinta de 6.1.7**: solo se verificó con la versión fijada en `packages.lock.json`.
- **Paralelismo real de la suite bajo `Console.SetOut`**: el diseño de xUnit v3 (`DisableParallelization` en la colección) y la ejecución limpia 69/69 dan confianza, pero no medí una ejecución concurrente con otro test que escriba en consola.
- **`Kulani.KDI.SqlIntegrationTests`**, el CLI de administración y el de migraciones: no ejecutados.

### No hecho
- **No se modificó el candidato, su manifiesto ni ningún fichero bajo `.local/d2b-candidate/`**: todo el build, las pruebas y las mutaciones se hicieron sobre copias en `C:\Users\Israel\AppData\Local\Temp\opencode\` (`kdi-cr-d2b-r2`, `kdi-cr-d2b-r2-mut`, `probe`). Verificación final del candidato: `freeze.py --verify` → `79d638649b2dbb338a8f9e8b37f7298add8f8454c3fbf07586e5ff807ae9ccb0`, 197 archivos, exit 0 (idéntico al inicio de la revisión). La copia de trabajo se verificó **197/197 byte-idéntica** al manifiesto tras todas las mutaciones.
- **No se modificaron** `docs/stage3/d2b/operator-credential-fix-20261021/CODE-REVIEW.md` ni ningún otro documento previo; este informe es un fichero nuevo.
- **No se delegó** en ningún subagente (rol `code-reviewer`, agent_type efectivo `general`).
- **No se accedió** al repositorio IDI.
- **No se ejecutó** ninguna suite que abra conexión a SQL Server.
