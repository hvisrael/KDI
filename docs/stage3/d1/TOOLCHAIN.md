# Herramientas y reproducción D1

## Corrección y cierre técnico — 2026-10-18

Esta sección sustituye las versiones y bloqueos históricos descritos debajo para el candidato de cierre. El SDK continúa en 10.0.103/runtime 10.0.3. Las versiones centrales encontradas al comenzar esta sesión fueron OpenAPI/Mvc.Testing 10.0.13, Test SDK 18.11.0, xunit.v3 4.0.2 y runner.visualstudio 4.0.1. Los tres locks de tests ya existían: sus metadatos locales indican origen nuget.org; no existe historial Git ni transcripción que permita atribuir su creación. Se conservaron y validaron mediante restore locked. No se cambió el SDK ni los ADR.

global.json selecciona Microsoft.Testing.Platform para esas dependencias. Reproducción, con el entorno local de proceso indicado abajo:

```powershell
dotnet restore Kulani.KDI.slnx --configfile NuGet.Config --locked-mode
dotnet build Kulani.KDI.slnx -c Release --no-restore
dotnet test --solution Kulani.KDI.slnx -c Release --no-build --output Detailed
python tools/smoke-api.py
python tools/content-manifest.py --verify
```

No utilizar `--logger trx` de VSTest ni asumir que `--report-trx` está disponible: el runner instalado no incluye esa extensión. Se conserva salida detallada de dotnet test con nombres y resultados. La prueba de excepción utiliza consola en su host derivado para evitar depender de permisos Windows EventLog; conserva todas las aserciones de redacción.

La auditoría NuGet se ejecutó explícitamente con NuGetAudit=true y NuGetAuditMode=all contra nuget.org. Los restores auxiliares sin auditoría no sustituyen ese resultado. npm audit detectó dos paquetes transitivos de desarrollo vulnerables; npm audit fix --ignore-scripts desde registry.npmjs.org actualizó tres instalaciones compatibles y terminó con cero vulnerabilidades. Se conservan los informes previos y posteriores en closure/. No se ejecutaron scripts de instalación ni cambios globales.

Para Playwright se utilizó Chromium/headless-shell 1234 ya instalado en la ubicación predeterminada del usuario, sin definir PLAYWRIGHT_BROWSERS_PATH y sin descargas. Los informes vigentes y límites están en closure/; los resultados anteriores se conservan como historial.

Entorno observado: Windows x64; .NET SDK 10.0.103, runtime ASP.NET Core/.NET 10.0.3; Node 22.23.3; npm 10.5.0. global.json deshabilita roll-forward. .node-version y package.json fijan Node/npm. Los comandos se ejecutan desde KDI, salvo indicación de Web. No requieren AI-ENGINEERING en ejecución.

Se usó la instalación Node 22.23.3 ya presente en el PATH secundario, invocando explícitamente npm 10.5.0 de la instalación existente. Solo cambió PATH del proceso. El SHA-256 medido de ese node.exe fue 9c9245166b4a8e182e0b797da9c20136117ff24368eaff1fec8343a123c8db0e; no se pudo verificar un checksum fresco de distribución por red.

## Versiones y fuentes

| Componente | Versión fijada | Fuente / licencia |
|---|---|---|
| Microsoft.AspNetCore.OpenApi | 10.0.3 | nuget.org / MIT |
| Microsoft.OpenApi transitivo | 2.0.0 | Lock de backend / MIT |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.3 | Propuesto en paquetes centrales; restore bloqueado / MIT |
| Microsoft.NET.Test.Sdk | 18.0.0 | Restore de pruebas bloqueado / MIT |
| xunit.v3 | 3.2.2 | Restore bloqueado / Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Restore bloqueado / Apache-2.0 |
| React / React DOM | 19.3.0 | registry.npmjs.org / MIT |
| Vite | 6.4.4 | registry.npmjs.org / MIT |
| TypeScript | 5.9.3 | registry.npmjs.org / Apache-2.0 |
| ESLint / @eslint/js | 9.39.5 | registry.npmjs.org / MIT |
| typescript-eslint | 8.65.0 | registry.npmjs.org / MIT |
| @types/react / react-dom | 19.3.0 | registry.npmjs.org / MIT |
| @types/node | 22.20.1 | registry.npmjs.org / MIT |
| @playwright/test | 1.62.1 | registry.npmjs.org / Apache-2.0 |

El inventario completo npm resuelto, sus licencias declaradas y los paquetes NuGet restaurados se registran en dependencies.json y los locks. Las licencias son las declaradas por cada paquete, no una aprobación legal del producto. La licencia propia de KDI sigue sin decidirse.

Fuentes primarias consultadas: [versiones mantenidas de Vite](https://vite.dev/releases), [requisitos de Vite actual](https://vite.dev/guide/), [distribución Node 22.23.3](https://nodejs.org/download/release/v22.23.3/), [descargas .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [soporte .NET](https://learn.microsoft.com/dotnet/core/releases-and-support), [xUnit 3.2.2](https://www.nuget.org/packages/xunit.v3/3.2.2), [API testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.3), [Test SDK](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.0.0), [runner](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.4), [OpenAPI](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.3) y [setup/teardown Playwright](https://playwright.dev/docs/test-global-setup-teardown).

## Entorno de proceso

En PowerShell interactivo, seleccionar los ejecutables existentes exactos; no modificar PATH global ni políticas de ejecución:

```powershell
$kdiRoot = (Get-Location).Path
$env:DOTNET_CLI_HOME = Join-Path $kdiRoot '.local\dotnet-home'
$env:NUGET_PACKAGES = Join-Path $kdiRoot '.local\nuget\packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $kdiRoot '.local\nuget\http'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$kdiNode = Get-Command node -All | Select-Object -ExpandProperty Source -Unique |
  Where-Object { (& $_ --version) -eq 'v22.23.3' } | Select-Object -First 1
if (!$kdiNode) { throw 'Se requiere Node 22.23.3 disponible.' }
$kdiNpm = Get-Command npm.cmd -All | Select-Object -ExpandProperty Source -Unique |
  Where-Object {
    $candidate = Join-Path (Split-Path $_ -Parent) 'node_modules\npm\package.json'
    (Test-Path $candidate) -and ((Get-Content -Raw $candidate | ConvertFrom-Json).version -eq '10.5.0')
  } | Select-Object -First 1
if (!$kdiNpm) { throw 'Se requiere npm 10.5.0 disponible.' }
$kdiNpmCli = Join-Path (Split-Path $kdiNpm -Parent) 'node_modules\npm\bin\npm-cli.js'
$env:PATH = (Split-Path $kdiNode -Parent) + [IO.Path]::PathSeparator + $env:PATH
```

## Backend

Con caché completa o conectividad oficial, conservar modo locked para el backend ya restaurado:

```powershell
dotnet restore src/Kulani.KDI.Api/Kulani.KDI.Api.csproj --configfile NuGet.Config --locked-mode
dotnet build src/Kulani.KDI.Api/Kulani.KDI.Api.csproj --configuration Release --no-restore
python tools/smoke-api.py
dotnet restore tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj --configfile NuGet.Config --locked-mode
dotnet run --project tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj --configuration Release --no-restore
```

En esta sesión offline se agregaron --ignore-failed-sources y -p:NuGetAudit=false a restore, registrando auditoría NOT RUN. No están deshabilitados en NuGet.Config. Para las suites de pruebas falta ejecutar el primer restore completo y generar sus locks:

```powershell
dotnet restore Kulani.KDI.slnx --configfile NuGet.Config
dotnet test Kulani.KDI.slnx --configuration Release --no-restore
```

Ese primer restore cambia el manifiesto. Luego usar --locked-mode en restauraciones reproducibles. No clasificar el fallo actual como prueba pasada.

## Web y navegador

```powershell
Set-Location src/Kulani.KDI.Web
& $kdiNode $kdiNpmCli ci --ignore-scripts
& $kdiNode $kdiNpmCli run typecheck
& $kdiNode $kdiNpmCli run lint
& $kdiNode $kdiNpmCli run build
& $kdiNode $kdiNpmCli run test:e2e
```

La corrida observada de ci añadió --offline --no-audit; no ejecutó scripts de instalación ni una auditoría. Playwright usó Chromium/headless-shell revisión 1234 ya instalado y compatible con 1.62.1. Si falta browser, su descarga autorizada para D1 requiere red y debe usar PLAYWRIGHT_BROWSERS_PATH dentro de .local; no se descargó en esta sesión.

Las pruebas arrancan procesos propios en 127.0.0.1:5080/4173, rechazan puertos ocupados y los cierran por sus handles. Logs en .local/e2e. Requieren builds previos y tienen timeout global de 60 s. No necesitan SQL.

Para desarrollo, desde la raíz ejecutar dotnet run --project src/Kulani.KDI.Api --no-launch-profile --urls http://127.0.0.1:5080 y, en otra terminal con el entorno anterior, el script dev de Web. El backend escucha en HTTP local; TLS y hosting siguen pendientes del diseño operativo.

## Recuperación offline opcional

tools/seed-offline-cache.py importa únicamente paquetes públicos necesarios desde las cachés de herramientas del usuario, comprobando SHA-512/formatos y copiando a .local. No lee productos, auth, npmrc ni configuración Codex; no contacta la red. Es un recurso de esta sesión, no requisito de KDI ni sustituto de restore oficial. cache-evidence.json registra la evidencia disponible, sin credenciales.
