# D2-A API SQL identity preparation — design decision required

TaskInput: KDI / D2A-API-IDENTITY / identities-20261018 / attempt 1.
Effective role: engineer. Dispatch: /root/d2a_api_identity_prepare.
Authorization: preparation only, outside frozen D2-A; no SQL sessions or persistence.

## Current technical findings

`src/Kulani.KDI.Infrastructure/Persistence/SqlTarget.cs` requires Windows integrated authentication and rejects UserID, Password and Authentication. `Program.cs` accepts only a connection string. SQL authentication for kdi_app cannot work with the frozen API. This is a current product contract change requiring Human Orchestrator approval, not a missing password setting.

`RuntimePrivilegeInterceptor` already checks the SQL server/database, absence of sysadmin/dbcreator/db_owner/CONTROL/DDL privileges and membership in kdi_application on every opened runtime EF connection. Preserve these checks. The existing schema has no business entities; the application role deliberately has no business DML grants. A readiness 200 establishes connectivity and the interceptor check, not business persistence or full migration state.

`tools/smoke-api.py` writes evidence and OpenAPI into docs/stage3/d2a and expects readiness 503. Do not run it against this frozen candidate. `SqlBaselineTests` uses integrated administrative authentication, EXECUTE AS witnesses and EF migration/downgrade operations; it does not authenticate the new SQL identities. Do not reuse it as independent SQL authentication evidence or run it without migration authorization.

## Proposed minimal change (not applied)

Review `sql-runtime-credential.patch`. Keep the current integrated path intact. Add a separate explicit console credential path with hardcoded kdi_app identity, connection metadata without UserID/Password, and Microsoft.Data.SqlClient.SqlCredential(SecureString). The API itself reads a password from a real local interactive console; no launcher passes it via args, environment, files, stdin tool calls or configuration providers. No secret-bearing connection string is built. Each scoped DbContext owns its SqlConnection; the credential lives for the process and the secure password is disposed after host shutdown.

The nonsecret connection configuration must explicitly select DESKTOP-F25O3MF and KulaniKDI_Dev (or Test for a separate approved check), Integrated Security=false, Encrypt=true, Persist Security Info=false. Local TrustServerCertificate=true may preserve the existing approved development exception; no global TLS settings change. Do not launch using `dotnet run` or alter launchSettings. Run the built API directly from its output directory, Production environment, loopback-only URL. No secret is placed in environment. Do not collect process memory dumps, configuration dumps or EF sensitive logging; never capture raw exception or SQL statement text from credential provisioning. This proposal does not authorize any OS/global diagnostic setting change.

After approval and patch validation, the human runs a local console, sets only nonsecret connection metadata and loopback hosting values for that process, and executes:

```powershell
dotnet .\Kulani.KDI.Api.dll --kdi-sql-console-credential
```

The console prompts `Password for kdi_app (not echoed):`. The human types there directly. This agent must not request that password in conversation or use write_stdin to transmit it. If the available execution surface cannot provide private interactive input, credential entry remains BLOCKED until an approved human console is available. No GUI is launched by this preparation.

## Required validation after separate approval

1. Compile the patch in a new authorized candidate/worktree, run unit, API and architecture regression suites. New unit tests cover credential-mode allowlist, forbidden embedded secrets, target/integrated-auth mismatch, failover/attach/persist/encryption guards. Existing integrated tests must remain green.
2. Auth three independent real sessions with SqlCredential, one per kdi_migrator, kdi_app, kdi_test_runner. Each session records only ORIGINAL_LOGIN(), SUSER_SNAME(), USER_NAME(), DB_NAME(), success and permission booleans. No EXECUTE AS substitutes for authentication. Passwords are distinct and private; no login without valid password must succeed.
3. DBA controls exact positive/negative matrix. Read permitted migration histories as migrator; verify its approved DDL permissions through HAS_PERMS_BY_NAME without invoking migration. Application session verifies kdi_application membership, absence of server/admin and schema DDL privileges. Test runner authenticates to Test and reads allowed histories; Dev must reject connection or all modification permissions. Do not run a mutating test merely to prove denial unless specifically authorized; transaction rollback is still a write attempt.
4. For actual negative DDL, use a separately authorized bounded transaction probe only in approved Test and confirm rejection; a success is a security failure even if rolled back. Permission metadata evidence alone must be labelled as such. No CREATE TABLE, UPDATE, CREATE LOGIN or GRANT is authorized by this proposal.
5. Start the API with private console kdi_app password and approved nonsecret metadata. Run `python <engineer-directory>/connected-smoke.py --port <loopback-port>`. The script issues GET/POST HTTP requests only, checks readiness 200, exact liveness/metadata/OpenAPI and error contracts, and writes fixed-shape sanitized evidence only beside itself. It does not start a host or query SQL.
6. Negative API run under an existing authorized overprivileged identity must retain runtime rejection; test wrong password only through secure local entry, capturing a generic failure result. No secret or provider exception in evidence. No claim that public HTTP readiness alone proves the login identity: pair it with the dedicated authenticated SQL session and process launch record.
7. Freeze a new candidate and manifest; independent QA and Code Review evaluate identical hashes. Human accepts D2-A. Existing candidate and all original D2-A evidence stay unchanged.

## Limits

Patch is a reviewable design draft; it has not been applied, compiled or run. `git apply --check --ignore-whitespace` verifies applicability against current files; the option accommodates existing CRLF. No product path changed. No database, SQL login, permission, global config, IDI, D2-B/C or deployment touched. Product implementation and connected tests remain gated on approval, identity provisioning and secure human input. Preparation completion does not mean D2-A PASS.

SqlCredential reference supplied by principal: https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlcredential .
