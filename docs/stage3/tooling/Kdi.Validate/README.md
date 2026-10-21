# Kdi.Validate — runner único de validación local (Stage 3 / D2-B)

Ejecuta, de forma reproducible y con un único punto de entrada, toda la cadena local de
verificación del workspace KDI sobre un **candidato configurable** y emite un informe
estructurado en JSON.

- Script: [`validate.ps1`](validate.ps1)
- PowerShell 5.1 (Windows PowerShell), sin dependencias externas más allá de las herramientas
  ya presentes en la máquina (Python, .NET SDK 10.0.103, Node 22.23.3, npm, Chromium local).
- No usa `git` (el repositorio no está inicializado).
- No crea ni modifica nada en `src/`, `tests/`, `tools/`, `docs/api/`, `docs/source/` ni
  `docs/stage3/d2a/`: el árbol de la raíz está congelado por hash
  (`tools/d2a-manifest.py`). Las únicas salidas del runner viven en `.local/validate/`.

> Este runner valida **reproducibilidad local**. No es una aceptación de D2-B ni un PASS
> global: los criterios que exigen SQL, Auth0/OIDC real, HTTPS o keyring siguen BLOCKED.

## 1. Uso

```powershell
# 1) Listar pasos y comandos previstos SIN ejecutar nada
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File E:\INSSOFT\KDI\docs\stage3\tooling\Kdi.Validate\validate.ps1 -ListSteps

# 2) Ejecutar la cadena completa contra el candidato por defecto
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File E:\INSSOFT\KDI\docs\stage3\tooling\Kdi.Validate\validate.ps1

# 3) Validación parcial (solo pasos de lectura, sin build ni pruebas)
... validate.ps1 -StopAfter integrity-d2a-before
```

| Parámetro | Por defecto | Descripción |
|---|---|---|
| `-RepoRoot` | deducedo de la ubicación del script | Raíz del workspace KDI. |
| `-CandidateRoot` | `<repo>/.local/d2b-candidate` | Candidato a validar. |
| `-OutputRoot` | `<repo>/.local/validate` | Directorio de logs e informes. |
| `-ReportPath` | `<OutputRoot>/run-<marca>/validate-report.json` | Informe JSON. |
| `-NodeHome` | descubierto | Carpeta con `node.exe` / `npm.cmd` de Node 22.23.3. |
| `-StopAfter` | — | Corta la cadena tras el paso indicado; los siguientes quedan `NOT_RUN`. |
| `-ListSteps` | off | Enumera pasos y comandos sin ejecutar nada. |
| `-SkipStructureCheck` | off | Omite `Kulani.KDI.StructureCheck`. |
| `-SkipPlaywright` | off | Omite la suite de navegador. |
| `-InstallWebDependencies` | off | `npm.cmd ci --offline` antes de los pasos web (borra `node_modules`). |

Códigos de salida: `0` = PASS, `1` = FAIL/PARTIAL, `2` = error de uso o precondición.

## 2. Pasos y comandos exactos

| # | id | comando | cwd |
|---|---|---|---|
| 1 | `preflight` | (solo lectura: intérpretes, Node, cachés, navegadores) | `<repo>` |
| 2 | `integrity-candidate-before` | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` | `<repo>` |
| 3 | `integrity-d2a-before` | `python docs/stage3/d2a/closure/identities-20261018/verify.py` | `<repo>` |
| 4 | `backend-build` | `dotnet build Kulani.KDI.slnx -c Release --no-restore` | `<candidato>` |
| 5 | `unit-tests` | `dotnet test --project tests/Kulani.KDI.UnitTests/Kulani.KDI.UnitTests.csproj -c Release --no-build --no-restore` | `<candidato>` |
| 6 | `api-tests` | `dotnet test --project tests/Kulani.KDI.ApiTests/Kulani.KDI.ApiTests.csproj -c Release --no-build --no-restore` | `<candidato>` |
| 7 | `architecture-tests` | `dotnet test --project tests/Kulani.KDI.ArchitectureTests/Kulani.KDI.ArchitectureTests.csproj -c Release --no-build --no-restore` | `<candidato>` |
| 8 | `sql-integration-tests` | **no se ejecuta** — `SKIPPED_BY_POLICY` | — |
| 9 | `structure-check-build` | `dotnet build tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj -c Release --no-restore` | `<candidato>` |
| 10 | `structure-check-restore` | `dotnet restore tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj --locked-mode --configfile <run>/NuGet.offline.config` (solo si el build falló) | `<candidato>` |
| 11 | `structure-check-rebuild` | `dotnet build tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj -c Release --no-restore` (solo si el restore terminó en 0) | `<candidato>` |
| 12 | `structure-check-run` | `tools/Kulani.KDI.StructureCheck/bin/Release/net10.0/Kulani.KDI.StructureCheck.exe` | `<candidato>` |
| 13 | `web-typecheck` | `cmd.exe /d /c "<nodeHome>\npm.cmd" run typecheck` | `<candidato>/src/Kulani.KDI.Web` |
| 14 | `web-lint` | `cmd.exe /d /c "<nodeHome>\npm.cmd" run lint` | `<candidato>/src/Kulani.KDI.Web` |
| 15 | `web-build` | `cmd.exe /d /c "<nodeHome>\npm.cmd" run build` | `<candidato>/src/Kulani.KDI.Web` |
| 16 | `playwright-chromium` | `node node_modules/@playwright/test/cli.js test --project=chromium --reporter=list --output=<run>/playwright-artifacts` | `<candidato>/src/Kulani.KDI.Web` |
| 17 | `integrity-candidate-after` | `python docs/stage3/d2b/preparation-20261020/freeze.py --verify` | `<repo>` |
| 18 | `integrity-d2a-after` | `python docs/stage3/d2a/closure/identities-20261018/verify.py` | `<repo>` |

Si se pide `-InstallWebDependencies`, se antepone el paso `web-install-dependencies`
(`npm.cmd ci --offline --ignore-scripts --no-audit`).

### Reglas de dependencia y parada

- Si falla `integrity-candidate-before` o `integrity-d2a-before`, el runner **se detiene**
  y lo reporta: no lanza build ni pruebas sobre un candidato que no coincide con su manifiesto.
- Los pasos que dependen de un paso fallido quedan `SKIPPED_DUE_TO_DEPENDENCY_FAILURE`.
- Los pasos web y de navegador quedan `SKIPPED` si falta Node 22.23.3, `npm.cmd`,
  `node_modules` o el Chromium local; el resultado nunca es `PASS` en ese caso.

## 3. Política de `Kulani.KDI.SqlIntegrationTests`

El runner **nunca** ejecuta esa suite. Aparece en el informe como

```json
{ "id": "sql-integration-tests", "status": "SKIPPED_BY_POLICY", "policyReason": "..." }
```

con el motivo explícito: abre conexiones a SQL Server y ejecuta operaciones persistentes
sobre `KulaniKDI_Test`, y la autorización A5 (migraciones, fixtures y limpieza) no existe.
No hay flag para activarla: habilitarla requiere un cambio autorizado del runner.

El build de la solución **sí compila** ese proyecto (forma parte de `Kulani.KDI.slnx`).
Compilar no ejecuta pruebas ni abre conexiones; el informe lo declara en `notes`.

## 4. Entorno y caches locales (sin red)

| Variable | Valor |
|---|---|
| `DOTNET_CLI_HOME` | `<repo>/.local/dotnet-home` |
| `NUGET_PACKAGES` | `<repo>/.local/nuget/packages` |
| `PLAYWRIGHT_BROWSERS_PATH` | `%LOCALAPPDATA%\ms-playwright` |
| `NPM_CONFIG_CACHE` | `<repo>/.local/npm-cache` |
| `NPM_CONFIG_OFFLINE` | `true` |
| `PATH` | `<nodeHome>` antepuesto |

Se fija además `DOTNET_CLI_TELEMETRY_OPTOUT`, `DOTNET_NOLOGO`,
`DOTNET_SKIP_FIRST_TIME_EXPERIENCE`, `NUGET_XMLDOC_MODE`, `PYTHONUTF8` y
`PYTHONDONTWRITEBYTECODE`. Todas son variables del proceso del runner: **no se toca la
configuración global** (política de ejecución, variables de usuario, instalaciones).

### Node 22.23.3

El proyecto declara `engines.node = 22.23.3` (`src/Kulani.KDI.Web/package.json` y
`.node-version`), y `engine-strict=true` en el `.npmrc`. El runner:

1. lee la versión esperada de `.node-version` (fallback: `package.json`);
2. recorre directorios candidatos (`%LOCALAPPDATA%\pi-node\current`, `pi-node`,
   `nodejs`, `Programs\nodejs`, `nvm`, `Volta`, `scoop`, o el `-NodeHome` explícito);
3. ejecuta `node.exe --version` y **exige coincidencia exacta** con la versión esperada;
4. usa `node.exe` y `npm.cmd` de esa misma instalación para los pasos web y de navegador.

En esta máquina la instalación válida es
`C:\Users\Israel\AppData\Local\pi-node\current` (Node `v22.23.3`, npm `10.9.9`).
El Node del `PATH` (`C:\Program Files\nodejs`, v20.12.2) **no** se usa: no cumple la
versión declarada.

### npm

Se usa `npm.cmd`, nunca `npm`: el shim `npm.ps1` choca con la política de firma de
PowerShell en este entorno. No se cambia la política de ejecución del sistema. El runner
no ejecuta `npm install`; solo `npm run typecheck|lint|build` (y `npm ci --offline` como
opción explícita y destructiva).

### Restore offline de StructureCheck

`Kulani.KDI.StructureCheck` no forma parte de la solución, así que el candidato puede no
tener sus assets de restore. Cuando el build `--no-restore` falla, el runner ejecuta un
restore **offline** con un `NuGet.config` temporal generado en el directorio de la corrida:

- única fuente: `<repo>/.local/nuget/packages` (sin `nuget.org`, sin red);
- `globalPackagesFolder` apuntando a la misma caché local;
- `--locked-mode`, para que NuGet **no** pueda reescribir `packages.lock.json`.

Si el restore no es posible, el paso queda `SKIPPED`/`FAILED` con el motivo en el informe
y en `notes`; no se degrada el resultado a un PASS silencioso.

## 5. El runner no muta el candidato

El manifiesto del candidato excluye `bin`, `obj`, `node_modules`, `dist`, `wwwroot`,
`TestResults`, `test-results`, `playwright-report`, `__pycache__`, `.local`, `.git` y
`docs/stage3/d2b/evidence/`. Aun así, el runner lo demuestra:

1. **Digest independiente**: antes de cualquier paso y al final calcula un SHA-256 sobre
   el inventario de archivos cubiertos por las mismas reglas de exclusión de `freeze.py`
   (`candidate.contentDigestBefore` / `contentDigestAfter`). Si el candidato cambia, los
   digests difieren y el resultado no puede ser `PASS`.
2. **Hash de manifiesto**: `candidate.hashBefore` / `candidate.hashAfter` se extraen de la
   salida de `freeze.py --verify` (`SHA256 <hash>`); deben ser idénticos.
3. **Artefacto de StructureCheck**: el tool escribe
   `docs/stage3/d2a/evidence/architecture-result.json` **dentro** del candidato. El runner
   lo restaura a su estado previo (byte a byte, o lo borra si no existía) y recalcula el
   digest inmediatamente después; si cambia, el paso se marca `FAILED`.
4. **Salidas de navegador**: `--reporter=list` y `--output` fuera del candidato evitan
   escribir `browser-contracts.json` y `browser-artifacts/` dentro del candidato.

## 6. Informe (`validate-report.json`)

Campos principales:

- `candidateRoot`, `candidate.root`.
- `candidate.hashBefore` / `candidate.hashAfter` (SHA-256 del manifiesto del candidato) y
  `candidate.contentDigestBefore` / `contentDigestAfter` (digest independiente del árbol),
  `candidate.unchanged`, `candidate.manifestHashStable`, `candidate.fileCountBefore/After`.
- `startedAtUtc`, `finishedAtUtc`, `durationMs`.
- `steps[]`: `id`, `title`, `kind`, `command` (comando exacto), `workingDirectory`,
  `dependsOn`, `status`, `exitCode`, `durationMs`, `logPath`, `startedAtUtc`,
  `finishedAtUtc`, `message`, `policyReason`, `notes`.
- `totals`: `planned`, `executed`, `passed`, `failed`, `skippedByPolicy`, `skippedOther`,
  `notRun`.
- `integrity`: `candidateUnchanged`, `manifestHashStable`, `contentDigestStable`.
- `prerequisites`: `nodeReady`, `webDependenciesReady`, `playwrightBrowsersReady`,
  `missing[]`.
- `host`: máquina, SO, PowerShell, rutas de python/dotnet/node/npm, cachés.
- `notes[]`: limitaciones explícitas de cobertura.

Estados de paso: `PASSED`, `FAILED`, `SKIPPED_BY_POLICY`, `SKIPPED`,
`SKIPPED_DUE_TO_DEPENDENCY_FAILURE`, `NOT_RUN`.

Cada paso deja un log en `<OutputRoot>/run-<marca>/<stepId>.log` con cabecera (comando,
cwd, exit code, duración) y el stdout/stderr separados.

## 7. Lo que el runner NO cubre

- `Kulani.KDI.SqlIntegrationTests` y toda la integración con SQL Server: migraciones,
  idempotencia, permisos efectivos, fixtures, concurrencia y auditoría transaccional.
- Auth0/OIDC real: discovery, validación criptográfica de tokens, code+PKCE, callback,
  `nonce`/`state`, replay, logout.
- HTTPS, certificados, confianza de la máquina, keyring externo y ACL.
- Revocación persistente, expiración y carreras; multiempresa con datos reales.
- Red y cadena de suministro: no hay restore contra `nuget.org`, ni `npm install`, ni
  auditoría de vulnerabilidades; tampoco reproducibilidad de restore desde caché vacía.
- Vigencia o actualización del SDK .NET instalado (`global.json` fija 10.0.103 con
  `rollForward: disable`).
- Revisión de código, aceptación funcional y calidad del producto: el runner mide
  ejecución y reproducibilidad, no corrección de negocio.

## 8. Riesgos conocidos (revisar con prioridad)

1. **Entorno sin Node 22.23.3 o sin cachés**: los pasos web y de navegador quedan
   `SKIPPED` y el resultado es `PARTIAL`, nunca `PASS`. Si `node_modules` falta, el
   runner **no** reinstala nada salvo que se pase `-InstallWebDependencies`, que borra
   `node_modules` y puede dejar el proyecto web sin dependencias si la caché no cubre el
   lockfile (la caché npm del candidato está vacía; la del repo tiene ~689 entradas).
2. **Restore offline de StructureCheck**: es el único punto del runner que escribe en el
   candidato (`obj/`), y `--locked-mode` es lo que impide que NuGet reescriba
   `packages.lock.json`. Si la caché local no cubre el grafo, el paso falla con NU1004.
   Está aislado en pasos propios para que un fallo no contamine el resto.
3. **Estado del workspace tras la corrida**: quedan `bin/`, `obj/`, `dist/`,
   `.local/e2e/` (logs de API y web del Playwright), `test-results/` y
   `playwright-report/` dentro del candidato y/o del repo. Todos están fuera del
   manifiesto, pero el árbol **no** queda limpio: `bin/` y `obj/` pueden quedar con
   compilaciones de un candidato anterior. Si se cambia de candidato, conviene borrar
   `bin/` y `obj/` del destino antes de comparar artefactos.
4. **Sin timeout por paso**: un proceso colgado deja el runner bloqueado indefinidamente.
   El timeout efectivo depende de cada suite (Playwright: `globalTimeout` 120 s).
5. **Concurrencia**: el runner escribe `bin/`, `obj/` y `dist/` dentro del candidato. No
   debe ejecutarse en paralelo con otra compilación del mismo árbol (el caso actual: hay
   un cambio concurrente en `tests/Kulani.KDI.SqlIntegrationTests`).
6. **Fidelidad de `StructureCheck`**: el tool escribe siempre en
   `docs/stage3/d2a/evidence/` de la raíz que detecta por `Kulani.KDI.slnx`; al correr
   dentro del candidato produce su artefacto ahí y el runner lo restaura. El runner no
   valida el resultado del `StructureCheck` del árbol original, solo el del candidato.
7. **El digest independiente replica las reglas de `freeze.py`** en PowerShell. Si esas
   reglas cambiaran en `freeze.py`, el digest podría quedar desalineado; la verificación
   autoritativa sigue siendo `freeze.py --verify` (paso 2 y 17).

## Correcciones aplicadas tras la primera ejecución de extremo a extremo

El runner no se habia ejecutado nunca completo. Al hacerlo aparecieron tres defectos reales, corregidos y verificados:

1. **Orden de pasos de StructureCheck.** Intentaba compilar el tool *antes* de restaurarlo. Como Kulani.KDI.StructureCheck esta fuera de la solucion y por tanto no tiene assets de restore en el candidato, el build fallaba con NETSDK1004 (no existe `obj/project.assets.json`) y el resultado global era FAIL. Ahora el **restore offline va primero** y el build despues, con `--no-restore`.

2. **`NuGet.offline.config` malformado.** La seccion `packageSourceMapping` usaba la forma invalida `<add key=... pattern=.../>`; NuGet la rechazaba con *Falta el atributo obligatorio "value" en el elemento "add"* y el restore no llegaba a empezar. Con una unica fuente local tras `<clear/>` el mapeo no es necesario y se elimino.

3. **Dependencias no bloqueantes.** Un paso se ejecutaba aunque su dependencia se hubiera **omitido**, produciendo un fallo engañoso (intentar ejecutar un ejecutable inexistente: exit -1). Ahora `SKIPPED` tambien bloquea al dependiente, que queda como `SKIPPED_DUE_TO_DEPENDENCY_FAILURE`.

## Estado de StructureCheck y el caso NU1004

El restore offline del tool puede fallar con **NU1004**: el `packages.lock.json` de `Kulani.KDI.StructureCheck` no coincide con las dependencias actuales del grafo. Es una inconsistencia **preexistente del candidato congelado**, y el runner **no la corrige** porque corregirla mutaria el candidato.

En ese caso el runner registra el paso como `SKIPPED` con el motivo explicito, no como fallo silencioso, y el resultado global baja a `PARTIAL`. El criterio de StructureCheck queda **NOT VERIFIED**. La politica de arquitectura **si** queda verificada, por la suite `Kulani.KDI.ArchitectureTests` (24/24), que forma parte de la solucion y si se ejecuta.

## Resultado de la ejecucion de referencia (2026-10-20)

Candidato `d106dac65976491637196752d55a5cb122ca16dab41a2f9f5a1f4a5c342809cb` (195 archivos).

- 13 pasos correctos, 0 fallidos.
- 1 omitido por politica: `sql-integration-tests` (abre conexiones a SQL Server; A5 no autorizada).
- 1 omitido con motivo explicito: restore de StructureCheck (NU1004) y 2 omitidos en cascada.
- Resultado global: **PARTIAL**.
- **Candidato intacto: True.** Manifiesto estable y digest de contenido identico antes y despues.

El resultado `PARTIAL` es el valor honesto: no todos los criterios del runner quedaron verificados.