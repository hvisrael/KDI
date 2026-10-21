# Fixed-scope D2-A provisioning. Run only after independent review in a private local STA process.
# No command text, exception messages, credentials, or connection strings are emitted.
[CmdletBinding()]
param([switch]$Preflight)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$VerbosePreference = 'SilentlyContinue'
$DebugPreference = 'SilentlyContinue'
Add-Type -AssemblyName System.Data
$script:events = [System.Collections.Generic.List[object]]::new()
$script:watch = [Diagnostics.Stopwatch]::StartNew()
$script:secrets = @{}
$script:output = Join-Path $PSScriptRoot ('execution-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '.json')
$identities = @(
    @{Name='kdi_migrator'; Default='KulaniKDI_Dev'; Role='kdi_migrations'; Databases=@('KulaniKDI_Dev','KulaniKDI_Test')},
    @{Name='kdi_app'; Default='KulaniKDI_Dev'; Role='kdi_application'; Databases=@('KulaniKDI_Dev','KulaniKDI_Test')},
    @{Name='kdi_test_runner'; Default='KulaniKDI_Test'; Role='kdi_tests'; Databases=@('KulaniKDI_Test')}
)
function Record([string]$operation,[string]$name,[string]$database,[string]$result,[int]$number=0,$facts=$null) {
    $script:events.Add([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');operation=$operation;identity=$name;database=$database;result=$result;sqlNumber=$number;facts=$facts})
}
function ErrorNumber($failure) {
    $e=$failure.Exception
    while ($null -ne $e) { if ($e -is [System.Data.SqlClient.SqlException]) { return [int]$e.Number }; $e=$e.InnerException }
    return 0
}
function Deadline { if ($script:watch.Elapsed.TotalMinutes -ge 15) { throw 'EXECUTION_LIMIT' } }
function Open-Connection([string]$database,[string]$identity='') {
    Deadline
    if ($database -notin @('master','KulaniKDI_Dev','KulaniKDI_Test')) { throw 'TARGET' }
    $builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder.DataSource='DESKTOP-F25O3MF'; $builder.InitialCatalog=$database
    $builder.Encrypt=$true; $builder.TrustServerCertificate=$true # approved local development exception; no global change
    $builder.PersistSecurityInfo=$false; $builder.ConnectTimeout=10; $builder.Pooling=$false
    $builder.ApplicationName='KDI-D2A-IdentityProvisioning'
    $builder.IntegratedSecurity=($identity -eq '')
    $connection=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    if ($identity -ne '') { $connection.Credential=[System.Data.SqlClient.SqlCredential]::new($identity,$script:secrets[$identity]) }
    try { $connection.Open(); return $connection } catch { $connection.Dispose(); throw }
}
function Command($connection,[string]$sql,$transaction=$null) {
    Deadline
    $cmd=$connection.CreateCommand(); $cmd.CommandTimeout=20; $cmd.CommandText=$sql
    if ($null -ne $transaction) { $cmd.Transaction=$transaction }
    return $cmd
}
function Scalar($connection,[string]$sql,$transaction=$null) {
    $cmd=Command $connection $sql $transaction
    try { return $cmd.ExecuteScalar() } finally { $cmd.Dispose() }
}
function Execute($connection,[string]$sql,$transaction) {
    $cmd=Command $connection $sql $transaction
    try { [void]$cmd.ExecuteNonQuery() } finally { $cmd.CommandText=''; $cmd.Dispose() }
}
function Equal-Secret([Security.SecureString]$a,[Security.SecureString]$b) {
    if ($a.Length -ne $b.Length) { return $false }
    $pa=[IntPtr]::Zero; $pb=[IntPtr]::Zero
    try {
        $pa=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($a); $pb=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($b)
        $difference=0
        for($i=0;$i -lt $a.Length;$i++) { $difference=$difference -bor ([Runtime.InteropServices.Marshal]::ReadInt16($pa,$i*2) -bxor [Runtime.InteropServices.Marshal]::ReadInt16($pb,$i*2)) }
        return ($difference -eq 0)
    } finally {
        if ($pa -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pa) }
        if ($pb -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pb) }
    }
}
function Observe-Permissions($connection,$item,[string]$db) {
    $fields=[System.Collections.Generic.List[string]]::new()
    $expected=@{database_name=$db;login_name=$item.Name;current_login=$item.Name;user_name=$item.Name;authentication_type='INSTANCE'}
    $fields.Add("DB_NAME() AS database_name, ORIGINAL_LOGIN() AS login_name, SUSER_SNAME() AS current_login, USER_NAME() AS user_name, (SELECT authentication_type_desc FROM sys.database_principals WHERE principal_id=USER_ID()) AS authentication_type")
    foreach($role in @('sysadmin','serveradmin','securityadmin','processadmin','setupadmin','bulkadmin','diskadmin','dbcreator')) {
        $fields.Add("IS_SRVROLEMEMBER('$role') AS server_$role"); $expected["server_$role"]=0
    }
    foreach($role in @('db_owner','db_securityadmin','db_ddladmin','db_accessadmin','db_datareader','db_datawriter','db_backupoperator','kdi_migrations','kdi_application','kdi_tests')) {
        $fields.Add("IS_MEMBER('$role') AS member_$role"); $expected["member_$role"]=[int]($role -eq $item.Role)
    }
    foreach($permission in @('CONTROL SERVER','ALTER ANY LOGIN','ALTER ANY SERVER ROLE')) {
        $alias='serverperm_'+$permission.Replace(' ','_')
        $fields.Add("HAS_PERMS_BY_NAME(NULL,'SERVER','$permission') AS [$alias]"); $expected[$alias]=0
    }
    foreach($permission in @('CONTROL','ALTER ANY ROLE','ALTER ANY USER','CREATE TABLE','CREATE VIEW','CREATE PROCEDURE','CREATE FUNCTION','CREATE SCHEMA')) {
        $alias='dbperm_'+$permission.Replace(' ','_')
        $fields.Add("HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','$permission') AS [$alias]")
        $expected[$alias]=[int]($item.Name -eq 'kdi_migrator' -and $permission -eq 'CREATE TABLE')
    }
    foreach($schema in @('dbo','identity','organizations','portfolio')) {
        foreach($permission in @('ALTER','SELECT','INSERT','UPDATE','DELETE','REFERENCES','VIEW DEFINITION')) {
            $alias=$schema+'_'+$permission.Replace(' ','_')
            $fields.Add("HAS_PERMS_BY_NAME('$schema','SCHEMA','$permission') AS [$alias]")
            $expected[$alias]=[int]($schema -ne 'dbo' -and ($item.Name -eq 'kdi_migrator' -or ($item.Name -eq 'kdi_test_runner' -and $permission -eq 'SELECT')))
        }
    }
    $cmd=Command $connection ('SELECT '+($fields -join ',')+';')
    $facts=@{}; $mismatch=[System.Collections.Generic.List[string]]::new()
    try {
        $reader=$cmd.ExecuteReader()
        try {
            if (!$reader.Read()) { throw 'NO_OBSERVATION' }
            for($i=0;$i -lt $reader.FieldCount;$i++) {
                $key=$reader.GetName($i); $value=$reader.GetValue($i)
                if ($value -is [DBNull]) { $facts[$key]=$null; $mismatch.Add($key+':NOT_VERIFIED') }
                else { $facts[$key]=$value; if ([string]$value -cne [string]$expected[$key]) { $mismatch.Add($key) } }
            }
        } finally { $reader.Dispose() }
    } finally { $cmd.Dispose() }
    if ($item.Name -in @('kdi_migrator','kdi_test_runner')) {
        foreach($schema in @('identity','organizations','portfolio')) {
            $facts[$schema+'_history_count']=[int](Scalar $connection "SELECT COUNT(*) FROM [$schema].[__EFMigrationsHistory];")
        }
    }
    $facts['mismatches']=@($mismatch)
    $result=if($mismatch.Count -eq 0){'READONLY_MATRIX_VERIFIED'}else{'FAIL_PERMISSION_MATRIX'}
    if ($item.Name -eq 'kdi_test_runner' -and $db -eq 'KulaniKDI_Dev') { $result='FAIL_UNEXPECTED_DEV_ACCESS' }
    Record 'SQL AUTH / READONLY PERMISSION OBSERVATION' $item.Name $db $result 0 $facts
}
try {
    if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'STA_REQUIRED' }
    if ($PSVersionTable.PSEdition -ne 'Desktop' -or $PSVersionTable.PSVersion.Major -ne 5) { throw 'WINDOWS_POWERSHELL_5_GUI_REQUIRED' }
    $admin=Open-Connection 'master'
    try {
        $server=[string](Scalar $admin "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))")
        $mixed=[int](Scalar $admin "SELECT CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly'))")
        if ($server -ne 'DESKTOP-F25O3MF' -or $mixed -ne 0) { throw 'PRECONDITION' }
        if ([int](Scalar $admin "SELECT IS_SRVROLEMEMBER('sysadmin')") -ne 1) { throw 'FULL_METADATA_VISIBILITY_REQUIRED' }
        Record 'precondition' '' 'master' 'VERIFIED' 0 @{server=$server;integratedOnly=$mixed}
    } finally { $admin.Dispose() }
    foreach($item in $identities) {
        $admin=Open-Connection 'master'
        try {
            $count=[int](Scalar $admin "SELECT COUNT(*) FROM sys.server_principals WHERE name=N'$($item.Name)'")
            Record 'PREFLIGHT LOGIN' $item.Name 'master' $(if($count -eq 0){'ABSENT'}else{'CONFLICT'}) 0 @{existing=$count}
        } finally { $admin.Dispose() }
        foreach($db in @('KulaniKDI_Dev','KulaniKDI_Test')) {
            $connection=Open-Connection $db
            try {
                $count=[int](Scalar $connection "SELECT COUNT(*) FROM sys.database_principals WHERE name=N'$($item.Name)'")
                $role=[int](Scalar $connection "SELECT COUNT(*) FROM sys.database_principals WHERE name=N'$($item.Role)' AND type='R' AND owning_principal_id=DATABASE_PRINCIPAL_ID(N'dbo')")
                $required=$db -in $item.Databases
                $result=if($count -ne 0){'CONFLICT'}elseif($required -and $role -ne 1){'FAILED_ROLE_PRECONDITION'}else{'VERIFIED'}
                Record 'PREFLIGHT USER / ROLE' $item.Name $db $result 0 @{existingUser=$count;rolePresentOwnedByDbo=$role;mappingRequired=$required}
            } finally { $connection.Dispose() }
        }
    }
    if (!$Preflight) {
    # Collect all credentials before any DDL. Cancellation or mismatch causes zero mutations.
    foreach($item in $identities) {
        $first=$null; $second=$null
        try {
            $first=Get-Credential -UserName $item.Name -Message ('Nueva contraseña SQL local para ' + $item.Name)
            $second=Get-Credential -UserName $item.Name -Message ('Confirmar contraseña SQL local para ' + $item.Name)
            if ($null -eq $first -or $null -eq $second -or $first.UserName -cne $item.Name -or $second.UserName -cne $item.Name -or $first.Password.Length -eq 0 -or !(Equal-Secret $first.Password $second.Password)) { throw 'CANCEL_OR_MISMATCH' }
            $secret=$first.Password.Copy(); $secret.MakeReadOnly(); $script:secrets[$item.Name]=$secret
        } finally {
            if ($null -ne $first) { $first.Password.Dispose() }; if ($null -ne $second) { $second.Password.Dispose() }
            $first=$null; $second=$null
        }
    }
    foreach($item in $identities) {
        $loginCreated=$false; $createdSid=$null; $admin=$null; $tx=$null; $pointer=[IntPtr]::Zero; $literal=$null; $sql=$null
        try {
            $admin=Open-Connection 'master'; $tx=$admin.BeginTransaction()
            if ([int](Scalar $admin "SELECT CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly'))" $tx) -ne 0) { throw 'MODE_CHANGED' }
            if ([int](Scalar $admin "SELECT COUNT(*) FROM sys.server_principals WHERE name=N'$($item.Name)'" $tx) -ne 0) {
                Record 'CREATE LOGIN' $item.Name 'master' 'CONFLICT'; $tx.Rollback(); $tx.Dispose(); $tx=$null
            } else {
                $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($script:secrets[$item.Name])
                $literal=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer).Replace("'","''")
                $sql="CREATE LOGIN [$($item.Name)] WITH PASSWORD=N'$literal', CHECK_POLICY=ON, CHECK_EXPIRATION=ON, DEFAULT_DATABASE=[$($item.Default)];"
                Execute $admin $sql $tx
                $sql=$null; $literal=$null
                $createdSid=[string](Scalar $admin "SELECT CONVERT(varchar(170),sid,1) FROM sys.sql_logins WHERE name=N'$($item.Name)' AND is_policy_checked=1 AND is_expiration_checked=1" $tx)
                if ($createdSid -notmatch '^0x[0-9A-Fa-f]+$') { throw 'CREATED_IDENTITY_PRECONDITION' }
                $tx.Commit(); $tx.Dispose(); $tx=$null; $loginCreated=$true
                Record 'CREATE LOGIN' $item.Name 'master' 'COMMITTED'
            }
        } catch {
            if ($null -ne $tx) { try { $tx.Rollback() } catch {} }
            Record 'CREATE LOGIN' $item.Name 'master' 'FAILED' (ErrorNumber $_)
        } finally {
            $sql=$null; $literal=$null
            if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
            if ($null -ne $tx) { $tx.Dispose() }; if ($null -ne $admin) { $admin.Dispose() }
        }
        if (!$loginCreated) { foreach($db in $item.Databases) { Record 'CREATE USER / ALTER ROLE' $item.Name $db 'SKIPPED_DEPENDENCY' }; continue }
        foreach($db in $item.Databases) {
            $connection=$null; $tx=$null
            try {
                $connection=Open-Connection $db; $tx=$connection.BeginTransaction()
                if ([int](Scalar $connection "SELECT COUNT(*) FROM sys.sql_logins WHERE name=N'$($item.Name)' AND sid=$createdSid AND is_policy_checked=1 AND is_expiration_checked=1" $tx) -ne 1) { throw 'LOGIN_CHANGED' }
                if ([int](Scalar $connection "SELECT COUNT(*) FROM sys.database_principals WHERE name=N'$($item.Name)'" $tx) -ne 0) {
                    Record 'CREATE USER / ALTER ROLE' $item.Name $db 'CONFLICT'; $tx.Rollback(); continue
                }
                if ([int](Scalar $connection "SELECT COUNT(*) FROM sys.database_principals WHERE name=N'$($item.Role)' AND type='R' AND owning_principal_id=DATABASE_PRINCIPAL_ID(N'dbo')" $tx) -ne 1) { throw 'ROLE_PRECONDITION' }
                Execute $connection "CREATE USER [$($item.Name)] FOR LOGIN [$($item.Name)] WITH DEFAULT_SCHEMA=[dbo];" $tx
                Execute $connection "ALTER ROLE [$($item.Role)] ADD MEMBER [$($item.Name)];" $tx
                $tx.Commit(); $tx.Dispose(); $tx=$null
                Record 'CREATE USER' $item.Name $db 'COMMITTED'; Record 'ALTER ROLE ADD MEMBER' $item.Name $db 'COMMITTED'
            } catch {
                if ($null -ne $tx) { try { $tx.Rollback() } catch {} }
                Record 'CREATE USER / ALTER ROLE' $item.Name $db 'FAILED_ROLLED_BACK' (ErrorNumber $_)
            } finally { if ($null -ne $tx) { $tx.Dispose() }; if ($null -ne $connection) { $connection.Dispose() } }
        }
        foreach($db in @('KulaniKDI_Dev','KulaniKDI_Test')) {
            $connection=$null
            try {
                $connection=Open-Connection $db $item.Name
                Observe-Permissions $connection $item $db
            } catch {
                $number=ErrorNumber $_; $result='FAILED'
                if ($item.Name -eq 'kdi_test_runner' -and $db -eq 'KulaniKDI_Dev' -and $number -in @(4060,916)) { $result='EXPECTED_DEV_ACCESS_DENIED' }
                Record 'SQL AUTH' $item.Name $db $result $number
            } finally { if ($null -ne $connection) { $connection.Dispose() } }
        }
    }
    } # end mutation/authentication mode; Preflight never enters this block
} catch { Record 'EXECUTION' '' '' 'STOPPED' (ErrorNumber $_) }
finally {
    foreach($secret in $script:secrets.Values) { $secret.Dispose() }; $script:secrets.Clear()
    $script:events | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:output -Encoding UTF8
}
# Exit status does not claim QA PASS. Detailed bounded outcomes are in the sanitized evidence.
if (@($script:events | Where-Object { $_.result -match 'FAILED|FAIL_|STOPPED|CONFLICT|SKIPPED' }).Count -gt 0) { exit 1 }
exit 0
