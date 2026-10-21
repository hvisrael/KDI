param([string]$ArtifactDirectory = (Join-Path $PSScriptRoot 'prepared-sql'), [string]$ReproductionDirectory = (Join-Path $PSScriptRoot 'repro-sql'))
$ErrorActionPreference = 'Stop'
$entries = Get-Content -Raw -LiteralPath (Join-Path $ArtifactDirectory 'manifest.json') | ConvertFrom-Json
if ($entries.Count -ne 14) { throw 'Expected 14 reviewed artifacts.' }
foreach ($entry in $entries) {
    $path = Join-Path $ArtifactDirectory $entry.path
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Hash mismatch: $($entry.path)" }
}
$reproductions = @(Get-ChildItem -LiteralPath $ReproductionDirectory -File)
if ($reproductions.Count -ne 12) { throw 'Expected 12 reproduced migration artifacts.' }
foreach ($file in $reproductions) {
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $ArtifactDirectory $file.Name)).Hash) { throw "Reproduction mismatch: $($file.Name)" }
}
foreach ($db in @('KulaniKDI_Test', 'KulaniKDI_Dev')) {
    foreach ($module in @('identity', 'organizations')) {
        $sql = Get-Content -Raw -LiteralPath (Join-Path $ArtifactDirectory "$db-$module.rollback.sql")
        if ($sql -notmatch [regex]::Escape("DB_NAME() <> N'$db'") -or $sql -notmatch 'Expected D2-B history required') { throw 'Rollback lacks target/history protection.' }
        if ($sql -match 'DROP SCHEMA|DROP TABLE \[[^]]+\]\.\[__EFMigrationsHistory\]' -or $sql -match "WHERE \[MigrationId\] = N'20261018000100") { throw 'Rollback damages accepted baseline.' }
        $tables = @([regex]::Matches($sql, 'DROP TABLE \[([^]]+)\]\.\[([^]]+)\]') | ForEach-Object { "$($_.Groups[1].Value).$($_.Groups[2].Value)" })
        $expected = if ($module -eq 'identity') { @('identity.AuditEntries','identity.Grants','identity.Sessions','identity.PermissionCatalog','identity.Users') } else { @('organizations.AuditEntries','organizations.OrganizationMemberships','organizations.Organizations') }
        if (@(Compare-Object $expected $tables).Count) { throw 'Rollback table scope mismatch.' }
    }
}
Write-Output 'OFFLINE VERIFIED: 14 artifact hashes, 12 reproduction hashes, 4 rollback target/history/baseline/table scopes. No SQL executed.'
