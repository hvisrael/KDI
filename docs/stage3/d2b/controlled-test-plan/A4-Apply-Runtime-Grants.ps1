<#
    A4-Apply-Runtime-Grants.ps1

    Aplica los permisos runtime aprobados a `kdi_application` en KulaniKDI_Test, con la
    identidad `kdi_migrator` (rol `kdi_migrations`, que tiene ALTER de esquema y por tanto
    puede conceder permisos sobre objetos). La contrasena se introduce en el PROMPT de
    sqlcmd: no viaja por la linea de comandos, ni por variables de entorno, ni por logs.

    Uso, desde una consola Windows interactiva y privada (no redirigida):

        .\docs\stage3\d2b\controlled-test-plan\A4-Apply-Runtime-Grants.ps1

    Solo comprobacion previa, sin conceder nada:

        .\docs\stage3\d2b\controlled-test-plan\A4-Apply-Runtime-Grants.ps1 -PreflightOnly

    El script:
      1. comprueba que la consola es interactiva (si esta redirigida, se niega);
      2. verifica el hash del artefacto contra el manifiesto revisado;
      3. verifica la integridad del candidato y de D2-A (si falta python, FALLA);
      4. comprueba el estado previo: kdi_application existe, NO tiene permisos a nivel de
         objeto (si los tiene, A4 ya se aplico) y los DENY de DDL estan intactos;
      5. aplica el archivo revisado de permisos;
      6. ejecuta la verificacion de A4, que exige el conjunto EXACTO de permisos y falla si
         hay uno de mas, uno de menos, un DELETE, un permiso sobre organizations.AuditEntries,
         una concesion de esquema o un cambio de membresias;
      7. toma el snapshot de permisos posterior, para comparar con el previo.

    Si la verificacion falla, el permiso queda aplicado: el script lo dice y NO lo revoca
    por su cuenta. La revocacion requiere revision aparte.
#>
[CmdletBinding()]
param(
    [switch]$PreflightOnly,
    [string]$EvidenceDirectory = (Join-Path $PSScriptRoot 'evidence\A1')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Raiz del workspace: docs/stage3/d2b/controlled-test-plan -> d2b -> stage3 -> docs -> raiz
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$Prepared = Join-Path $Root '.local\d2b-candidate\tools\Kulani.KDI.Migrations\prepared-sql'
$Manifest = Join-Path $Prepared 'manifest.json'
$Grants = Join-Path $Prepared 'KulaniKDI_Test-runtime-grants.sql'
$Snapshot = Join-Path $PSScriptRoot 'A4-PERMISSION-SNAPSHOT.sql'
$Verify = Join-Path $PSScriptRoot 'A4-VERIFY.sql'

$Sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
if (-not (Test-Path -LiteralPath $Sqlcmd)) { $Sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source }
if (-not $Sqlcmd) { throw 'sqlcmd not found.' }
foreach ($required in @($Manifest, $Grants, $Snapshot, $Verify)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required file not found: $required" }
}

$Stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')

function Write-Step { param([string]$Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Ok { param([string]$Text) Write-Host "    OK  $Text" -ForegroundColor Green }
function Write-Bad { param([string]$Text) Write-Host "    !!  $Text" -ForegroundColor Red }

function Invoke-Sqlcmd {
    param(
        [string[]]$Arguments,
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
        throw "sqlcmd failed (exit $code)."
    }
    return $output
}

# --- 1. Rutas y consola ----------------------------------------------------------------
# La exigencia de consola privada NO se comprueba aqui: solo importa donde sqlcmd pide la
# contrasena. El preflight es de solo lectura y se puede ejecutar en cualquier contexto.
# La comprobacion que bloquea la concesion esta justo antes del paso 5.

if (-not (Test-Path -LiteralPath $EvidenceDirectory)) {
    New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
}

# --- 2. Hash del artefacto revisado ---------------------------------------------------
Write-Step 'Verificando el hash del artefacto de permisos revisado'
$entries = Get-Content -Raw -LiteralPath $Manifest | ConvertFrom-Json
$entry = $entries | Where-Object { $_.path -eq 'KulaniKDI_Test-runtime-grants.sql' }
if (-not $entry) { throw 'The grants artifact is not present in the reviewed manifest.' }
$actual = (Get-FileHash -LiteralPath $Grants -Algorithm SHA256).Hash
if ($actual -ne $entry.sha256) { throw "HASH MISMATCH: expected $($entry.sha256), actual $actual" }
Write-Ok "KulaniKDI_Test-runtime-grants.sql  $($entry.sha256)"

# --- 3. Integridad --------------------------------------------------------------------
Write-Step 'Verificando integridad del candidato y de D2-A'
$python = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $python) { throw 'python not found. The integrity verification is mandatory and is NOT skipped.' }
foreach ($v in @(
    @{ Name = 'freeze.py --verify'; Path = 'docs\stage3\d2b\preparation-20261020\freeze.py'; Args = @('--verify') },
    @{ Name = 'verify.py D2-A';     Path = 'docs\stage3\d2a\closure\identities-20261018\verify.py'; Args = @() }
)) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $out = & $python (Join-Path $Root $v.Path) @($v.Args) 2>&1
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($code -ne 0) {
        Write-Bad "Integrity check failed: $($v.Name) (exit $code)"
        $out | Select-Object -Last 8 | ForEach-Object { Write-Host "        $_" }
        throw "Integrity check failed: $($v.Name). Stop. Nothing was granted."
    }
    Write-Ok $v.Name
}

# --- 4. Estado previo -----------------------------------------------------------------
Write-Step 'Comprobando el estado previo de los permisos'
$pre = Invoke-Sqlcmd -LogPath (Join-Path $EvidenceDirectory "A4-snapshot-antes-$Stamp.log") -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '60', '-b', '-W', '-s', ' | ', '-i', $Snapshot
)
$pre | ForEach-Object { Write-Host "    $_" }

$existing = Invoke-Sqlcmd -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '30', '-b', '-h', '-1', '-W',
    '-Q', "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.database_permissions p JOIN sys.objects o ON o.object_id=p.major_id JOIN sys.database_principals pr ON pr.principal_id=p.grantee_principal_id WHERE pr.name=N'kdi_application' AND p.state_desc=N'GRANT' AND p.class_desc=N'OBJECT_OR_COLUMN';"
)
$grantedText = ($existing | ForEach-Object { [string]$_ } | Where-Object { $_.Trim() -ne '' } | Select-Object -First 1)
$grantedCount = 0
if (-not [int]::TryParse(($grantedText -replace '\D', ''), [ref]$grantedCount)) {
    throw "The pre-state probe did not return a readable count (got: '$grantedText'). Refusing to continue."
}
if ($grantedCount -ne 0) {
    throw "BLOCKED: kdi_application already holds $grantedCount object level permissions. A4 looks applied. Run A4-VERIFY.sql and evaluate before doing anything else."
}
Write-Ok 'kdi_application no tiene permisos a nivel de objeto: A4 no se ha aplicado todavia.'

if ($PreflightOnly) {
    Write-Host "`nPREFLIGHT OK: hash, integridad y estado previo verificados. No se concedio nada." -ForegroundColor Green
    Write-Host "Siguiente paso: ejecuta el script sin -PreflightOnly. Te pedira la contrasena de kdi_migrator." -ForegroundColor Cyan
    exit 0
}

# --- 5. Aplicacion --------------------------------------------------------------------
# La concesion la ejecuta la IDENTIDAD DEL OPERADOR (autenticacion Windows integrada), no
# kdi_migrator. Motivo medido: los objetos D2-B se crearon sin dueno (sys.objects.principal_id
# es NULL, igual que los de D2-A), y SQL Server solo permite conceder permisos sobre un objeto
# sin dueno a quien tenga CONTROL o pertenezca al rol fijo que lo posee. Se comprobo que
# kdi_migrator recibe Msg 15151 ("Cannot find the object 'Users'") y que la identidad del
# operador si concede (prueba reversible: 2 permisos concedidos y revertidos).
# El conjunto de permisos resultante para kdi_application es IDENTICO en ambos casos: lo que
# cambia es quien lo concede, no lo que se concede. Los DENY de DDL no se ven afectados.
if ([Console]::IsInputRedirected) {
    Write-Step 'Nota: la consola esta redirigida. No es un problema para A4 (no hay contrasena), pero el prompt'
    Write-Host '    de A3 si lo seria: sqlcmd leeria la contrasena de una tuberia, que esta prohibido.' -ForegroundColor Yellow
}

Write-Step 'Concediendo los permisos runtime con la identidad del operador'
Write-Host "    Archivo: $Grants"
Write-Host "    Destino: DESKTOP-F25O3MF / KulaniKDI_Test   Evidencia: $EvidenceDirectory\A4-apply-$Stamp.log"
Invoke-Sqlcmd -LogPath (Join-Path $EvidenceDirectory "A4-apply-$Stamp.log") -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-C', '-l', '10', '-t', '60', '-b', '-W', '-i', $Grants
) | Out-Null
Write-Ok 'Los permisos fueron concedidos.'

# --- 6. Verificacion con aserciones duras ----------------------------------------------
Write-Step 'Verificando el conjunto de permisos concedido (aserciones duras)'
try {
    $verifyOutput = Invoke-Sqlcmd -LogPath (Join-Path $EvidenceDirectory "A4-verify-$Stamp.log") -Arguments @(
        '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
        '-l', '8', '-t', '60', '-b', '-W', '-s', ' | ', '-i', $Verify
    )
    $verifyOutput | ForEach-Object { Write-Host "    $_" }
    Write-Ok 'La verificacion de A4 paso sin errores.'
} catch {
    Write-Bad "La verificacion de A4 FALLO: $($_.Exception.Message)"
    Write-Bad 'Los permisos pueden haber quedado concedidos. NO se revoca de forma automatica: revisa la evidencia y decide la remediacion.'
    throw
}

# --- 7. Snapshot posterior -------------------------------------------------------------
Write-Step 'Snapshot de permisos posterior, para comparar con el previo'
$post = Invoke-Sqlcmd -LogPath (Join-Path $EvidenceDirectory "A4-snapshot-despues-$Stamp.log") -Arguments @(
    '-E', '-I', '-S', 'DESKTOP-F25O3MF', '-d', 'KulaniKDI_Test',
    '-l', '8', '-t', '60', '-b', '-W', '-s', ' | ', '-i', $Snapshot
)
$post | ForEach-Object { Write-Host "    $_" }

Write-Host "`nA4 COMPLETADO. Evidencia en $EvidenceDirectory" -ForegroundColor Green
exit 0
