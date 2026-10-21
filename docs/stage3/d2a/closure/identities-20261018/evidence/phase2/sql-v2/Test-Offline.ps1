# Offline contract tests. Extract only function declarations; never execute helper main, SQL, or GUI.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$errors=$null; $tokens=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Provision-Identities.ps1'),[ref]$tokens,[ref]$errors)
if (@($errors).Count -ne 0) { throw 'SYNTAX' }
$allowed=@('New-ConnectionBuilder','Equal-Secret','Observe-Permissions','Record')
foreach($definition in $ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$false)) {
    if ($definition.Name -in $allowed) { . ([scriptblock]::Create($definition.Extent.Text)) }
}
$script:stage='OFFLINE'; $script:events=[Collections.Generic.List[object]]::new()
$checks=0
function Assert($condition,[string]$label) { if (!$condition) { throw $label }; $script:checks++ }
foreach($db in @('master','KulaniKDI_Dev','KulaniKDI_Test')) {
    foreach($integrated in @($true,$false)) {
        $b=New-ConnectionBuilder $db $integrated
        Assert ($b -is [Data.SqlClient.SqlConnectionStringBuilder]) 'BUILDER_TYPE'
        Assert ($b['Data Source'] -eq 'DESKTOP-F25O3MF' -and $b['Initial Catalog'] -eq $db) 'TARGET'
        Assert ($b['Integrated Security'] -eq $integrated) 'AUTH'
        Assert ($b['Encrypt'] -and $b['TrustServerCertificate'] -and !$b['Persist Security Info'] -and !$b['Pooling']) 'TRANSPORT'
        Assert ($b['Connect Timeout'] -eq 10 -and $b['Application Name'] -eq 'KDI-D2A-IdentityProvisioning') 'LIMITS'
        Assert (!$b.ShouldSerialize('Password') -and !$b.ShouldSerialize('User ID')) 'NO_CREDENTIAL_KEY'
        $c=[Data.SqlClient.SqlConnection]::new($b.ConnectionString)
        try { Assert ($c.State -eq 'Closed') 'OFFLINE_CONNECTION' } finally { $c.Dispose() }
    }
}
$blocked=$false
try { $null=New-ConnectionBuilder 'Unauthorized' $true } catch { $blocked=$true }
Assert $blocked 'ALLOWLIST'
$a=[Security.SecureString]::new(); $b=[Security.SecureString]::new(); $different=[Security.SecureString]::new()
try {
    # Synthetic characters, not credentials; never used in a connection.
    $a.AppendChar([char]65); $b.AppendChar([char]65); $different.AppendChar([char]66)
    Assert (Equal-Secret $a $b) 'SECURE_EQUAL'
    Assert (!(Equal-Secret $a $different)) 'SECURE_UNEQUAL'
} finally { $a.Dispose(); $b.Dispose(); $different.Dispose() }
# Mock only read-side command/reader. Query construction and matrix comparison are production functions.
function Command($connection,[string]$sql) {
    if (!$sql.StartsWith('SELECT ')) { throw 'OFFLINE_NON_SELECT' }
    $table=[Data.DataTable]::new()
    $row=$table.NewRow() # recreated after defining columns below
    $values=@{}
    foreach($match in [regex]::Matches($sql,'(?i)\bAS\s+\[?(\w+)\]?')) {
        $key=$match.Groups[1].Value; [void]$table.Columns.Add($key,[object])
        $value=0
        if ($key -eq 'database_name') { $value=$script:db }
        elseif ($key -in @('login_name','current_login','user_name')) { $value=$script:item.Name }
        elseif ($key -eq 'authentication_type') { $value='INSTANCE' }
        elseif ($key -eq ('member_'+$script:item.Role)) { $value=1 }
        elseif ($key -eq 'dbperm_CREATE_TABLE' -and $script:item.Name -eq 'kdi_migrator') { $value=1 }
        elseif ($key -match '^(identity|organizations|portfolio)_(.+)$') {
            if ($script:item.Name -eq 'kdi_migrator' -or ($script:item.Name -eq 'kdi_test_runner' -and $Matches[2] -eq 'SELECT')) { $value=1 }
        }
        if ($script:injectNull -and $key -eq 'server_sysadmin') { $value=[DBNull]::Value }
        if ($script:injectAdmin -and $key -eq 'server_sysadmin') { $value=1 }
        $values[$key]=$value
    }
    $row=$table.NewRow(); foreach($key in $values.Keys) { $row[$key]=$values[$key] }; $table.Rows.Add($row)
    $mock=[pscustomobject]@{Reader=$table.CreateDataReader()}
    $mock | Add-Member ScriptMethod ExecuteReader { return ,$this.Reader }
    $mock | Add-Member ScriptMethod Dispose {}
    return $mock
}
function Scalar($connection,[string]$sql) {
    if ($sql -notmatch '^SELECT COUNT\(\*\) FROM \[(identity|organizations|portfolio)\]\.\[__EFMigrationsHistory\];$') { throw 'OFFLINE_UNEXPECTED_SCALAR' }
    return 1
}
$script:injectNull=$false; $script:injectAdmin=$false
foreach($script:item in @(@{Name='kdi_app';Role='kdi_application'},@{Name='kdi_migrator';Role='kdi_migrations'},@{Name='kdi_test_runner';Role='kdi_tests'})) {
    $script:db='KulaniKDI_Test'
    Observe-Permissions $null $script:item $script:db
    Assert ($script:events[$script:events.Count-1].result -eq 'READONLY_MATRIX_VERIFIED') 'OBSERVE_MATCH'
}
$script:injectNull=$true
Observe-Permissions $null $script:item $script:db
Assert ($script:events[$script:events.Count-1].result -eq 'FAIL_PERMISSION_MATRIX') 'OBSERVE_NULL'
$script:injectNull=$false; $script:injectAdmin=$true
Observe-Permissions $null $script:item $script:db
Assert ($script:events[$script:events.Count-1].result -eq 'FAIL_PERMISSION_MATRIX') 'OBSERVE_ADMIN'
$script:injectAdmin=$false; $script:db='KulaniKDI_Dev'
Observe-Permissions $null $script:item $script:db
Assert ($script:events[$script:events.Count-1].result -eq 'FAIL_UNEXPECTED_DEV_ACCESS') 'OBSERVE_RUNNER_DEV'
[pscustomobject]@{result='PASS_OFFLINE';assertions=$checks;syntaxErrors=@($errors).Count;connectionsOpened=0;guiPrompts=0;sqlStatementsExecuted=0} | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'offline-results.json') -Encoding UTF8
