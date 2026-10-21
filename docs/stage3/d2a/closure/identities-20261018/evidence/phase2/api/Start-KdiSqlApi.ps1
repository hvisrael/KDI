[CmdletBinding()]
param(
    [ValidateSet('KulaniKDI_Test', 'KulaniKDI_Dev')]
    [string]$Database = 'KulaniKDI_Test',
    [ValidateRange(1024,65535)]
    [int]$Port = 5088
)
$ErrorActionPreference = 'Stop'
# Execute in a visible local interactive PowerShell console. Never capture stdin,
# ReadKey, transcripts, process dumps, or the password-entry session.
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
$candidate = Join-Path $workspace '.local/d2a-phase2-candidate'
$api = Join-Path $candidate 'src/Kulani.KDI.Api/bin/Release/net10.0/Kulani.KDI.Api.dll'
if (!(Test-Path -LiteralPath $api)) { throw 'Build the isolated candidate in Release first.' }
if ([Console]::IsInputRedirected -or [Console]::IsOutputRedirected) { throw 'Visible local interactive console required.' }
# This string contains target metadata only. The password is read inside the API
# directly into SecureString and supplied through Microsoft.Data.SqlClient.SqlCredential.
$metadata = "Server=DESKTOP-F25O3MF;Database=$Database;Integrated Security=false;Encrypt=true;TrustServerCertificate=true;Persist Security Info=false;Connect Timeout=10"
Push-Location $candidate
try {
    & dotnet $api '--kdi-sql-console-credential' '--environment' 'Production' '--urls' "http://127.0.0.1:$Port" '--ConnectionStrings:Kdi' $metadata
    if ($LASTEXITCODE -ne 0) { throw 'API process failed. No credentials are recorded by this launcher.' }
}
finally { Pop-Location }
