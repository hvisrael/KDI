param(
    [Parameter(Mandatory=$true)][uri]$Issuer,
    [Parameter(Mandatory=$true)][string]$ClientId,
    [Parameter(Mandatory=$true)][string]$KeyDirectory,
    [Parameter(Mandatory=$true)][string]$CandidateRoot
)
$ErrorActionPreference='Stop'
if ($Issuer.Scheme -ne 'https' -or -not $Issuer.AbsoluteUri.EndsWith('/')) { throw 'Exact HTTPS issuer with trailing slash required' }
$resolvedRoot=(Resolve-Path -LiteralPath $CandidateRoot).Path
$entry=Join-Path $resolvedRoot 'src/Kulani.KDI.Api/bin/Release/net10.0/Kulani.KDI.Api.dll'
if (-not (Test-Path -LiteralPath $entry -PathType Leaf)) { throw 'Build the reviewed candidate in Release before local launch' }
if (-not (Test-Path -LiteralPath $KeyDirectory -PathType Container)) { throw 'Existing protected operational key directory required; launcher does not provision it' }
# No secrets in parameters, environment or generated files. The API reads both passwords privately in this foreground console.
# Test only. This does not migrate or grant permissions. HTTPS certificate/trust must already be authorized and installed.
& dotnet $entry --kdi-sql-console-credential --kdi-oidc-console-secret '--KdiAccess:Enabled=true' '--KdiAccess:Origin=https://localhost:7443' "--KdiAccess:Issuer=$($Issuer.AbsoluteUri)" "--KdiAccess:ClientId=$ClientId" "--KdiAccess:KeyDirectory=$KeyDirectory" '--ConnectionStrings:Kdi=Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=False;Encrypt=True;TrustServerCertificate=True;Persist Security Info=False'
exit $LASTEXITCODE
