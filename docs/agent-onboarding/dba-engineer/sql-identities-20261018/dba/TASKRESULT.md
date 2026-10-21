# TaskResult 1.0 — SQL identity review, phase 1

projectId KDI; taskId D2A-SQL-IDENTITIES; runId identities-20261018; attemptId 1.
Effective agent_type: dba-engineer. Dispatch/task: /root/d2a_sql_identity_review.
Result: phase-1 metadata checks PASS; provisioning and independent authentication NOT VERIFIED (not authorized/executed). End-to-end D2-A is not PASS.

## Current observations

Read-only sqlcmd Windows-auth queries to DESKTOP-F25O3MF: master metadata and KulaniKDI_Dev/Test. No other database queried. Server reports IsIntegratedSecurityOnly=0. Current metadata reader is sysadmin, solely for authorized administrative metadata inspection; application must never use that credential. All three requested login names absent; no matching server-role membership. All three proposed database usernames absent in both databases.

Both databases have kdi_migrations/kdi_application/kdi_tests owned by dbo; current members are only matching WITHOUT LOGIN witnesses. Schemas identity/organizations/portfolio owned by dbo, each containing __EFMigrationsHistory. No guest CONNECT grant appears; public grants seen are system-catalog SELECT and column-encryption metadata, not application schema DML or DDL. These observations are not independent authentication tests.

The three existing roles match frozen PRIVILEGES.md: migrations CONNECT, CREATE TABLE and schema ALTER/DML/REFERENCES/VIEW DEFINITION; application CONNECT with DENY CREATE TABLE/VIEW/PROCEDURE/FUNCTION/SCHEMA and schema ALTER; tests CONNECT and schema SELECT, no write grants. No new grants or role definitions proposed.

## Exact proposed binding

See PROPOSAL-NOT-EXECUTED.sql: three CREATE LOGIN templates, five CREATE USER and five ALTER ROLE statements. No SQL executed from that file. kdi_migrator maps to migrations in Dev and Test. kdi_app maps to application in Dev and Test. kdi_test_runner maps only to tests in Test; absent in Dev. This is less access than mapping the test identity into both databases and satisfies the required Dev isolation. Default dbo schema grants no rights on dbo. No administrative roles or additional grants.

Existing frozen documentation proposed Windows authentication and states SQL authentication was not proposed. The human's new request authorizes design of this replacement, not mutation of the old frozen candidate. New SQL-auth design/API support requires a new candidate and independent review. Principal reports current API SqlTarget.Validate only supports Windows authentication: readiness with SQL identity remains blocked on a separately approved implementation.

## Secure execution procedure after specific human authorization

Human authorizes the exact three CREATE LOGIN, five CREATE USER and five ALTER ROLE operations for this fixed server and databases, including policy/expiration settings. Provisioner first repeats preconditions in same administrative session: fixed server, mixed mode 0, names absent, roles and guest state unchanged. Abort on conflict; never silently reuse/alter an existing identity.

Use a local private interactive PowerShell console, transcript disabled, no shell tracing and no statement/error-payload logging. Prompt each password with Read-Host -AsSecureString, confirm privately; never send passwords through tool inputs/conversation, environment variables, command-line arguments or files. Do not install SMO. A narrowly scoped reviewed helper can use System.Data.SqlClient and Windows integrated administrative connection; construct CREATE LOGIN only in process memory with fixed allowlisted identifiers and escaped single-quote password text, execute without echo, clear unmanaged secret buffers immediately in finally and dispose connections/SecureStrings. Since T-SQL CREATE LOGIN PASSWORD requires a literal, a transient managed string may exist; minimize lifetime and never serialize/log it. Emit only operation ID/name/success or sanitized numeric error. If available existing SMO SecureString API avoids custom literal handling, but is not assumed installed. Do not run an unreviewed helper. The present delivery documents the procedure; no secret prompt/helper execution has occurred.

Then create users and memberships with nonsecret fixed statements. Verify each identity with a separate System.Data.SqlClient.SqlCredential using its SecureString and an explicit target database; SQL auth password stays outside connection string. Use encrypted connections with a trusted server certificate; any local certificate trust exception must be explicit and documented, never disable encryption silently. Application configuration must keep credentials outside tracked configuration and logs. Expiration ON requires an operational rotation process and readiness monitoring; no blanket exemption proposed.

## Postprovision test plan (pending authorization)

1. For every independent SQL login: record ORIGINAL_LOGIN()/SUSER_SNAME()/USER_NAME()/DB_NAME(), not secrets. Confirm sysadmin/securityadmin/server privileges absent and expected role membership only; inspect public inheritance again.
2. Migrator Dev/Test: positive HAS_PERMS_BY_NAME CREATE TABLE and schema ALTER/DML/REFERENCES; negative CREATE SCHEMA and ALTER dbo/server permissions. Actual transactional DDL/DML probe requires explicit operation authorization, even if rollback planned.
3. App Dev/Test: positive CONNECT/SELECT 1/readiness; negative HAS_PERMS_BY_NAME CREATE TABLE/VIEW/PROCEDURE/FUNCTION/SCHEMA, schema ALTER and history-table DML/SELECT. To prove actual DDL rejection, a unique Test-only CREATE TABLE attempt can be executed only after explicit authorization; use transaction and unconditional rollback if unexpectedly permitted, fail test if succeeds.
4. Test runner Test: positive SELECT on each schema.__EFMigrationsHistory; negative schema/table INSERT/UPDATE/DELETE and all DDL. Do not substitute metadata permission checks for actual denied operation evidence when required by acceptance.
5. Test runner Dev: new connection with explicit Dev must fail access (e.g. 916/4060), and successful Test connection remains control. Absence of mapping, guest CONNECT and server privilege checks support isolation; authentication failure to all targets would not prove isolation. Do not execute Dev DML probe because intended denial is database access itself.
6. Run API as kdi_app only after reviewed SQL-auth support; readiness SQL 200 and connected smoke tests. Existing role intentionally has no business DML for D2-A; do not grant extra rights just to make later-phase tests pass.

## Evidence and limits

server-metadata.txt, dev-metadata.txt, test-metadata.txt are current raw nonsecret query outputs; phase1-database-metadata.sql reproduces database query. Successful SQL commands used sqlcmd -S DESKTOP-F25O3MF -E -d <master|KulaniKDI_Dev|KulaniKDI_Test> -l 10 -b -r 1 -W with -Q server SELECT or -i database query and -o evidence path. Each successful execution exited 0. Initial sandbox master attempt failed ODBC17 SSL/SSPI/network; required escalated retry succeeded. No interpretation of that sandbox failure as server failure. No SQL mutations, provisioning, password entry, global setting change, deployment, IDI or other product access. No frozen candidate edited. Future rollback is not automatic: user/login removal requires separately reviewed authorization, dependency and ownership checks; do not drop users/logins on partial failure implicitly.
