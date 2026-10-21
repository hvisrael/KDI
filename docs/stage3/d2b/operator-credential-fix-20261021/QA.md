# QA — D2-B, ronda 3 de corrección de la ruta de credencial del operador

**Rol funcional:** `qa-engineer` (AGENTS.md). **agent_type efectivo del harness:** `general` — los perfiles de Codex no son invocables en este entorno; se reporta el tipo real y el rol que ejerce. **No se delegó.**

**QA independiente:** no he escrito el código de esta ronda y **no he modificado** el candidato, su manifiesto ni ningún fichero bajo `.local/d2b-candidate/`. Todas las mutaciones se hicieron sobre una **copia** en `%TEMP%`.

| Dato | Valor |
|---|---|
| Candidato | `E:\INSSOFT\KDI\.local\d2b-candidate` |
| Manifiesto | `docs/stage3/d2b/preparation-20261020/candidate-manifest.json` |
| **candidateHash (SHA-256 del manifiesto)** | **`91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448`** |
| Ficheros | **197** |
| Verificador | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` → exit 0 (antes y después) |
| **Veredicto global** | **BLOCKED.** No hay PASS global. **D2-B sigue OPEN/BLOCKED.** |

---

## 1. Tabla de comprobaciones

| # | Comprobación | Comando exacto | Exit | Resultado |
|---|---|---|---|---|
| 1 | Integridad del candidato, **al principio** | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` | **0** | `SHA256 91dce7d9…` / `197 files; verification OK` |
| 2 | Árbol de la raíz congelado, **al principio** | `python docs/stage3\d2a\closure\identities-20261018\verify.py` | **0** | `Original D2-A, frozen increment and frozen evidence verified` |
| 3 | Diff acumulado desde la línea base previa a las rondas | `python docs\stage3\d2b\preparation-20261020\manifest-diff.py --old docs/stage3/d2b/preparation-20261020/candidate-manifest.20261020-a1cbed38.json --new docs/stage3/d2b/preparation-20261020/candidate-manifest.json` | **0** | 184 sin cambio · **7 altas** · 0 bajas · **6 cambios** |
| 4 | Diff de la **ronda 3** (R2 → vigente) | `python …\manifest-diff.py --old docs/stage3/d2b/preparation-20261020/candidate-manifest.20261020-79d63864.json --new docs/stage3/d2b/preparation-20261020/candidate-manifest.json` | **0** | 195 sin cambio · 0 altas · 0 bajas · **2 cambios** (`OperatorConnection.cs`, `OperatorCredentialTests.cs`) |
| 5 | Build Release de la solución | `dotnet build Kulani.KDI.slnx -c Release` (en `.local\d2b-candidate`) | **0** | **0 errores, 0 advertencias** |
| 6 | Suite UnitTests | `dotnet test --project tests\Kulani.KDI.UnitTests\Kulani.KDI.UnitTests.csproj -c Release --no-build` | **0** | **71/71** (log: `qa/UnitTests.log`) |
| 7 | Suite ApiTests | `dotnet test --project tests\Kulani.KDI.ApiTests\Kulani.KDI.ApiTests.csproj -c Release --no-build` | **0** | **20/20** (`qa/ApiTests.log`) |
| 8 | Suite ArchitectureTests | `dotnet test --project tests\Kulani.KDI.ArchitectureTests\Kulani.KDI.ArchitectureTests.csproj -c Release --no-build` | **0** | **24/24** (`qa/ArchitectureTests.log`) |
| 9 | **Sonda independiente sobre el ensamblado publicado** (22 comprobaciones) | `dotnet run -c Release` en `C:\Users\Israel\AppData\Local\Temp\opencode\kdi-probe` | **0** | `PROBE OK: no failures` (`qa/operator-probe.log`) |
| 10 | **13 mutaciones** sobre la copia `%TEMP%` | `python C:\Users\Israel\AppData\Local\Temp\opencode\kdi-mut\mutate.py <id>` | 0/2 | tabla en §4 (`qa/mutations.log`) |
| 11 | Front: typecheck | `npm.cmd run typecheck` (Node 22.23.3, `npm_config_cache=E:\INSSOFT\KDI\.local\npm-cache`) | **0** | sin salida de error (`qa/web-typecheck.log`) |
| 12 | Front: lint | `npm.cmd run lint` | **0** | `--max-warnings 0` sin avisos (`qa/web-lint.log`) |
| 13 | Front: build | `npm.cmd run build` | **0** | `index-Do02OOjV.css`, `index-DY6Y8_x8.js` — **hashes idénticos a la referencia** (`qa/web-build.log`) |
| 14 | Front: navegador | `node.exe node_modules\@playwright\test\cli.js test --reporter=list` | **0** | **11/11 passed**, 0 fallos, 0 omitidas (`qa/browser.log`) |
| 15 | Verificación **offline** de artefactos SQL | `python docs\stage3\d2b\continuation-20261020\qa\verify_sql.py` | **0** | `OFFLINE VERIFIED: 14 hashes/byte counts; 12 exact reproductions; 4 rollback scopes/guards; 3 proposals without DELETE grant. No SQL executed.` |
| 16 | Integridad del candidato, **al final** | `python docs/stage3\d2b\preparation-20261020\freeze.py --verify` | **0** | `SHA256 91dce7d9…` / `197 files` — **hash idéntico al inicio** |
| 17 | Árbol de la raíz congelado, **al final** | `python docs/stage3\d2a\closure\identities-20261018\verify.py` | **0** | verificado, sin deriva |

**Escrituras dentro del candidato durante las pruebas:** solo `bin/`, `obj/`, `dist/`, `.local/e2e/` y `docs/stage3/d2b/evidence/browser-artifacts/`, **todas ya excluidas** por el verificador de congelación (`EXCLUDED` en `freeze.py` y la exclusión explícita de `docs/stage3/d2b/evidence/`). Confirmado por el verificador (comprobaciones 1 y 16), **no supuesto**.

---

## 2. Conteos reales de las tres suites

| Suite | Referencia previa (`continuation-20261020/qa/*-final.log`) | **Real de esta ronda** | Fallos | Omitidas | **Nuevas** |
|---|---|---|---|---|---|
| `Kulani.KDI.UnitTests` | 57 | **71** | 0 | 0 | **+14** |
| `Kulani.KDI.ApiTests` | 20 | **20** | 0 | 0 | **0** |
| `Kulani.KDI.ArchitectureTests` | 24 | **24** | 0 | 0 | **0** |
| Navegador (Playwright Chromium) | 11 | **11** | 0 | 0 | 0 (re-ejecutado) |

**Las 14 tests nuevas son exactamente las de `tests/Kulani.KDI.UnitTests/OperatorCredentialTests.cs`** (fichero nuevo, `Select-String '^\s*\[Fact\]'` → 14; 57 + 14 = 71). Ninguna suite de SQL se ejecutó: `Kulani.KDI.SqlIntegrationTests` **NO ejecutada** (prohibido).

Build Release: **0 advertencias, 0 errores** (`TreatWarningsAsErrors=true` en `Directory.Build.props`).

### Las tres rondas, según los manifiestos archivados (verificado por diff, no de oídas)

| Ronda | Manifiesto | Ficheros | Unitarias | Cambio estructural |
|---|---|---|---|---|
| Base previa | `a1cbed38` | 190 | 57 | — |
| R1 | `7a057a2e` | 196 | 61 | sobrecarga `public Create(string, SecureString)` + 4 tests |
| R2 | `79d63864` | 197 | 69 | `internal` + `InternalsVisibleTo`, `Session` propietario, seams de consola/teclado, cableado de los CLI, +12 tests |
| **R3 (vigente)** | **`91dce7d9`** | **197** | **71** | **+2 tests**: cierre de N1 (punto de entrada público con `ForceNotInteractive`) y N2 (destrucción del buffer tecleado con el observador `observeTypedBuffer`) |

---

## 3. Verificación independiente del defecto original

**Defecto original:** `OperatorConnection.Create` marcaba el `SecureString` original como de solo lectura y entregaba a `SqlCredential` una **copia que no lo era** → `ArgumentException: password debe marcarse como de solo lectura.` **siempre**.

### 3.1 Reproducción del modo de fallo (sin SQL)

Con mi propia prueba `Qa_SqlCredential_rejects_a_writable_copy_and_the_message_is_the_original_one` (copia `%TEMP%`):

```
Assert.Contains() Failure: Sub-string not found
String:    "password debe marcarse como de solo lectura."
```

Es decir: **el mensaje original es exactamente ese**, y se dispara cuando se entrega a `SqlCredential` un `SecureString` escribible. Esto identifica el defecto con precisión y no lo reinterpreta.

### 3.2 La ruta de producción **no** lanza (prueba sobre el ensamblado publicado)

Sonda propia `qa/operator-probe.log`, ejecutada contra el **Release ya compilado del candidato** (`src\Kulani.KDI.Infrastructure\bin\Release\net10.0\Kulani.KDI.Infrastructure.dll`, SHA-256 `9727a768b21be0d543b56ba2a873010e3f09eafbda42cc4f093b1aa6b162d5f8`; la copia cargada por la sonda tiene el **mismo** hash), por reflexión sobre la sobrecarga `internal`:

```
OK   credential construction does not throw
OK   SqlCredential built
OK   credential user id is kdi_migrator
OK   handed SecureString is read-only
OK   handed SecureString is not the caller buffer
OK   handed SecureString keeps the length :: 15
OK   caller buffer is still writable and alive
OK   connection state is Closed
OK   connection string target is KulaniKDI_Test :: KulaniKDI_Test
OK   connection string embeds no user id or password
OK   session.Dispose destroys the credential password
```

**Conclusión:** la credencial del camino de producción **se construye sin lanzar**, el `SecureString` entregado **es de solo lectura**, **no es** el buffer del llamador, y el estado de la conexión es `Closed` (**no se abrió ninguna conexión**). El buffer del llamador ni se marca ni se destruye: sigue escribible y vivo.

### 3.3 Cierre real del defecto por mutación (M3)

Quitar el `credentialPassword.MakeReadOnly();` — es decir, volver al estado defectuoso — hace fallar **11 tests** (8 del candidato + 3 míos). Los 8 del candidato que fallan son precisamente los que construyen la credencial. **La batería de tests habría atrapado el defecto original.**

---

## 4. Utilidad de los tests: los tests detectan regresiones (mutaciones)

Todas las mutaciones se aplicaron **solo** a la copia `C:\Users\Israel\AppData\Local\Temp\opencode\kdi-mut\base` (verificada byte a byte con el candidato: `OperatorConnection.cs` = `faa3506a…` en ambos antes y después) y se revirtieron tras cada una. La copia contenía **mi propia batería de 7 tests independientes** (`QaIndependentOperatorCredentialTests.cs`), de modo que la línea base de la copia es **78/78** (71 del candidato + 7 míos).

| # | Mutación | Resultado | Test(s) que la detecta |
|---|---|---|---|
| M1 | `Create(string, SecureString)` vuelve a ser **`public`** | **DETECTADA** (2 errores) | `Operator_public_surface_exposes_only_the_interactive_entry_point` + mi `Qa_Public_surface_exposes_only_the_interactive_entry_point` |
| M2 | `Session.Dispose()` ya **no** destruye el `SecureString` | **DETECTADA** (2) | `Operator_session_destroys_the_credential_password_when_disposed` + la mía |
| M3 | **Defecto original**: la copia no se marca de solo lectura | **DETECTADA (11)** | los 8 tests que construyen la credencial |
| M4 | El **buffer tecleado** deja de destruirse (`using` → `var`) | **DETECTADA (1)** | `Operator_typed_buffer_is_destroyed_when_the_protected_read_finishes` ← **cierre de N2** |
| M5 | Orden de comprobaciones invertido (consola antes que base de datos) | **DETECTADA (2)** | `Operator_console_guard_still_rejects_an_unauthorized_database_first` |
| M6 | La guarda de consola se abre siempre (`NotInteractive() => false`) | **DETECTADA (2)** | `Operator_public_entry_point_refuses_to_build_a_credential_without_a_private_console` |
| M7 | Texto del prompt cambiado | **DETECTADA (2)** | `Operator_protected_read_keeps_the_prompt_and_the_key_flow` |
| M8 | `DisposeAsync()` ya no destruye el `SecureString` | **DETECTADA (1)** | `Operator_session_destroys_the_credential_password_when_disposed_asynchronously` |
| M10 | Se elimina el rechazo de buffer vacío en el camino interactivo | **NO detectada (78/78)** | — **mutante equivalente**: la sobrecarga interna `Create(db, password)` rechaza también el buffer vacío con el **mismo** mensaje (`"Credential required."`). El comportamiento observable es idéntico. **No es un hueco de cobertura.** |
| M11 | La `Session` guarda el buffer del llamador en vez de la copia | **DETECTADA (3)** | los dos tests de destrucción + la mía |
| M12 | **El punto de entrada público se salta la guarda de consola** (`Create(string) => Create(database, () => false, ReadKey)`) | **DETECTADA (2)** | `Operator_public_entry_point_refuses_to_build_a_credential_without_a_private_console` ← **cierre de N1** (en R2 esta mutación **no** se detectaba) |
| M13 | El hook deja de estar conectado a la guarda (`_notInteractiveForced` fuera de `NotInteractive()`) | **NO detectada (78/78)** | — **mutante equivalente en este entorno**: la consola del host de tests está redirigida, así que la guarda real ya rechaza. Observación, no hueco funcional (§6) |
| M9b+M9 | El hook pasa a ser **bidireccional** (`ForceInteractive` desactiva la guarda) | **NO detectada (78/78)** | — **observación propia, no registrada en informes anteriores** (§6) |

**Lectura:** 11 de 13 mutaciones se detectan; las 2 no detectadas son (a) un mutante **equivalente** (M10), (b) un mutante **equivalente en este entorno** (M13) y (c) una vía **nueva y permisiva** (M9b+M9) que la batería actual no forbids. El resto —causa raíz, superficie pública, orden de comprobaciones, prompt, destrucción síncrona y asíncrona, buffer tecleado y guarda del punto de entrada público— **están fijados por aserciones que discriminan de verdad**.

Ningún test contiene credenciales reales (solo `"synthetic-operator-secret"` y `"qa-independent-secret"`), ninguno abre conexión (`ConnectionState.Closed` asertado) y ninguno usa red.

---

## 5. La API no se ha roto y el comportamiento observable se mantiene

### 5.1 `src/Kulani.KDI.Api/**` byte-idéntico — verificado por hash

Agregado SHA-256 de los 10 ficheros de `src/Kulani.KDI.Api/` **idéntico en los 8 manifiestos archivados**, incluida la línea base previa a las rondas:

| Manifiesto | Ficheros | API | Agregado API |
|---|---|---|---|
| `a1cbed38` (base) | 190 | 10 | `4f01cd22f4d29207…` |
| `7a057a2e` (R1) | 196 | 10 | `4f01cd22f4d29207…` |
| `79d63864` (R2) | 197 | 10 | `4f01cd22f4d29207…` |
| **`candidate-manifest.json` (R3)** | **197** | **10** | **`4f01cd22f4d29207…`** |

`src/Kulani.KDI.Api/Program.cs` = `400f468572412debca668d7c3e0ae4f7a2a82383a3fb2decfd983145479f7b13` — **coincide** con el registrado en R1/R2. Lo mismo para `src/Kulani.KDI.Web/` (18 ficheros, agregado `965ecfbc96fb6043…` constante). Además, los 197 ficheros del manifiesto vigente coinciden **uno a uno** con los hashes reales en disco (0 discrepancias).

### 5.2 El camino de credencial de la API, por lectura

`src/Kulani.KDI.Api/Program.cs`:

- L15: `using var password = consoleCredential ? ReadRuntimePassword() : null;` → el `using` de nivel superior **destruye el `SecureString` al cerrar el proceso**.
- L109-134 `ReadRuntimePassword`: construye `result`, en el camino de error `catch { result.Dispose(); throw; }`; en el de éxito `result.MakeReadOnly(); return result;` → **entrega el `SecureString` original, de solo lectura**, a `new SqlCredential("kdi_app", password)`.

**Conclusión:** el camino de la API es **independiente** de `OperatorConnection` y **no ha sido tocado**. Su invariante (original de solo lectura + destrucción determinista por `using`) se mantiene íntegra.

### 5.3 Comportamiento observable de la vía interactiva

Verificado **por lectura** (`OperatorConnection.cs:63-77`) y **por ejecución con los seams** (mi test `Qa_Interactive_path_keeps_prompt_messages_and_check_order`, que captura `Console.Out`):

| Elemento | Valor verificado | Cómo |
|---|---|---|
| Prompt | `"kdi_migrator password (protected input): "` | constante `PasswordPrompt` + `Console.Write(PasswordPrompt)`; capturado en la salida |
| 1ª comprobación | base de datos → `ArgumentException("Unauthorized database.")` | ejecución y lectura |
| 2ª comprobación | consola → `InvalidOperationException("A private interactive console is required.")` | ejecución y lectura |
| 3ª comprobación | lectura por teclado (prompt → bucle `Enter`/`Backspace`/`!char.IsControl` → `Console.WriteLine()` → `MakeReadOnly()`) | lectura + test del flujo de teclas (`a b c ⌫ d ^C ⏎` → longitud 3) |
| 4ª comprobación | buffer vacío → `InvalidOperationException("Credential required.")` | ejecución |
| Fin de la vía | `return Create(database, password)` → credencial de solo lectura | ejecución |

El **orden** queda fijado por aserción: M5 (orden invertido) → **detectada**.

**Nota (sin impacto observable):** la ronda 3 añadió el mensaje `"The operator console is not interactive."` en el `ReadKey()` privado cuando el hook está activo. Esa rama es **inalcanzable**: `NotInteractive()` ya devuelve `true` cuando `_notInteractiveForced` está activo, de modo que la guarda lanza antes de leer el teclado. Es defensive y hace el camino **más estricto**, no menos.

---

## 6. Análisis del hook: ¿abre una vía?

**Sentido en que puede actuar: solo hacia el estado más estricto.**

`OperatorConnection.cs:16-18` y `:109-113`:

```csharp
private static volatile bool _notInteractiveForced;
internal static void ForceNotInteractive(bool forced) => _notInteractiveForced = forced;
private static bool NotInteractive() => Console.IsInputRedirected || Console.IsOutputRedirected || _notInteractiveForced;
private static ConsoleKeyInfo ReadKey() => _notInteractiveForced
    ? throw new InvalidOperationException("The operator console is not interactive.")
    : Console.ReadKey(intercept: true);
```

- El hook **no puede desactivar** la guarda: el único predicado que decide es `NotInteractive()`, y el hook solo **añade** un término `||`. Ponerlo a `false` devuelve el estado real de la consola.
- El hook **no puede permitir** la lectura del teclado: con el hook activo, `ReadKey()` lanza antes de leer.
- El hook **no puede relajar** la comprobación de base de datos: es independiente del hook.
- **Comprobado empíricamente** (sonda sobre el ensamblado publicado): con el hook activo, `Create("KulaniKDI_Test")` → `InvalidOperationException: A private interactive console is required.`; tras liberar el hook, los hechos reales de la consola vuelven a decidir → **sigue rechazando** (en este proceso la consola está redirigida: `Console.IsInputRedirected=True Console.IsOutputRedirected=True`). **No puede construirse una credencial sin consola privada por el hook.**

**Matices que sí debo declarar:**

1. **El hook es `internal` y el ensamblado no está firmado con clave** (`Directory.Build.props` no define `SignAssembly`; solo hay `[assembly: InternalsVisibleTo("Kulani.KDI.UnitTests")]`). El control es de **tiempo de compilación**: cualquier ensamblado que se compile con el nombre simple `Kulani.KDI.UnitTests` obtiene acceso a los internos y podría construir una credencial de `kdi_migrator` saltándose la guarda de consola. Es la misma observación **N5** de R2, y **sigue abierta**. Prácticamente, cargar un ensamblado con ese nombre en el proceso del operador ya implica ejecución de código.
2. **El control no es de tiempo de ejecución.** Cualquier código *in-proceso* puede invocar la sobrecarga `internal` por **reflexión** (mi sonda lo hizo, precisamente para poder verificar la construcción de la credencial). Los CLI son procesos separados y ningún código de producto usa la reflexión, así que no hay vía de producto; pero «internal» aquí no es una frontera de seguridad.
3. **Los seams de consola y teclado sí evitan la guarda de consola por diseño** (`Create(db, consoleRedirected, readKey, observeTypedBuffer)`): permiten construir una credencial sin consola privada. Es **inherente** a la costura y es aceptable **porque es `internal`** y solo la usa el ensamblado de tests. El riesgo está en (1) y (2), no en el seam en sí.
4. **Observación propia (no está en R1 ni en R2):** la batería actual **no impide que alguien añada un hook permisivo**. La mutación M9b+M9 (añadir `ForceInteractive` que desactive la guarda) deja los 78 tests **en verde**. No es un defecto del candidato actual —que no tiene ese hook—, pero es un **hueco de cobertura sobre la dirección del hook**: si R4 quisiera blindarlo, bastaría una aserción de que no existe ningún miembro estático público/no público que pueda forzar el estado "interactivo", o directamente que `NotInteractive()` solo pueda endurecerse. Lo dejo como **BAJO / informativo**, no como FAIL.

---

## 7. Veredicto por criterio

### PASS

| Criterio | Motivo |
|---|---|
| Integridad del candidato (antes y después) | `freeze.py --verify` exit 0, `91dce7d9…`, 197 ficheros, **idéntico** antes y después de todas las pruebas |
| Árbol de la raíz congelado intacto | `identities-20261018/verify.py` exit 0 al principio y al final |
| Build Release | 0 errores, 0 advertencias |
| Regresión UnitTests | 71/71, 0 fallos, 0 omitidas (referencia 57 → **+14**, todas en `OperatorCredentialTests.cs`) |
| Regresión ApiTests | 20/20, 0 fallos, 0 omitidas (sin cambio) |
| Regresión ArchitectureTests | 24/24, 0 fallos, 0 omitidas (sin cambio) |
| **Defecto original cerrado** | Construcción de la credencial **sin excepción** comprobada sobre el ensamblado publicado; `SecureString` entregado **de solo lectura** y **distinto** del buffer del llamador; el defecto reintroducido (M3) hace fallar 11 tests |
| **Los tests nuevos son útiles, no decorativos** | 11 de 13 mutaciones detectadas; las 2 no detectadas están justificadas (mutante equivalente / observación menor) |
| Superficie pública acotada | Una sola entrada pública `Create(string) → Session`; la sobrecarga con `SecureString` es `internal` (verificado por reflexión **y** por mutación M1) |
| Ciclo de vida del material sensible | Buffer tecleado destruido (`using`), copia destruida por `Session.Dispose`/`DisposeAsync` (M2/M8/M11), rama de error destruye la copia; **ningún camino depende del GC** |
| Comportamiento observable de la vía interactiva | Prompt, tres mensajes y **orden** de comprobaciones verificados por lectura **y** por ejecución con los seams; fijados por aserción (M5/M7) |
| `src/Kulani.KDI.Api/**` sin cambios | Agregado de hashes **idéntico en los 8 manifiestos**, incluida la base previa a las rondas; `Program.cs` = `400f4685…` |
| Camino de credencial de la API intacto | `ReadRuntimePassword` entrega el original de solo lectura; su `using` lo destruye al cerrar el proceso (lectura) |
| Front: typecheck, lint, build | exit 0 en los tres; **hashes de artefacto idénticos** a la referencia (`index-Do02OOjV.css`, `index-DY6Z8_x8.js` → `index-DY6Y8_x8.js`) |
| Front: navegador | Playwright Chromium **11/11**, 0 fallos, 0 omitidas |
| Artefactos SQL, verificación offline | `verify_sql.py` exit 0: 14 hashes, 12 reproducciones exactas, 4 ámbitos de rollback, 3 propuestas sin `GRANT DELETE`. **No se ejecutó SQL** |
| Hook unidireccional (sentido) | Solo puede endurecer la guarda; demostrado por lectura, por sonda sobre el ensamblado publicado y por M6/M12 |

### BLOCKED (no PASS)

| Criterio | Motivo |
|---|---|
| **A5 (fixtures/harness y pruebas de sesiones por corrida)** | El snapshot posterior a A4 (`evidence/A1/A4-snapshot-despues-20261021-031002.log`, leído como fichero) muestra que **`kdi_tests` conserva solo `SELECT`** sobre los tres schemas: el harness necesita `INSERT` y `DELETE`. **Bloqueante de plan vigente**, no de código. No se ha solicitado A5. |
| **Permisos efectivos del harness sobre `DELETE` en las 7 tablas** | No medidos por mí (prohibido abrir conexiones); el estado por rol documentado sigue siendo `kdi_tests` = solo lectura |
| **Comportamiento real de `DELETE` contra SQL; conteos de residuo; `sp_getapplock`; carrera concurrente; invariante global de administradores en runtime** | Requieren ejecución conectada: **no ejecutados** |
| **Restauración de ensayo / A7** | A7 es una puerta separada y destructiva, **no forma parte de la ejecución inicial**; no hay ensayo de restauración ni evidencia de `RESTORE` |
| **Auth0 / OIDC (A6)** | Sin cuenta, dominio, secretos ni trafico real; no verificado |
| **HTTPS real (A6)** | No verificado: el front de pruebas sirve por HTTP en `127.0.0.1` |
| **Keyring / almacén de secretos local** | No verificado; no existe en esta plataforma lo que el plan suppose |
| **Residuo histórico con `ActorId = S-1-5-21-SYNTHETIC-TEST`** | No alcanzable por el código nuevo por diseño (`tests/Kulani.KDI.SqlIntegrationTests/SqlTestRun.cs:27,40` lo documenta como residuo de la **versión anterior** del harness). Su **existencia real** en `KulaniKDI_Test` no la he medido (no conexiones) y su limpieza está **prohibida en bloque por actor fijo**: exige limpieza manual acotada a GUID/acción/ventana exactos y revisada. Dato adicional de lectura: la fila de auditoría sembrada por A3 lleva `ActorKind='User'` y `ActorId` = GUID (`controlled-test-plan/A3-SEED-USER.sql:46`), **no** el SID sintético, así que el residuo histórico (si existe) **no** procede de A3 |
| **Permisos efectivos del operador (`kdi_migrator`) con la credencial construida por la ruta corregida** | Requeriría conexión y una consola privada interactiva: no disponible en este entorno |

### NOT VERIFIED

| Criterio | Motivo |
|---|---|
| **CLI del operador**: `Kulani.KDI.AccessAdmin plan` / `apply` y `Kulani.KDI.Migrations --apply` | Prohibido por mi rol (no abrir conexiones) y **no hay consola privada interactiva** en este entorno. Solo he verificado por lectura que ambos usan `await using var session = OperatorConnection.Create(db);` y que `Session` es `IAsyncDisposable`. Sigue abierto el **N3** de R2 (ningún test ejercita el cableado de los CLI en modo no interactivo) |
| Que disponer del `SecureString` antes de `OpenAsync` no rompa el inicio de sesión | Requiere abrir una conexión |
| `SecureString` (cifrado DPAPI) fuera de Windows | No ejercitado; el `Migrations --apply` no exige Windows (`AccessAdmin` sí). Es **N8/H6** de R1/R2, sigue abierto |
| Comportamiento con una versión de `Microsoft.Data.SqlClient` distinta de **6.1.7** (la fijada en `packages.lock.json`) | Solo verificado con 6.1.7 |
| Paralelismo real de la suite bajo `Console.SetOut` | El diseño de xUnit v3 (`CollectionDefinition … DisableParallelization = true`) y la ejecución limpia 71/71 dan confianza; no medí concurrencia con otro test que escriba en consola |
| Reglas de `ArchitectureTests` sobre el camino de operador | Verificado por lectura: **ninguna** de las 24 reglas menciona `Operator`, `SecureString`, `Credential`, `Console` ni `Password`. La garantía del delta descansa **enteramente** en `OperatorCredentialTests` |
| `Kulani.KDI.SqlIntegrationTests` | **No ejecutada** (prohibido). Los ficheros de esa suite sí cambiaron en el diff acumulado (5 ficheros: `D2bAccessSqlTests.cs`, `D2bAdministrationSqlTests.cs`, `SqlBaselineTests.cs` y 3 nuevos auxiliares) |

### FAIL

**Ninguno.** No he encontrado ningún defecto funcional en el candidato vigente.

---

## 8. Omitidas y bloqueadas, sin eufemismos

- **No hay PASS global.** D2-B sigue **OPEN/BLOCKED**.
- **No abrí ninguna conexión a SQL Server.** Ni `SqlConnection.Open`, ni `sqlcmd`, ni `dotnet ef`, ni el harness SQL, ni el CLI de administración o el de migraciones.
- **No ejecuté** `Kulani.KDI.SqlIntegrationTests` (prohibido).
- **No ejecuté** `AccessAdmin plan/apply` ni `Migrations --apply`: requieren consola privada interactiva que este entorno no ofrece, y está fuera de mi rol.
- **No desplegué** nada. El front de pruebas se levantó **solo en `127.0.0.1`** (API en `5080`, previsualización Vite en `4173`), lo arranca y lo para el propio `global-setup.ts` de Playwright; es un proceso de pruebas, no un despliegue.
- **No toqué el estado de la base.** El ejecutor principal aplicó A4 antes de esta revisión; yo solo he **leído como fichero** `evidence/A1/A4-verify-20261021-031002.log` y `A4-snapshot-despues-20261021-031002.log`.
- **Observación de evidencia:** `evidence/A1/A4-apply-20261021-031002.log` está **vacío (0 bytes)**. Los logs de *snapshot* y de *verificación* sí son sustantivos y coherentes entre sí (`A4 VERIFICATION OK`), pero **el paso de aplicación no tiene log de salida**: es un hueco de trazabilidad, no un fallo funcional. Lo dejo dicho sin exagerarlo.
- **No hice ensayo de restauración** (A7) ni ejecuté el `RESTORE` de ensayo que la evidencia de A2 menciona como verificado en su día.
- **No ejecuté `npm ci`** ni borré `node_modules`; caché npm en `E:\INSSOFT\KDI\.local\npm-cache`; `npm.cmd` (nunca `npm`).
- **No modifiqué** el candidato, su manifiesto ni nada bajo `.local/d2b-candidate/`. Las únicas escrituras ahí fueron `bin/`, `obj/`, `dist/`, `.local/e2e/` y `docs/stage3/d2b/evidence/browser-artifacts/`, todas **excluidas** por el verificador; integridad reconfirmada al final con el **mismo hash**.
- **No accedí** al repositorio IDI.
- **No delegué** en ningún subagente (rol `qa-engineer`, agent_type efectivo `general`).

---

## 9. Discrepancias y novedades respecto a los informes anteriores

1. **R2 (`CODE-REVIEW-R2.md`) está desactualizado respecto del candidato vigente**, no solo en los conteos: describe **12 tests / 69 unitarias / hash `79d63864…`**. El candidato vigente es **`91dce7d9…` / 14 tests / 71 unitarias**. La ronda 3 es un delta de **2 ficheros** (`OperatorConnection.cs`, `OperatorCredentialTests.cs`) con **0 altas y 0 bajas**.
2. **N1 y N2 de R2 están CERRADOS, y lo he verificado por mutación, no por lectura del informe.** R2 comprobó que su mutación M9 (`Create(string) => Create(database, () => false, ReadKey)`) **no** se detectaba; en el candidato vigente **sí** (mi M12 → 2 errores). R2 comprobó igualmente que su M8 (buffer tecleado sin `using`) **no** se detectaba; en el candidato vigente **sí** (mi M4 → 1 error). Ninguno de los dos informes previos contenía esa verificación.
3. **N4 de R2 (comentario inexacto) está cerrado en la ronda 3**: el comentario vigente dice «The plain text of the password is never exposed; the SecureString instance itself stays reachable through `SqlConnection.Credential.Password`, as it was before», que ya no afirma que el material sea inalcanzable. R2 citaba el texto anterior («The material is never exposed»).
4. **N3 (cableado de los CLI sin cobertura) sigue abierto** en el candidato vigente. Confirmado: ningún test del repositorio invoca `AccessAdmin` ni `Migrations`. No lo cierro.
5. **N5 (`InternalsVisibleTo` sin firma fuerte) sigue abierto.** Confirmado por lectura: `Directory.Build.props` no define `SignAssembly`; la puerta es el nombre simple del ensamblado.
6. **N6 (`Session` sin finalizador de reserva) sigue abierto.** Confirmado por lectura del fichero vigente: no hay finalizador.
7. **N7 / H5 (documentación desactualizada) sigue abierto**, y es más grave de lo que R2 decía: `docs/stage3/d2b/STATUS.md:7` sigue diciendo «**57 unitarias**» y `STATUS.md:9` sigue declarando `d106dac6…` con «195 archivos»; lo vigente es **`91dce7d9…`, 197 archivos, 71 unitarias**. `docs/stage3/d2b/preparation-20261020/RESULT.md` sigue declarando «190 archivos; SHA-256 `1ab77cd8…`» y «57/57». Ninguno de esos ficheros forma parte del candidato congelado, así que **no afecta al hash**, pero el paquete de evidencia de D2-B miente sobre el estado vigente.
8. **N8/H6 (`Migrations --apply` no exige Windows) sigue abierto.**
9. **Observación nueva, no registrada en R1 ni en R2:** la batería de tests **no impide añadir un hook permisivo** (mi M9b+M9 deja 78/78 en verde) y **no verifica que el hook esté cableado a la guarda** (mi M13 deja 78/78 en verde; mutante equivalente en un host con consola redirigida). Lo reporto como **BAJO/informativo** y como límite explícito de la afirmación «el hook no abre una vía»: hoy es cierto **por construcción del código**, no **por aserción**.
10. **Matiz mío sobre la afirmación «el hook es unidireccional»**: es cierto en el sentido de *comportamiento* (probado por sonda), pero la restricción es de **tiempo de compilación**, no de tiempo de ejecución: por reflexión, cualquier código in-proceso puede invocar la sobrecarga `internal` (es lo que hizo mi sonda). Los CLI son procesos separados y el producto no usa reflexión, así que **no hay vía de producto**; pero conviene no presentar `internal` como frontera de seguridad.
11. **Sobre el recuento de tests nuevos:** R1 (4) → R2 (12) → R3 (14). El total 71 = 57 + 14. Las 14 están todas en el fichero nuevo `OperatorCredentialTests.cs`; ninguna suite de SQL, API o arquitectura ha cambiado de recuento.

---

## 10. Evidencia dejada

- `docs/stage3/d2b/operator-credential-fix-20261021/QA.md` (este informe).
- `docs/stage3/d2b/operator-credential-fix-20261021/qa/backend-build.log`, `UnitTests.log`, `ApiTests.log`, `ArchitectureTests.log`, `web-typecheck.log`, `web-lint.log`, `web-build.log`, `browser.log`, `operator-probe.log`, `mutations.log`.
- Copia de mutaciones (fuera del candidato): `C:\Users\Israel\AppData\Local\Temp\opencode\kdi-mut` (base + `mutate.py`), sonda independiente: `C:\Users\Israel\AppData\Local\Temp\opencode\kdi-probe`.
- **Veredicto global de D2-B: BLOCKED. No hay PASS global.**
