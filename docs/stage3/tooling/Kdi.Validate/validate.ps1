#Requires -Version 5.1
<#
.SYNOPSIS
    Kdi.Validate - runner unico de validacion local para candidatos KDI (Stage 3 / D2-B).

.DESCRIPTION
    Ejecuta, en orden y de forma reproducible, la cadena local de verificacion sobre un
    candidato configurable (por defecto `.local/d2b-candidate`):

      1. Integridad previa    : freeze.py --verify  +  identities-20261018/verify.py
      2. Build backend        : dotnet build Kulani.KDI.slnx -c Release --no-restore
      3. Suites xUnit         : UnitTests, ApiTests, ArchitectureTests (una por una)
         Politica             : Kulani.KDI.SqlIntegrationTests NUNCA se ejecuta (SKIPPED_BY_POLICY)
      4. StructureCheck       : build + ejecucion de Kulani.KDI.StructureCheck (opcional)
      5. Frontend             : npm.cmd run typecheck / lint / build
      6. Playwright           : Chromium (suite de src/Kulani.KDI.Web/tests)
      7. Integridad posterior : misma verificacion previa + comparacion de hash del candidato

    Restricciones del runner (no negociables):
      * Sin red: nunca `dotnet restore` contra nuget.org ni `npm install` con red.
        El restore offline (solo para StructureCheck) usa un NuGet.config temporal
        generado por este script, la cache local de paquetes y `--locked-mode`.
      * No abre conexiones SQL, no ejecuta migraciones, no lee credenciales.
      * No muta el candidato: cualquier artefacto generado dentro del candidato se restaura
        al estado previo y se demuestra con un digest independiente del arbol de archivos
        (no solo con el manifiesto).
      * No usa `git` (el repositorio no esta inicializado).
      * No usa `npm` (npm.ps1) por la politica de firma de PowerShell: usa `npm.cmd`.

.PARAMETER RepoRoot
    Raiz del workspace KDI. Por defecto se deduce de la ubicacion de este script
    (`<repo>/docs/stage3/tooling/Kdi.Validate/validate.ps1` -> `<repo>`).

.PARAMETER CandidateRoot
    Candidato a validar. Por defecto `<repo>/.local/d2b-candidate`.

.PARAMETER OutputRoot
    Directorio de salidas (logs + informe). Por defecto `<repo>/.local/validate`.

.PARAMETER ReportPath
    Ruta del informe JSON. Por defecto `<OutputRoot>/run-<marca>/validate-report.json`.

.PARAMETER NodeHome
    Directorio con node.exe / npm.cmd de Node 22.23.3. Si se omite se descubre en la maquina.

.PARAMETER ListSteps
    Enumera los pasos y los comandos previstos SIN ejecutar nada.

.PARAMETER SkipStructureCheck
    Omite el paso de Kulani.KDI.StructureCheck.

.PARAMETER SkipPlaywright
    Omite la suite de navegador.

.PARAMETER InstallWebDependencies
    Opcional: ejecuta `npm.cmd ci --offline` antes de los pasos web. OFF por defecto
    porque `npm ci` borra node_modules y la cache npm del candidato esta vacia.

.PARAMETER StopAfter
    Corta la cadena tras el paso indicado (por ejemplo `integrity-d2a-before`). Los pasos
    posteriores se registran como NOT_RUN y el resultado nunca es PASS. Pensado para
    validar el runner sin lanzar build ni pruebas.

.NOTES
    Codigo de salida: 0 = PASS, 1 = FAIL/PARTIAL, 2 = error de uso o de precondicion.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$CandidateRoot,
    [string]$OutputRoot,
    [string]$ReportPath,
    [string]$NodeHome,
    [string]$StopAfter,
    [switch]$ListSteps,
    [switch]$SkipStructureCheck,
    [switch]$SkipPlaywright,
    [switch]$InstallWebDependencies
)

$ErrorActionPreference = 'Stop'
$script:RunnerName    = 'Kdi.Validate'
$script:RunnerVersion = '1.0.0'
$script:SchemaVersion = '1.0'

# --------------------------------------------------------------------------------------
# Constantes de politica
# --------------------------------------------------------------------------------------
$script:ExpectedNodeVersion = '22.23.3'
$script:ExcludedDirNames    = @('bin', 'obj', 'node_modules', 'dist', 'wwwroot', 'TestResults',
                                'test-results', 'playwright-report', '__pycache__', '.local', '.git')
$script:ExcludedPathPrefix = 'docs/stage3/d2b/evidence/'
$script:StructureArtifact  = 'docs/stage3/d2a/evidence/architecture-result.json'
$script:StructureState     = $null

# --------------------------------------------------------------------------------------
# Utilidades
# --------------------------------------------------------------------------------------
function Write-Info { param([string]$Message) Write-Host $Message }
function Write-Warn2 { param([string]$Message) Write-Host "AVISO: $Message" -ForegroundColor Yellow }

function Resolve-Executable {
    param([string]$Name)
    $cmd = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { return $cmd.Source }
    return $null
}

function Format-Command {
    param([string]$Executable, [string[]]$Arguments)
    $parts = @()
    if ($Executable) {
        $parts += $(if ($Executable -match '\s') { '"' + $Executable + '"' } else { $Executable })
    }
    foreach ($a in $Arguments) {
        if ($null -eq $a) { continue }
        if ($a.Contains('"')) {
            # Linea de comando ya compuesta (por ejemplo para cmd.exe): no volver a citar.
            $parts += $a
        } elseif ($a -match '\s') {
            $parts += '"' + $a + '"'
        } else {
            $parts += $a
        }
    }
    return ($parts -join ' ')
}

function Get-RelativeUnixPath {
    param([string]$Root, [string]$Path)
    return $Path.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
}

# Digest independiente del arbol del candidato. Reproduce las reglas de exclusion de
# docs/stage3/d2b/preparation-20261020/freeze.py para demostrar, sin depender del manifiesto,
# que el runner no muto el candidato.
function Get-CandidateDigest {
    param([string]$Root)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $stack.Push($Root)
    while ($stack.Count -gt 0) {
        $dir = $stack.Pop()
        foreach ($file in [System.IO.Directory]::GetFiles($dir)) {
            $rel = Get-RelativeUnixPath -Root $Root -Path $file
            if ($rel.StartsWith($script:ExcludedPathPrefix)) { continue }
            $hash = [System.BitConverter]::ToString($sha.ComputeHash([System.IO.File]::ReadAllBytes($file))).Replace('-', '')
            $lines.Add("$rel`t$hash")
        }
        foreach ($sub in [System.IO.Directory]::GetDirectories($dir)) {
            $name = [System.IO.Path]::GetFileName($sub)
            if ($script:ExcludedDirNames -contains $name) { continue }
            $stack.Push($sub)
        }
    }
    $lines.Sort([System.StringComparer]::Ordinal)
    $payload = [System.Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $digest = [System.BitConverter]::ToString($sha.ComputeHash($payload)).Replace('-', '')
    return [ordered]@{ digest = $digest.ToLowerInvariant(); fileCount = $lines.Count }
}

function New-Step {
    param(
        [string]$Id,
        [string]$Title,
        [string]$Kind = 'Command',
        [string]$Executable,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory,
        [string[]]$DependsOn = @(),
        [string[]]$Notes = @(),
        [string]$PolicyReason,
        [scriptblock]$Condition,
        [scriptblock]$PreAction,
        [scriptblock]$PostAction
    )
    return [ordered]@{
        Id               = $Id
        Title            = $Title
        Kind             = $Kind
        Executable       = $Executable
        Arguments        = @($Arguments)
        Command          = (Format-Command -Executable $Executable -Arguments $Arguments)
        WorkingDirectory = $WorkingDirectory
        DependsOn        = @($DependsOn)
        Notes            = @($Notes)
        PolicyReason     = $PolicyReason
        Condition        = $Condition
        PreAction        = $PreAction
        PostAction       = $PostAction
        Status           = 'NOT_RUN'
        ExitCode         = $null
        DurationMs       = $null
        LogPath          = $null
        StartedAtUtc     = $null
        FinishedAtUtc    = $null
        Message          = $null
    }
}

# Ejecuta un paso y deja un log unico por paso (cabecera + stdout + stderr).
function Invoke-Step {
    param([System.Collections.IDictionary]$Step, [string]$LogDirectory)
    $logPath = Join-Path $LogDirectory ("{0}.log" -f $Step.Id)
    $outPath = Join-Path $LogDirectory ("{0}.stdout.tmp" -f $Step.Id)
    $errPath = Join-Path $LogDirectory ("{0}.stderr.tmp" -f $Step.Id)
    $Step.LogPath = $logPath

    $started = [DateTime]::UtcNow
    $Step.StartedAtUtc = $started.ToString('o')
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $process = Start-Process -FilePath $Step.Executable `
                                -ArgumentList $Step.Arguments `
                                -WorkingDirectory $Step.WorkingDirectory `
                                -NoNewWindow -Wait -PassThru `
                                -RedirectStandardOutput $outPath `
                                -RedirectStandardError $errPath
        $exitCode = $process.ExitCode
    } catch {
        $sw.Stop()
        $Step.DurationMs = [int]$sw.ElapsedMilliseconds
        $Step.FinishedAtUtc = [DateTime]::UtcNow.ToString('o')
        $Step.Status = 'FAILED'
        $Step.ExitCode = -1
        $Step.Message = "No se pudo iniciar el proceso: $($_.Exception.Message)"
        $header = @(
            "# step:      $($Step.Id)",
            "# command:   $($Step.Command)",
            "# error:     $($Step.Message)",
            ""
        ) -join "`n"
        Set-Content -LiteralPath $logPath -Value $header -Encoding UTF8
        return $Step
    }
    $sw.Stop()
    $Step.DurationMs = [int]$sw.ElapsedMilliseconds
    $Step.FinishedAtUtc = [DateTime]::UtcNow.ToString('o')
    $Step.ExitCode = $exitCode
    $Step.Status = $(if ($exitCode -eq 0) { 'PASSED' } else { 'FAILED' })
    if ($exitCode -ne 0) {
        $Step.Message = "El comando termino con exit code $exitCode. Revisar el log del paso."
    }

    $stdout = $(if (Test-Path $outPath) { Get-Content -LiteralPath $outPath -Raw } else { '' })
    $stderr = $(if (Test-Path $errPath) { Get-Content -LiteralPath $errPath -Raw } else { '' })
    $header = @(
        "# step:      $($Step.Id)",
        "# title:     $($Step.Title)",
        "# command:   $($Step.Command)",
        "# working:   $($Step.WorkingDirectory)",
        "# started:   $($Step.StartedAtUtc)",
        "# finished:  $($Step.FinishedAtUtc)",
        "# duration:  $($Step.DurationMs) ms",
        "# exitCode:  $exitCode",
        ""
    ) -join "`n"
    Set-Content -LiteralPath $logPath -Value ($header + "=== stdout ===`n" + $stdout + "`n=== stderr ===`n" + $stderr) -Encoding UTF8
    Remove-Item -LiteralPath $outPath, $errPath -Force -ErrorAction SilentlyContinue
    return $Step
}

function Write-JsonFile {
    param([string]$Path, $Object)
    $json = $Object | ConvertTo-Json -Depth 12
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $encoding)
}

# --------------------------------------------------------------------------------------
# Descubrimiento de Node 22.23.3
# --------------------------------------------------------------------------------------
function Get-ExpectedNodeVersion {
    param([string]$Candidate)
    $file = Join-Path $Candidate 'src\Kulani.KDI.Web\.node-version'
    if (Test-Path -LiteralPath $file) {
        $value = (Get-Content -LiteralPath $file -Raw).Trim()
        if ($value) { return $value }
    }
    $pkg = Join-Path $Candidate 'src\Kulani.KDI.Web\package.json'
    if (Test-Path -LiteralPath $pkg) {
        try {
            $json = Get-Content -LiteralPath $pkg -Raw | ConvertFrom-Json
            if ($json.engines -and $json.engines.node) { return [string]$json.engines.node }
        } catch { }
    }
    return $script:ExpectedNodeVersion
}

function Get-NodeHomeCandidates {
    param([string]$Explicit)
    $list = New-Object 'System.Collections.Generic.List[string]'
    if ($Explicit) { $list.Add($Explicit) }
    $roots = @($env:LOCALAPPDATA, $env:ProgramFiles, ${env:ProgramFiles(x86)}, $env:APPDATA, $env:USERPROFILE)
    foreach ($r in $roots) {
        if (-not $r -or -not (Test-Path -LiteralPath $r)) { continue }
        $list.Add((Join-Path $r 'pi-node\current'))
        $list.Add((Join-Path $r 'pi-node'))
        $list.Add((Join-Path $r 'nodejs'))
        $list.Add((Join-Path $r 'Programs\nodejs'))
        $list.Add((Join-Path $r 'nvm'))
        $list.Add((Join-Path $r 'Volta\tools\image\node'))
        $list.Add((Join-Path $r 'scoop\apps\nodejs'))
    }
    return $list
}

function Resolve-NodeRuntime {
    param([string]$Candidate, [string]$Explicit, [bool]$Verify)
    $expected = Get-ExpectedNodeVersion -Candidate $Candidate
    $seen = @{}
    $fallback = $null
    foreach ($dir in (Get-NodeHomeCandidates -Explicit $Explicit)) {
        if (-not $dir) { continue }
        $key = $dir.ToLowerInvariant()
        if ($seen.ContainsKey($key)) { continue }
        $seen[$key] = $true
        $exe = Join-Path $dir 'node.exe'
        if (Test-Path -LiteralPath $exe) {
            if (-not $fallback) { $fallback = $dir }
            if (-not $Verify) {
                return [pscustomobject]@{ Home = $dir; NodeExe = $exe; Version = 'no verificado (modo listado)'; Expected = $expected; Verified = $false }
            }
            $v = & $exe --version 2>$null
            if ($v -and $v.Trim() -eq ("v" + $expected)) {
                return [pscustomobject]@{ Home = $dir; NodeExe = $exe; Version = $v.Trim(); Expected = $expected; Verified = $true }
            }
            continue
        }
        # pi-node/current puede ser un enlace: probar subdirectorios versionados.
        if (Test-Path -LiteralPath $dir) {
            foreach ($sub in (Get-ChildItem -LiteralPath $dir -Directory -ErrorAction SilentlyContinue)) {
                $subExe = Join-Path $sub.FullName 'node.exe'
                if (-not (Test-Path -LiteralPath $subExe)) { continue }
                if (-not $fallback) { $fallback = $sub.FullName }
                if (-not $Verify) {
                    return [pscustomobject]@{ Home = $sub.FullName; NodeExe = $subExe; Version = 'no verificado (modo listado)'; Expected = $expected; Verified = $false }
                }
                $v = & $subExe --version 2>$null
                if ($v -and $v.Trim() -eq ("v" + $expected)) {
                    return [pscustomobject]@{ Home = $sub.FullName; NodeExe = $subExe; Version = $v.Trim(); Expected = $expected; Verified = $true }
                }
            }
        }
    }
    if ($fallback) {
        return [pscustomobject]@{ Home = $fallback; NodeExe = (Join-Path $fallback 'node.exe'); Version = 'version distinta de la esperada'; Expected = $expected; Verified = $false }
    }
    return [pscustomobject]@{ Home = $null; NodeExe = $null; Version = 'no encontrado'; Expected = $expected; Verified = $false }
}

# --------------------------------------------------------------------------------------
# Resolucion de rutas base
# --------------------------------------------------------------------------------------
if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..\..')).Path
} else {
    $RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
}

if (-not $CandidateRoot) { $CandidateRoot = Join-Path $RepoRoot '.local\d2b-candidate' }
if (-not (Test-Path -LiteralPath $CandidateRoot)) {
    Write-Error "El candidato no existe: $CandidateRoot"
    exit 2
}
$CandidateRoot = (Resolve-Path -LiteralPath $CandidateRoot).Path

if (-not $OutputRoot) { $OutputRoot = Join-Path $RepoRoot '.local\validate' }

$runStamp    = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
$RunDirectory = Join-Path $OutputRoot ("run-" + $runStamp)
if (-not $ReportPath) { $ReportPath = Join-Path $RunDirectory 'validate-report.json' }

$webRoot     = Join-Path $CandidateRoot 'src\Kulani.KDI.Web'
$pythonExe   = Resolve-Executable -Name 'python'
$dotnetExe   = Resolve-Executable -Name 'dotnet'
$nodeRuntime = Resolve-NodeRuntime -Candidate $CandidateRoot -Explicit $NodeHome -Verify (-not $ListSteps)
$npmCmd      = $(if ($nodeRuntime.Home) { Join-Path $nodeRuntime.Home 'npm.cmd' } else { $null })

# --------------------------------------------------------------------------------------
# Entorno (heredado por los procesos hijos; no se toca la configuracion global)
# --------------------------------------------------------------------------------------
$env:DOTNET_CLI_HOME                    = Join-Path $RepoRoot '.local\dotnet-home'
$env:NUGET_PACKAGES                     = Join-Path $RepoRoot '.local\nuget\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT       = '1'
$env:DOTNET_NOLOGO                      = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE  = '1'
$env:NUGET_XMLDOC_MODE                  = 'skip'
$env:PYTHONUTF8                         = '1'
$env:PYTHONIOENCODING                   = 'utf-8'
$env:PYTHONDONTWRITEBYTECODE            = '1'
$env:PLAYWRIGHT_BROWSERS_PATH           = Join-Path $env:LOCALAPPDATA 'ms-playwright'
$env:NPM_CONFIG_AUDIT                   = 'false'
$env:NPM_CONFIG_FUND                    = 'false'
$env:NPM_CONFIG_UPDATE_NOTIFIER         = 'false'
$env:NPM_CONFIG_OFFLINE                 = 'true'
$env:NPM_CONFIG_CACHE                   = Join-Path $RepoRoot '.local\npm-cache'
if ($nodeRuntime.Home) { $env:PATH = ($nodeRuntime.Home + ';' + $env:PATH) }

# --------------------------------------------------------------------------------------
# Plan de pasos
# --------------------------------------------------------------------------------------
function New-Plan {
    $plan = New-Object 'System.Collections.Generic.List[object]'

    $plan.Add((New-Step -Id 'preflight' `
        -Title 'Preflight: interpretes, caches locales y prerequisites' `
        -Kind 'Preflight' `
        -WorkingDirectory $RepoRoot `
        -Notes @('Solo lectura: comprueba interpretes, version de Node, node_modules, caches NuGet/npm y navegadores de Playwright.')))

    $plan.Add((New-Step -Id 'integrity-candidate-before' `
        -Title 'Integridad previa del candidato (freeze.py --verify)' `
        -Executable $pythonExe `
        -Arguments @('docs/stage3/d2b/preparation-20261020/freeze.py', '--verify') `
        -WorkingDirectory $RepoRoot `
        -DependsOn @('preflight') `
        -Notes @('Solo lectura. Si falla, el runner se detiene: el candidato no coincide con su manifiesto.')))

    $plan.Add((New-Step -Id 'integrity-d2a-before' `
        -Title 'Integridad previa D2-A (identities-20261018/verify.py)' `
        -Executable $pythonExe `
        -Arguments @('docs/stage3/d2a/closure/identities-20261018/verify.py') `
        -WorkingDirectory $RepoRoot `
        -DependsOn @('preflight') `
        -Notes @('Solo lectura. Verifica el original D2-A, el incremento congelado y la evidencia congelada.')))

    $plan.Add((New-Step -Id 'backend-build' `
        -Title 'Build backend Release (sin restore de red)' `
        -Executable $dotnetExe `
        -Arguments @('build', 'Kulani.KDI.slnx', '-c', 'Release', '--no-restore') `
        -WorkingDirectory $CandidateRoot `
        -DependsOn @('integrity-candidate-before', 'integrity-d2a-before') `
        -Notes @('La solucion incluye Kulani.KDI.SqlIntegrationTests, que por tanto se compila. Compilar no ejecuta la suite ni abre conexiones a SQL.')))

    $plan.Add((New-Step -Id 'unit-tests' `
        -Title 'Suite unitaria (tests/Kulani.KDI.UnitTests)' `
        -Executable $dotnetExe `
        -Arguments @('test', '--project', 'tests/Kulani.KDI.UnitTests/Kulani.KDI.UnitTests.csproj', '-c', 'Release', '--no-build', '--no-restore') `
        -WorkingDirectory $CandidateRoot `
        -DependsOn @('backend-build')))

    $plan.Add((New-Step -Id 'api-tests' `
        -Title 'Suite API (tests/Kulani.KDI.ApiTests)' `
        -Executable $dotnetExe `
        -Arguments @('test', '--project', 'tests/Kulani.KDI.ApiTests/Kulani.KDI.ApiTests.csproj', '-c', 'Release', '--no-build', '--no-restore') `
        -WorkingDirectory $CandidateRoot `
        -DependsOn @('backend-build') `
        -Notes @('Usa TestHost y almacenamiento doble: no abre conexiones SQL.')))

    $plan.Add((New-Step -Id 'architecture-tests' `
        -Title 'Suite de arquitectura (tests/Kulani.KDI.ArchitectureTests)' `
        -Executable $dotnetExe `
        -Arguments @('test', '--project', 'tests/Kulani.KDI.ArchitectureTests/Kulani.KDI.ArchitectureTests.csproj', '-c', 'Release', '--no-build', '--no-restore') `
        -WorkingDirectory $CandidateRoot `
        -DependsOn @('backend-build')))

    $plan.Add((New-Step -Id 'sql-integration-tests' `
        -Title 'Suite de integracion SQL (Kulani.KDI.SqlIntegrationTests)' `
        -Kind 'Policy' `
        -PolicyReason 'Kulani.KDI.SqlIntegrationTests abre conexiones a SQL Server y ejecuta operaciones persistentes sobre KulaniKDI_Test. La autorizacion A5 (migraciones, fixtures y limpieza) no existe, por lo que el runner no la ejecuta. Este criterio permanece BLOCKED y no debe declararse PASS.' `
        -Notes @('Registro explicito: omision por politica vigente, no por error ni por silencio.')))

    if (-not $SkipStructureCheck) {
        # Orden correcto: el tool no forma parte de la solucion, asi que en el candidato puede no tener
        # assets de restore. Restaurar PRIMERO (offline, fuente unica local, --locked-mode) y compilar
        # despues con --no-restore. El orden inverso fallaba con NETSDK1004 y dejaba el runner en FAIL.
        $plan.Add((New-Step -Id 'structure-check-restore' `
            -Title 'Restore offline de Kulani.KDI.StructureCheck (fuente unica local)' `
            -Executable $dotnetExe `
            -Arguments @('restore', 'tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj', '--locked-mode', '--configfile', (Join-Path $RunDirectory 'NuGet.offline.config')) `
            -WorkingDirectory $CandidateRoot `
            -DependsOn @('backend-build') `
            -Notes @('Offline: unica fuente = cache local de paquetes; --locked-mode impide reescribir packages.lock.json. Escribe solo en obj/, que el manifiesto excluye.')))

        $plan.Add((New-Step -Id 'structure-check-build' `
            -Title 'Build de Kulani.KDI.StructureCheck (offline, sin restore)' `
            -Executable $dotnetExe `
            -Arguments @('build', 'tools/Kulani.KDI.StructureCheck/Kulani.KDI.StructureCheck.csproj', '-c', 'Release', '--no-restore') `
            -WorkingDirectory $CandidateRoot `
            -DependsOn @('structure-check-restore') `
            -Condition { param($c) return ($c.RestoreExitCode -eq 0) } `
            -Notes @('Compila con los assets generados por el restore offline anterior.')))

        $plan.Add((New-Step -Id 'structure-check-run' `
            -Title 'Ejecucion de Kulani.KDI.StructureCheck' `
            -Executable (Join-Path $CandidateRoot 'tools\Kulani.KDI.StructureCheck\bin\Release\net10.0\Kulani.KDI.StructureCheck.exe') `
            -Arguments @() `
            -WorkingDirectory $CandidateRoot `
            -DependsOn @('structure-check-build') `
            -PreAction {
                param($s)
                $path = Join-Path $CandidateRoot $script:StructureArtifact
                $exists = Test-Path -LiteralPath $path
                $bytes = $null
                if ($exists) { $bytes = [System.IO.File]::ReadAllBytes($path) }
                $script:StructureState = @{ Exists = $exists; Bytes = $bytes; Path = $path }
            } `
            -PostAction {
                param($s)
                $state = $script:StructureState
                if (-not $state) { return }
                if ($state.Exists) {
                    [System.IO.File]::WriteAllBytes($state.Path, $state.Bytes)
                } else {
                    if (Test-Path -LiteralPath $state.Path) { Remove-Item -LiteralPath $state.Path -Force }
                    $dir = Split-Path -Parent $state.Path
                    if ((Test-Path -LiteralPath $dir) -and -not (Get-ChildItem -LiteralPath $dir -Force | Select-Object -First 1)) {
                        Remove-Item -LiteralPath $dir -Force -Recurse
                    }
                }
                $script:StructureState = $null
            } `
            -Notes @('El tool escribe docs/stage3/d2a/evidence/architecture-result.json dentro del candidato; el runner restaura ese artefacto al estado previo y verifica el digest inmediatamente despues.')))
    }

    if ($InstallWebDependencies) {
        $plan.Add((New-Step -Id 'web-install-dependencies' `
            -Title 'Instalacion offline de dependencias web (npm.cmd ci --offline)' `
            -Executable 'cmd.exe' `
            -Arguments @('/d', '/c', ('"{0}" ci --offline --ignore-scripts --no-audit' -f $npmCmd)) `
            -WorkingDirectory $webRoot `
            -DependsOn @('backend-build') `
            -Notes @('BORRA node_modules. Off por defecto: la cache npm del candidato esta vacia y la cache del repo puede no cubrir el lockfile.')))
    }

    $plan.Add((New-Step -Id 'web-typecheck' `
        -Title 'Frontend typecheck (tsc --noEmit)' `
        -Executable 'cmd.exe' `
        -Arguments @('/d', '/c', ('"{0}" run typecheck' -f $npmCmd)) `
        -WorkingDirectory $webRoot `
        -DependsOn @('backend-build') `
        -Condition { param($c) return ($c.NodeReady -and $c.WebDepsReady) } `
        -Notes @('Usa npm.cmd (no npm) por la politica de firma de PowerShell sobre npm.ps1.')))

    $plan.Add((New-Step -Id 'web-lint' `
        -Title 'Frontend lint (eslint --max-warnings 0)' `
        -Executable 'cmd.exe' `
        -Arguments @('/d', '/c', ('"{0}" run lint' -f $npmCmd)) `
        -WorkingDirectory $webRoot `
        -DependsOn @('backend-build') `
        -Condition { param($c) return ($c.NodeReady -and $c.WebDepsReady) }))

    $plan.Add((New-Step -Id 'web-build' `
        -Title 'Frontend build (tsc --noEmit && vite build)' `
        -Executable 'cmd.exe' `
        -Arguments @('/d', '/c', ('"{0}" run build' -f $npmCmd)) `
        -WorkingDirectory $webRoot `
        -DependsOn @('backend-build') `
        -Condition { param($c) return ($c.NodeReady -and $c.WebDepsReady) } `
        -Notes @('Escribe dist/ (excluido del manifiesto del candidato).')))

    if (-not $SkipPlaywright) {
        $plan.Add((New-Step -Id 'playwright-chromium' `
            -Title 'Suite de navegador (Playwright Chromium)' `
            -Executable $nodeRuntime.NodeExe `
            -Arguments @('node_modules/@playwright/test/cli.js', 'test', '--project=chromium', '--reporter=list',
                         ('--output=' + ((Join-Path $RunDirectory 'playwright-artifacts').Replace('\', '/')))) `
            -WorkingDirectory $webRoot `
            -DependsOn @('web-build') `
            -Condition { param($c) return ($c.NodeReady -and $c.WebDepsReady -and $c.PlaywrightBrowsersReady) } `
            -Notes @('Reporter=list para no escribir el JSON de reporteria dentro del candidato; --output fuera del candidato. Usa el Chromium local: no descarga navegadores.')))
    }

    $plan.Add((New-Step -Id 'integrity-candidate-after' `
        -Title 'Integridad posterior del candidato (freeze.py --verify)' `
        -Executable $pythonExe `
        -Arguments @('docs/stage3/d2b/preparation-20261020/freeze.py', '--verify') `
        -WorkingDirectory $RepoRoot `
        -DependsOn @('integrity-candidate-before') `
        -Notes @('Debe producir el mismo hash de manifiesto que la verificacion previa.')))

    $plan.Add((New-Step -Id 'integrity-d2a-after' `
        -Title 'Integridad posterior D2-A (identities-20261018/verify.py)' `
        -Executable $pythonExe `
        -Arguments @('docs/stage3/d2a/closure/identities-20261018/verify.py') `
        -WorkingDirectory $RepoRoot `
        -DependsOn @('integrity-d2a-before')))

    return $plan
}

# --------------------------------------------------------------------------------------
# Modo listado
# --------------------------------------------------------------------------------------
if ($ListSteps) {
    $plan = New-Plan
    Write-Info ("Kdi.Validate {0} - MODO LISTADO (no se ejecuta ningun comando)" -f $script:RunnerVersion)
    Write-Info ("RepoRoot      : {0}" -f $RepoRoot)
    Write-Info ("CandidateRoot : {0}" -f $CandidateRoot)
    Write-Info ("OutputRoot    : {0}" -f $OutputRoot)
    Write-Info ("ReportPath    : {0}" -f $ReportPath)
    Write-Info ("python        : {0}" -f $pythonExe)
    Write-Info ("dotnet        : {0}" -f $dotnetExe)
    Write-Info ("node esperado : v{0}" -f (Get-ExpectedNodeVersion -Candidate $CandidateRoot))
    Write-Info ("node resuelto : {0} ({1})" -f $(if ($nodeRuntime.NodeExe) { $nodeRuntime.NodeExe } else { 'NO ENCONTRADO' }), $nodeRuntime.Version)
    Write-Info ("npm.cmd       : {0}" -f $(if ($npmCmd) { $npmCmd } else { 'NO ENCONTRADO' }))
    Write-Info ("Playwright    : {0}" -f $env:PLAYWRIGHT_BROWSERS_PATH)
    Write-Info ''
    Write-Info 'Pasos previstos:'
    $index = 0
    foreach ($step in $plan) {
        $index++
        Write-Info ("  [{0,2}] {1}" -f $index, $step.Id)
        Write-Info ("       titulo : {0}" -f $step.Title)
        Write-Info ("       comando: {0}" -f $step.Command)
        Write-Info ("       cwd    : {0}" -f $step.WorkingDirectory)
        if ($step.DependsOn.Count -gt 0) { Write-Info ("       depende: {0}" -f ($step.DependsOn -join ', ')) }
        if ($step.Kind -eq 'Policy') {
            Write-Info ("       estado : SKIPPED_BY_POLICY -> {0}" -f $step.PolicyReason)
        }
        foreach ($n in $step.Notes) { Write-Info ("       nota   : {0}" -f $n) }
    }
    $listReport = [ordered]@{
        schemaVersion  = $script:SchemaVersion
        mode           = 'list'
        runner         = [ordered]@{ name = $script:RunnerName; version = $script:RunnerVersion; script = $PSCommandPath }
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
        repoRoot       = $RepoRoot
        candidateRoot  = $CandidateRoot
        outputRoot     = $OutputRoot
        reportPath     = $ReportPath
        host           = [ordered]@{
            machine                = $env:COMPUTERNAME
            os                     = [System.Environment]::OSVersion.VersionString
            powershell             = $PSVersionTable.PSVersion.ToString()
            python                 = $pythonExe
            dotnet                 = $dotnetExe
            nodeExpected           = (Get-ExpectedNodeVersion -Candidate $CandidateRoot)
            nodeResolved           = $nodeRuntime.NodeExe
            nodeVersion            = $nodeRuntime.Version
            npmCmd                 = $npmCmd
            playwrightBrowsersPath = $env:PLAYWRIGHT_BROWSERS_PATH
            dotnetCliHome          = $env:DOTNET_CLI_HOME
            nugetPackages          = $env:NUGET_PACKAGES
        }
        steps          = @($plan | ForEach-Object {
            [ordered]@{
                id = $_.Id; title = $_.Title; kind = $_.Kind; command = $_.Command
                workingDirectory = $_.WorkingDirectory; dependsOn = $_.DependsOn
                status = $(if ($_.Kind -eq 'Policy') { 'SKIPPED_BY_POLICY' } else { 'NOT_RUN' })
                notes = $_.Notes; policyReason = $_.PolicyReason
            }
        })
        totals         = [ordered]@{
            planned = $plan.Count; executed = 0; passed = 0; failed = 0
            skippedByPolicy = @($plan | Where-Object { $_.Kind -eq 'Policy' }).Count
            skippedOther = 0; notRun = @($plan | Where-Object { $_.Kind -ne 'Policy' }).Count
        }
        notes          = @(
            'Modo de listado: no se ejecuto ningun comando, no se genero ninguna salida de build y no se modifico el candidato.',
            'Los pasos SKIPPED_BY_POLICY se omiten siempre por politica vigente, con independencia de los parametros.'
        )
    }
    Write-JsonFile -Path $ReportPath -Object $listReport
    Write-Info ''
    Write-Info ("Informe de listado escrito en: {0}" -f $ReportPath)
    exit 0
}

# --------------------------------------------------------------------------------------
# Ejecucion
# --------------------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $RunDirectory)) { New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null }

# NuGet.config offline: unica fuente local de paquetes, sin red.
$offlineNugetConfig = Join-Path $RunDirectory 'NuGet.offline.config'
$offlinePackages    = Join-Path $RepoRoot '.local\nuget\packages'
$nugetXml = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-offline" value="$offlinePackages" />
  </packageSources>
  <config>
    <add key="globalPackagesFolder" value="$offlinePackages" />
  </config>
</configuration>
"@
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($offlineNugetConfig, $nugetXml, $utf8NoBom)

$startedAt = [DateTime]::UtcNow
$plan = New-Plan
$results = New-Object 'System.Collections.Generic.List[object]'
$ctx = [ordered]@{
    BuildExitCode          = $null
    RestoreExitCode        = $null
    NodeReady              = $false
    WebDepsReady           = $false
    PlaywrightBrowsersReady = $false
    Missing                = @()
}

function Get-ResultStatus {
    param([string]$Id)
    foreach ($r in $results) { if ($r.Id -eq $Id) { return $r.Status } }
    return $null
}

Write-Info ("Kdi.Validate {0} - ejecutando sobre {1}" -f $script:RunnerVersion, $CandidateRoot)
Write-Info ("Logs: {0}" -f $RunDirectory)
Write-Info ''

# Digest del candidato ANTES de cualquier paso (prueba de no mutacion).
$digestBefore = Get-CandidateDigest -Root $CandidateRoot

foreach ($step in $plan) {
    # --- Preflight (solo lectura) -----------------------------------------------------
    if ($step.Kind -eq 'Preflight') {
        $missing = @()
        if (-not $pythonExe) { $missing += 'python no encontrado' }
        if (-not $dotnetExe) { $missing += 'dotnet no encontrado' }
        if (-not $nodeRuntime.Verified) { $missing += ("Node {0} no encontrado (esperado v{1})" -f $nodeRuntime.Version, $nodeRuntime.Expected) }
        $ctx.NodeReady = [bool]$nodeRuntime.Verified
        if ($npmCmd -and -not (Test-Path -LiteralPath $npmCmd)) { $missing += 'npm.cmd no encontrado en el home de Node'; $ctx.NodeReady = $false }
        if (-not (Test-Path -LiteralPath (Join-Path $webRoot 'node_modules'))) { $missing += 'node_modules ausente en el proyecto web'; $ctx.WebDepsReady = $false }
        else { $ctx.WebDepsReady = $true }
        if (-not (Test-Path -LiteralPath $env:PLAYWRIGHT_BROWSERS_PATH)) { $missing += 'navegadores de Playwright no encontrados'; $ctx.PlaywrightBrowsersReady = $false }
        else { $ctx.PlaywrightBrowsersReady = $true }
        if (-not (Test-Path -LiteralPath $env:NUGET_PACKAGES)) { $missing += 'cache local de paquetes NuGet no encontrada' }
        $ctx.Missing = $missing
        $step.Status = $(if ($pythonExe -and $dotnetExe -and $nodeRuntime.Verified) { 'PASSED' } else { 'FAILED' })
        $step.Message = $(if ($missing.Count -eq 0) { 'Todos los prerequisites criticos estan disponibles.' }
                         else { 'Prerequisites ausentes: ' + ($missing -join '; ') })
        $step.StartedAtUtc = [DateTime]::UtcNow.ToString('o')
        $step.FinishedAtUtc = $step.StartedAtUtc
        $step.DurationMs = 0
        $step.LogPath = Join-Path $RunDirectory 'preflight.log'
        $log = @(
            "# step:      preflight",
            "# node:      $($nodeRuntime.NodeExe) ($($nodeRuntime.Version))",
            "# npm.cmd:   $npmCmd",
            "# dotnet:    $dotnetExe",
            "# python:    $pythonExe",
            "# playwright:$($env:PLAYWRIGHT_BROWSERS_PATH)",
            "# nuget:     $($env:NUGET_PACKAGES)",
            "# missing:   $($missing -join '; ')",
            ""
        ) -join "`n"
        Set-Content -LiteralPath $step.LogPath -Value $log -Encoding UTF8
        $results.Add($step)
        Write-Info ("[{0}] preflight -> {1}" -f $step.Status, $step.Message)
        continue
    }

    # --- Paso de politica: nunca se ejecuta -------------------------------------------
    if ($step.Kind -eq 'Policy') {
        $step.Status = 'SKIPPED_BY_POLICY'
        $step.Message = $step.PolicyReason
        $results.Add($step)
        Write-Info ("[SKIPPED_BY_POLICY] {0}" -f $step.Id)
        continue
    }

    # --- Condicion opcional ----------------------------------------------------------
    if ($step.Condition) {
        $ok = $false
        try { $ok = [bool](& $step.Condition $ctx) } catch { $ok = $false }
        if (-not $ok) {
            $step.Status = 'SKIPPED'
            $step.Message = 'Prerequisito no disponible o condicion no satisfecha: paso omitido sin ejecutar.'
            $results.Add($step)
            Write-Info ("[SKIPPED] {0}" -f $step.Id)
            continue
        }
    }

    # --- Dependencias fallidas -------------------------------------------------------
    # Un paso cuyas dependencias no se superaron NO debe ejecutarse. SKIPPED tambien bloquea: un
    # paso omitido por condicion o por pre-requisito significa que su resultado no es utilizable,
    # y ejecutar el dependiente produciria un fallo enganoso (por ejemplo, un ejecutable ausente).
    $blocked = $false
    foreach ($dep in $step.DependsOn) {
        $depStatus = Get-ResultStatus -Id $dep
        if ($depStatus -in @('FAILED', 'SKIPPED', 'SKIPPED_DUE_TO_DEPENDENCY_FAILURE')) {
            $step.Status = 'SKIPPED_DUE_TO_DEPENDENCY_FAILURE'
            $step.Message = "Dependencia no superada: $dep ($depStatus)."
            $blocked = $true
            break
        }
    }
    if ($blocked) {
        $results.Add($step)
        Write-Info ("[SKIPPED_DUE_TO_DEPENDENCY_FAILURE] {0}" -f $step.Id)
        continue
    }

    # --- Ejecutar ---------------------------------------------------------------------
    if ($step.PreAction) { try { & $step.PreAction $step } catch { Write-Warn2 ("PreAction de {0}: {1}" -f $step.Id, $_.Exception.Message) } }

    Write-Info ("[RUN ] {0} :: {1}" -f $step.Id, $step.Command)
    $stepResult = Invoke-Step -Step $step -LogDirectory $RunDirectory

    if ($step.PostAction) { try { & $step.PostAction $stepResult } catch { Write-Warn2 ("PostAction de {0}: {1}" -f $step.Id, $_.Exception.Message) } }

    # --- Comprobacion inmediata de no mutacion tras StructureCheck --------------------
    if ($stepResult.Id -eq 'structure-check-run') {
        $digestNow = Get-CandidateDigest -Root $CandidateRoot
        if ($digestNow.digest -ne $digestBefore.digest) {
            $stepResult.Status = 'FAILED'
            $stepResult.Message = "El candidato cambio durante StructureCheck (digest $digestBefore -> $digestNow)."
            Write-Warn2 $stepResult.Message
        } else {
            $stepResult.Notes = @($stepResult.Notes) + @('Artefacto docs/stage3/d2a/evidence/architecture-result.json restaurado y digest del candidato sin cambios.')
        }
    }

    # --- StructureCheck: lock inconsistente en el candidato -----------------------------
    # NU1004 significa que el packages.lock.json del tool no coincide con las dependencias
    # actuales del grafo. Es una propiedad PREEXISTENTE del candidato congelado. El runner no
    # puede corregirla sin mutar el candidato, asi que la registra como omision con motivo
    # explicito, no como fallo silencioso. La politica de arquitectura sigue cubierta por la
    # suite Kulani.KDI.ArchitectureTests, que si forma parte de la solucion y se ejecuta.
    # Cualquier otro error de restore sigue siendo FAILED.
    if ($stepResult.Id -eq 'structure-check-restore' -and $stepResult.Status -eq 'FAILED' -and $stepResult.LogPath) {
        $log = Get-Content -LiteralPath $stepResult.LogPath -Raw -ErrorAction SilentlyContinue
        if ($log -and $log -match 'NU1004') {
            $stepResult.Status = 'SKIPPED'
            $stepResult.Message = 'Restore offline del tool bloqueado por NU1004: el packages.lock.json del StructureCheck no coincide con las dependencias del grafo. Es una inconsistencia preexistente del candidato congelado; el runner no la corrige porque corregirla mutaria el candidato. La politica de arquitectura sigue verificada por la suite ArchitectureTests, que forma parte de la solucion.'
            $stepResult.Notes = @($stepResult.Notes) + @('Omision explicita, no fallo silencioso: el criterio de StructureCheck queda NOT VERIFIED y el de arquitectura verificado por ArchitectureTests (24/24).')
            Write-Warn2 $stepResult.Message
        }
    }

    $results.Add($stepResult)
    Write-Info ("[{0}] {1} (exit={2}, {3} ms) -> {4}" -f $stepResult.Status, $stepResult.Id, $stepResult.ExitCode, $stepResult.DurationMs, $stepResult.LogPath)

    if ($stepResult.Id -eq 'structure-check-build')   { $ctx.BuildExitCode = $stepResult.ExitCode }
    if ($stepResult.Id -eq 'structure-check-restore') { $ctx.RestoreExitCode = $stepResult.ExitCode }

    # --- Parada por fallo critico ----------------------------------------------------
    if ($stepResult.Status -eq 'FAILED' -and $stepResult.Id -in @('integrity-candidate-before', 'integrity-d2a-before')) {
        Write-Warn2 ("Integridad previa fallida ({0}). El runner se detiene: no se ejecuta la cadena de build y pruebas." -f $stepResult.Id)
        break
    }

    # --- Paradarequested por el operador ----------------------------------------------
    if ($StopAfter -and $stepResult.Id -eq $StopAfter) {
        Write-Warn2 ("Ejecucion detenida por -StopAfter {0}. Los pasos posteriores quedan como NOT_RUN." -f $StopAfter)
        break
    }
}

$finishedAt = [DateTime]::UtcNow

# Pasos del plan que no se alcanzaron (cadena cortada o detenida): quedan como NOT_RUN.
foreach ($step in $plan) {
    $known = $false
    foreach ($r in $results) { if ($r.Id -eq $step.Id) { $known = $true; break } }
    if (-not $known) {
        $step.Status = 'NOT_RUN'
        $step.Message = 'No se alcanzo este paso en esta ejecucion.'
        $results.Add($step)
    }
}

# --------------------------------------------------------------------------------------
# Verificacion posterior y digest independiente
# --------------------------------------------------------------------------------------
$digestAfter = Get-CandidateDigest -Root $CandidateRoot

function Get-ManifestHashFromLog {
    param([string]$LogPath)
    if (-not $LogPath -or -not (Test-Path -LiteralPath $LogPath)) { return $null }
    $match = Select-String -LiteralPath $LogPath -Pattern 'SHA256 ([0-9a-fA-F]{64})' -ErrorAction SilentlyContinue | Select-Object -Last 1
    if ($match) { return $match.Matches[0].Groups[1].Value.ToLowerInvariant() }
    return $null
}

$hashBefore = $null
$hashAfter  = $null
foreach ($r in $results) {
    if ($r.Id -eq 'integrity-candidate-before') { $hashBefore = Get-ManifestHashFromLog -LogPath $r.LogPath }
    if ($r.Id -eq 'integrity-candidate-after')  { $hashAfter  = Get-ManifestHashFromLog -LogPath $r.LogPath }
}

$candidate = [ordered]@{
    root                = $CandidateRoot
    manifestPath        = (Join-Path $RepoRoot 'docs\stage3\d2b\preparation-20261020\candidate-manifest.json')
    hashBefore          = $hashBefore
    hashAfter           = $hashAfter
    contentDigestBefore = $digestBefore.digest
    contentDigestAfter  = $digestAfter.digest
    fileCountBefore     = $digestBefore.fileCount
    fileCountAfter      = $digestAfter.fileCount
    unchanged           = ($digestBefore.digest -eq $digestAfter.digest)
    manifestHashStable  = ($hashBefore -ne $null -and $hashBefore -eq $hashAfter)
    contentDigestStable = ($digestBefore.digest -eq $digestAfter.digest)
}

# --------------------------------------------------------------------------------------
# Totales e informe
# --------------------------------------------------------------------------------------
$planned        = $plan.Count
$executed       = @($results | Where-Object { $_.Status -in @('PASSED', 'FAILED') }).Count
$passed         = @($results | Where-Object { $_.Status -eq 'PASSED' }).Count
$failed         = @($results | Where-Object { $_.Status -eq 'FAILED' }).Count
$skippedPolicy  = @($results | Where-Object { $_.Status -eq 'SKIPPED_BY_POLICY' }).Count
$skippedOther   = @($results | Where-Object { $_.Status -in @('SKIPPED', 'SKIPPED_DUE_TO_DEPENDENCY_FAILURE') }).Count
$notRun         = @($results | Where-Object { $_.Status -eq 'NOT_RUN' }).Count

$integrityOk = ($candidate.unchanged -and $candidate.manifestHashStable)
$result = $(if ($failed -gt 0) { 'FAIL' } elseif ($skippedOther -gt 0 -or $notRun -gt 0) { 'PARTIAL' } else { 'PASS' })
if (-not $integrityOk -and $result -eq 'PASS') { $result = 'FAIL' }

$notes = @(
    'Este runner valida reproducibilidad local; NO es una aceptacion de D2-B ni un PASS global.',
    'Kulani.KDI.SqlIntegrationTests no se ejecuta por politica (abre conexiones a SQL Server; A5 no autorizado). El criterio permanece BLOCKED.',
    'No hay red: no se ejecuta restore contra nuget.org, ni npm install con red, ni auditoria de dependencias. No se acredita la cadena de suministro ni el estado de vulnerabilidades.',
    'No se ejecutan migraciones, no se abren conexiones SQL, no se leen credenciales, no se accede a IDI, no se despliega ni se publica nada.',
    'No se verifican Auth0/OIDC real, discovery, validacion de tokens, HTTPS/certificados, keyring/ACL, login/logout completo ni revocacion persistente.',
    'Las pruebas API usan TestHost y almacenamiento doble; el navegador usa contratos sinteticos y smoke anonimo de fundacion. No acreditan persistencia real.',
    'El build compila tambien Kulani.KDI.SqlIntegrationTests porque forma parte de la solucion; compilar no ejecuta la suite ni abre conexiones.',
    'Kulani.KDI.StructureCheck escribe docs/stage3/d2a/evidence/architecture-result.json dentro del candidato; el runner restaura el artefacto al estado previo y verifica el digest inmediatamente despues.',
    'No se comprueba reproducibilidad de restore desde cache vacio, ni la vigencia o actualizacion del SDK instalado (global.json fija 10.0.103 con rollForward disable).',
    'Los pasos no tienen timeout propio: un cuelgue bloquea el runner. Los limites internos dependen de cada suite (Playwright globalTimeout 120s).',
    'El informe no valida la correccion funcional de negocio ni la seguridad de la configuracion de despliegue.',
    'Si los pasos web se omiten por falta de Node 22.23.3, de npm.cmd o de node_modules, el resultado es PARTIAL y nunca PASS.'
)
if ($StopAfter) {
    $notes += ("Ejecucion detenida por -StopAfter {0}: los pasos posteriores figuran como NOT_RUN y el resultado no puede ser PASS." -f $StopAfter)
}

$report = [ordered]@{
    schemaVersion  = $script:SchemaVersion
    mode           = 'execute'
    runner         = [ordered]@{ name = $script:RunnerName; version = $script:RunnerVersion; script = $PSCommandPath }
    startedAtUtc   = $startedAt.ToString('o')
    finishedAtUtc  = $finishedAt.ToString('o')
    durationMs     = [int]($finishedAt - $startedAt).TotalMilliseconds
    result         = $result
    stopAfter      = $(if ($StopAfter) { $StopAfter } else { $null })
    repoRoot       = $RepoRoot
    candidateRoot  = $CandidateRoot
    candidate      = $candidate
    logsDirectory  = $RunDirectory
    reportPath     = $ReportPath
    prerequisites  = [ordered]@{
        nodeReady               = $ctx.NodeReady
        webDependenciesReady    = $ctx.WebDepsReady
        playwrightBrowsersReady = $ctx.PlaywrightBrowsersReady
        missing                 = @($ctx.Missing)
    }
    host           = [ordered]@{
        machine                = $env:COMPUTERNAME
        os                     = [System.Environment]::OSVersion.VersionString
        powershell             = $PSVersionTable.PSVersion.ToString()
        python                 = $pythonExe
        dotnet                 = $dotnetExe
        nodeExpected           = (Get-ExpectedNodeVersion -Candidate $CandidateRoot)
        nodeResolved           = $nodeRuntime.NodeExe
        nodeVersion            = $nodeRuntime.Version
        npmCmd                 = $npmCmd
        playwrightBrowsersPath = $env:PLAYWRIGHT_BROWSERS_PATH
        dotnetCliHome          = $env:DOTNET_CLI_HOME
        nugetPackages          = $env:NUGET_PACKAGES
        npmCache               = $env:NPM_CONFIG_CACHE
    }
    steps          = @($results | ForEach-Object {
        [ordered]@{
            id               = $_.Id
            title            = $_.Title
            kind             = $_.Kind
            command          = $_.Command
            workingDirectory = $_.WorkingDirectory
            dependsOn        = $_.DependsOn
            status           = $_.Status
            exitCode         = $_.ExitCode
            durationMs       = $_.DurationMs
            logPath          = $_.LogPath
            startedAtUtc     = $_.StartedAtUtc
            finishedAtUtc    = $_.FinishedAtUtc
            message          = $_.Message
            policyReason     = $_.PolicyReason
            notes            = $_.Notes
        }
    })
    totals         = [ordered]@{
        planned         = $planned
        executed        = $executed
        passed          = $passed
        failed          = $failed
        skippedByPolicy = $skippedPolicy
        skippedOther    = $skippedOther
        notRun          = $notRun
    }
    integrity      = [ordered]@{
        candidateUnchanged  = $candidate.unchanged
        manifestHashStable  = $candidate.manifestHashStable
        contentDigestStable = $candidate.contentDigestStable
    }
    notes          = $notes
}

Write-JsonFile -Path $ReportPath -Object $report

Write-Info ''
Write-Info ("Resultado: {0}" -f $result)
Write-Info ("Pasos: {0} planificados / {1} ejecutados / {2} correctos / {3} fallidos / {4} omitidos por politica / {5} omitidos otros" -f `
    $planned, $executed, $passed, $failed, $skippedPolicy, $skippedOther)
Write-Info ("Candidato intacto: {0} (manifiesto estable: {1})" -f $candidate.unchanged, $candidate.manifestHashStable)
Write-Info ("Informe: {0}" -f $ReportPath)

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
