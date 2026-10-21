<#
    A1-FILE-CHECK.ps1

    Verificacion PREVIA de la puerta A1. Solo lee archivos y calcula hashes.
    NO abre conexiones SQL, NO ejecuta SQL, NO modifica nada, NO regenera manifiestos.

    Uso (desde E:\INSSOFT\KDI, PowerShell normal, sin elevacion):
        .\docs\stage3\d2b\controlled-test-plan\A1-FILE-CHECK.ps1

    Salida: OK / FALLO por cada comprobacion. Exit code 0 solo si todas pasan.

    Documentacion: A1-OPERATOR-PACK.md (paso 2), evidence/A1/RECOVERY-LOCAL-WINDOWS.md (paso 3).
#>

[CmdletBinding()]
param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Raiz del workspace KDI: tres niveles acima de este archivo
#   docs/stage3/d2b/controlled-test-plan/ -> d2b -> stage3 -> docs -> raiz KDI
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path

$Expected = [ordered]@{
    'candidato D2-B' = @{
        Path = 'docs\stage3\d2b\preparation-20261020\candidate-manifest.json'
        Sha  = '91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448'
    }
    'manifiesto D2-A original' = @{
        Path = 'docs\stage3\d2a\candidate-manifest.json'
        Sha  = '66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f'
    }
    'script A1' = @{
        Path = 'docs\stage3\d2b\controlled-test-plan\01-preflight-readonly-REQUIRES-A1.sql'
        Sha  = '60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c'
    }
}

function Write-Result {
    param([string]$Name, [bool]$Ok, [string]$Detail)
    $mark = if ($Ok) { 'OK   ' } else { 'FALLO' }
    if (-not $Quiet) { Write-Host ("[{0}] {1}: {2}" -f $mark, $Name, $Detail) }
}

$failed = 0

Write-Result -Name 'raiz del workspace' -Ok $true -Detail $Root

foreach ($entry in $Expected.GetEnumerator()) {
    $full = Join-Path $Root $entry.Value.Path
    if (-not (Test-Path -LiteralPath $full)) {
        Write-Result -Name $entry.Key -Ok $false -Detail ("no existe: " + $entry.Value.Path)
        $failed++
        continue
    }
    $actual = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -eq $entry.Value.Sha) {
        Write-Result -Name $entry.Key -Ok $true -Detail ($actual.Substring(0, 16) + '...')
    }
    else {
        Write-Result -Name $entry.Key -Ok $false -Detail ("esperado " + $entry.Value.Sha + " / actual " + $actual)
        $failed++
    }
}

# Verificadores de integridad, si estan disponibles. No son requisito: su exit code
# se informa tal cual. Ninguno abre conexion SQL.
#
# Nota: se ejecuta con ErrorActionPreference Continua dentro de este bloque. Un
# verificador que falla escribe su traza en stderr y, con Stop, PowerShell abortaria
# el script sin llegar a mostrar el exit code. Aqui interesa el codigo de salida.
$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
    foreach ($v in @(
        @{ Name = 'freeze.py --verify'; Script = 'docs\stage3\d2b\preparation-20261020\freeze.py'; Args = @('--verify') },
        @{ Name = 'verify.py D2-A';     Script = 'docs\stage3\d2a\closure\identities-20261018\verify.py'; Args = @() }
    )) {
        $script = Join-Path $Root $v.Script
        if (-not (Test-Path -LiteralPath $script)) {
            Write-Result -Name $v.Name -Ok $false -Detail ('no existe: ' + $v.Script)
            $failed++
            continue
        }
        $prev = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $out = & $python.Source $script @($v.Args) 2>&1
        $code = $LASTEXITCODE
        $ErrorActionPreference = $prev
        $detail = 'exit ' + $code
        if ($code -ne 0) {
            $tail = ($out | Select-Object -Last 3) -join ' | '
            $detail = $detail + ' :: ' + $tail
        }
        Write-Result -Name $v.Name -Ok ($code -eq 0) -Detail $detail
        if ($code -ne 0) { $failed++ }
    }
}
else {
    Write-Result -Name 'verificadores de integridad' -Ok $false -Detail 'python no disponible en PATH'
    $failed++
}

if ($failed -gt 0) {
    Write-Host ''
    Write-Host "DETENCION: $failed comprobacion(es) fallida(s). No ejecutar A1. No regenerar manifiestos." -ForegroundColor Red
    Write-Host 'Si el contexto SQL no puede verificarse, A1 permanece BLOCKED: reporte esa condicion y nada mas.'
    exit 1
}

Write-Host ''
Write-Host 'OK: verificacion de archivos previa superada. Continuar con el paso 3 del paquete de operador.' -ForegroundColor Green
exit 0
