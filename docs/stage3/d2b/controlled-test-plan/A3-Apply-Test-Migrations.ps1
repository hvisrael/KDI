<#
    A3-Apply-Test-Migrations.ps1

    Aplica las DOS migraciones preparadas de KulaniKDI_Test, en orden, como `kdi_migrator`
    (autenticacion SQL). La contrasena se introduce en el PROMPT de sqlcmd: no viaja por la
    linea de comandos, ni por variables de entorno, ni por registros, ni por la evidencia.

    Uso, desde una consola Windows interactiva y privada (no redirigida):

        .\docs\stage3\d2b\controlled-test-plan\A3-Apply-Test-Migrations.ps1

    Para comprobar todo ANTES de teclear la contrasena:

        .\docs\stage3\d2b\controlled-test-plan\A3-Apply-Test-Migrations.ps1 -PreflightOnly

    El script:
      0. comprueba que la consola es interactiva (si esta redirigida, se niega);
      1. comprueba que el directorio de evidencia existe y es escribible;
      2. verifica los hashes de los dos artefactos contra el manifiesto revisado;
      3. verifica la integridad del candidato y de D2-A (si falta python, FALLA: no se omite);
      4. comprueba que las tablas D2-B no existen todavia;
      5. demuestra que la sesion de sqlcmd tiene QUOTED_IDENTIFIER ON con -I, y que sin -I
         la comprobacion falla (autocomprobacion con control negativo);
      6. aplica identity, y solo si termina con exito aplica organizations;
      7. crea la semilla sintetica de usuario para N2 (se puede omitir con -SkipSeedUser);
      8. ejecuta la verificacion de solo lectura y guarda todas las salidas como evidencia.

    Si un archivo falla, el script se detiene: NO reintenta, NO edita los scripts y NO aplica
    el segundo modulo. Deja el estado y lo dice.
#>
[CmdletBinding()]
param(
    [switch]$SkipSeedUser,
    [switch]$PreflightOnly,
    [string]$EvidenceDirectory = (Join-Path $PSScriptRoot 'evidence\A1')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- 0. Rutas y consola ----------------------------------------------------------------
# La exigencia de consola privada e interactiva NO se comprueba aqui: solo importa donde
# sqlcmd pide la contrasena. El preflight es de solo lectura y se puede ejecutar en
# cualquier contexto. La comprobacion que bloquea la aplicacion esta justo antes del paso 6.
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$Prepared = Join-Path $Root '.local\d2b-candidate\tools\Kulani.KDI.Migrations\prepared-sql'
$Manifest = Join-Path $Prepared 'manifest.json'
$Identity = Join-Path $Prepared 'KulaniKDI_Test-identity.sql'
$Organizations = Join-Path $Prepared 'KulaniKDI_Test-organizations.sql'
$Seed = Join-Path $PSScriptRoot 'A3-SEED-USER.sql'
$Verify = Join-Path $PSScriptRoot 'A3-VERIFY.sql'
$SetOptions = Join-Path $PSScriptRoot 'A3-SET-OPTIONS.sql'

$Sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
if (-not (Test-Path -LiteralPath $Sqlcmd)) { $Sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source }
if (-not $Sqlcmd) { throw 'sqlcmd not found.' }

foreach ($required in @($Manifest, $Identity, $Organizations, $Seed, $Verify, $SetOptions)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required file not found: $required" }
}

$Stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')

function Write-Step { param([string]$Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Ok { param([string]$Text) Write-Host "    OK  $Text" -ForegroundColor Green }
function Write-Bad { param([string]$Text) Write-Host "    !!  $Text" -ForegroundColor Red }

# Ejecuta sqlcmd capturando la salida, sin que un stderr native aborte el script.
# El codigo de salida es lo que decide; el texto se imprime si algo falla.
function Invoke-Sqlcmd {
    param(
        [string[]]$Arguments,
        [string]$WorkingDirectory = $Root,
        [string]$LogPath,
        [switch]$ExpectFailure,
        [string]$Expectation = ''
    )
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = & $Sqlcmd @Arguments 2>&1
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($LogPath) { $output | Out-File -LiteralPath $LogPath -Encoding utf8 }
    if ($ExpectFailure) {
        if ($code -eq 0) { throw "Expected failure did not occur: $Expectation" }
        Write-Ok "$Expectation (exit $code, as expected)"
        return $output
    }
    if ($code -ne 0) {
        Write-Bad "sqlcmd failed with exit $code"
        $output | Select-Object -Last 12 | ForEach-Object { Write-Host "        $_" }
        throw "sqlcmd failed (exit $code). Arguments: $($Arguments -join ' ')"
    }
    return $output
}

# --- 1. Directorio de evidencia ------------------------------------------------------
Write-Step 'Preparando el directorio de evidencia'
if (-not (Test-Path -LiteralPath $EvidenceDirectory)) {
    New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
}
$probeFile = Join-Path $EvidenceDirectory ".write-probe-$Stamp"
try {
    Set-Content -LiteralPath $probeFile -Value 'probe' -Encoding utf8
    Remove-Item -LiteralPath $probeFile -Force
} catch {
    throw "The evidence directory is not writable: $EvidenceDirectory"
}
Write-Ok $EvidenceDirectory

# --- 2. Hashes de los artefactos revisados -------------------------------------------
Write-Step 'Verificando los hashes de los artefactos revisados'
$entries = Get-Content -Raw -LiteralPath $Manifest | ConvertFrom-Json
foreach ($path in @('KulaniKDI_Test-identity.sql', 'KulaniKDI_Test-organizations.sql')) {
    $entry = $entries | Where-Object { $_.path -eq $path }
    if (-not $entry) { throw "Artifact not present in the reviewed manifest: $path" }
    $actual = (Get-FileHash -LiteralPath (Join-Path $Prepared $path) -Algorithm SHA256).Hash
    if ($actual -ne $entry.sha256) { throw "HASH MISMATCH on ${path}: expected $($entry.sha256), actual $actual" }
    Write-Ok "$path  $($entry.sha256)"
}

# --- 3. Integridad del candidato y de D2-A -------------------------------------------
Write-Step 'Verificando integridad del candidato y de D2-A'
$python = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $python) {
    # Sin python no se puede verificar la integridad: se FALLA. Omitirla en silencio
    # permitiria aplicar una migracion sobre un arbol que no se sabe que es el revisado.
    throw 'python not found. The integrity verification is mandatory and is NOT skipped.'
}
$integrity = @(
    @{ Name = 'freeze.py --verify'; Path = 'docs\stage3\d2b\preparation-20261020\freeze.py'; Args = @('--verify') },
    @{ Name = 'verify.py D2-A';     Path = 'docs\stage3\d2a\closure\identities-20261018\verify.py'; Args = @() }
)
foreach ($v in $integrity) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $out = & $python (Join-Path $Root $v.Path) @($v.Args) 2>&1
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($code -ne 0) {
        Write-Bad "Integrity check failed: $($v.Name) (exit $code)"
        $out | Select-Object -Last 8 | ForEach-Object { Write-Host "        $_" }
        throw "Integrity check failed: $($v.Name). Stop. Nothing was applied."
    }
    Write-Ok $v.Name
}

# --- 4. Estado previo: las tablas D2-B no deben existir -------------------------------
Write-Step 'Comprobando el estado previo de las tablas D2-B'
$probeOutput = Invoke-Sqlcmd -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '30', '-b', '-h', '-1', '-W',
    '-Q', "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE (s.name=N'identity' AND t.name IN (N'Users',N'Grants',N'Sessions',N'AuditEntries',N'PermissionCatalog')) OR (s.name=N'organizations' AND t.name IN (N'Organizations',N'OrganizationMemberships',N'AuditEntries'));"
)
$probeText = ($probeOutput | ForEach-Object { [string]$_ } | Where-Object { $_.Trim() -ne '' } | Select-Object -First 1)
$existing = 0
if (-not [int]::TryParse(($probeText -replace '\D', ''), [ref]$existing)) {
    throw "The preflight probe did not return a readable count (got: '$probeText'). Refusing to continue."
}
if ($existing -ne 0) {
    throw "BLOCKED: $existing D2-B tables already exist. A3 looks applied or partially applied. Run A3-VERIFY.sql and evaluate before doing anything else."
}
Write-Ok 'Ninguna tabla D2-B existe todavia.'

# --- 5. Opciones SET de la sesion, con control negativo -------------------------------
# El paquete D2-B crea columnas calculadas PERSISTED e indices filtrados, que exigen
# QUOTED_IDENTIFIER ON. sqlcmd arranca con OFF salvo -I. Se demuestra el flag con un
# control negativo: el mismo archivo debe fallar sin -I y pasar con -I.
Write-Step 'Comprobando las opciones SET de la sesion (control negativo y afirmativo)'
Invoke-Sqlcmd -ExpectFailure -Expectation 'Control negativo: la comprobacion falla sin -I, como debe' -Arguments @(
    '-E', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '30', '-b', '-W', '-i', $SetOptions
) | Out-Null
Invoke-Sqlcmd -LogPath (Join-Path $EvidenceDirectory "A3-set-options-$Stamp.log") -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '30', '-b', '-W', '-i', $SetOptions
) | Out-Null
Write-Ok 'QUOTED_IDENTIFIER ON confirmado con -I'

if ($PreflightOnly) {
    Write-Host "`nPREFLIGHT OK: hashes, integridad, estado previo y opciones SET verificados. No se aplico nada." -ForegroundColor Green
    Write-Host "Siguiente paso: ejecuta el script sin -PreflightOnly. Te pedira la contrasena de kdi_migrator tres veces." -ForegroundColor Cyan
    exit 0
}

# --- 6. Aplicacion -------------------------------------------------------------------
# sqlcmd lee la contrasena de la entrada estandar. Si la entrada esta redirigida, la
# contrasena tendria que llegar por una tuberia, que es justo lo que se prohibe.
# Esta comprobacion bloquea la EJECUCION desde cualquier contexto no interactivo, incluido
# el agente, y por tanto en un sandbox.
if ([Console]::IsInputRedirected) {
    throw 'BLOCKED: A3 requires a private interactive console. The password prompt reads from the console; a redirected input would mean the password travels through a pipe. The preflight (-PreflightOnly) is read-only and can run anywhere.'
}

function Invoke-Migration {
    param([string]$File, [string]$Label)
    $log = Join-Path $EvidenceDirectory "A3-$Label-$Stamp.log"
    Write-Step "Aplicando $Label (se pedira la contrasena de kdi_migrator)"
    Write-Host "    Archivo: $File"
    Write-Host "    Destino: DESKTOP-F25O3MF / KulaniKDI_Test   Evidencia: $log"
    Invoke-Sqlcmd -LogPath $log -Arguments @(
        '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test', '-U', 'kdi_migrator',
        '-C', '-I', '-l', '10', '-t', '300', '-b', '-W', '-i', $File
    ) | Out-Null
    Write-Ok "$Label aplicado correctamente."
}

Invoke-Migration -File $Identity -Label 'identity'
Invoke-Migration -File $Organizations -Label 'organizations'

# --- 7. Semilla sintetica para N2 (opcional) ------------------------------------------
if (-not $SkipSeedUser) {
    $log = Join-Path $EvidenceDirectory "A3-seed-user-$Stamp.log"
    Write-Step 'Creando la semilla sintetica de usuario para N2'
    try {
        Invoke-Sqlcmd -LogPath $log -Arguments @(
            '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test', '-U', 'kdi_migrator',
            '-C', '-I', '-l', '10', '-t', '60', '-b', '-W', '-i', $Seed
        ) | Out-Null
        Write-Ok 'Semilla creada (o ya existente).'
    } catch {
        Write-Bad "La semilla fallo. Las migraciones siguen aplicadas: NO repitas la migracion. Reparta este error y evalua antes de reintentar. $($_.Exception.Message)"
    }
}

# --- 8. Verificacion de solo lectura -------------------------------------------------
$log = Join-Path $EvidenceDirectory "A3-verify-$Stamp.log"
Write-Step 'Verificacion de solo lectura del resultado'
$verifyOutput = Invoke-Sqlcmd -LogPath $log -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '120', '-b', '-W', '-s', ' | ', '-i', $Verify
)
$verifyOutput | ForEach-Object { Write-Host "    $_" }
Write-Ok 'Verificacion terminada sin errores.'

Write-Host "`nA3 COMPLETADO. Evidencia en $EvidenceDirectory" -ForegroundColor Green
exit 0
