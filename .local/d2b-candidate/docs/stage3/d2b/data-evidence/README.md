# D2-B persistence implementation — review candidate only

TaskInput: project KDI, task D2B-IMPLEMENT-DATA, run d2b-20261018, attempt 1. Effective specialist: dba-engineer, task /root/d2b_data. Candidate: `.local/d2b-candidate`, based on approved D2-A c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973.

Implemented eight module-owned tables, exact issuer/subject uniqueness including byte lengths, module-local foreign keys, active grant uniqueness, rowversions, session time constraints, fixed eight-permission catalog and organization ASCII code constraint. No changes to the historical Initial migrations or Portfolio. Updated EF snapshots represent the new additive migrations.

Runtime store composes both contexts behind IAccessStore. User/session creation and revocation audit atomically; duplicate identity creation resolves the existing exact identity. Session renewal is a rowversion compare-and-swap with live user and expiration predicates, never an upsert. Organization access rechecks active user, organization, membership and active grants.

## Offline validation

- Infrastructure, migrations tool, AccessAdmin and SQL integration test projects compile with zero warnings/errors.
- Selected PersistenceTests and D2bDataModelTests: 18/18 pass. Assertions preserve empty historical baseline migration target and schema ownership; current models now have five and three entities respectively.
- EF official design-time services generated additive migrations and static snapshots offline. SQL scripts under proposed-sql are generated idempotently with server/database/history guards. No connection opened during generation.
- SQL integration test D2bAccessSqlTests is present but NOT RUN; explicitly requires `KDI_D2B_SQL_AUTHORIZED=TEST-FIXTURES-AND-CLEANUP` plus existing authorized Test-only connection. It exercises real SQL membership isolation and stale renewal after revocation. Missing authorization fails BLOCKED rather than silently passing.
- Five additional gated tests target the actual privileged administration engine: exact identity case/trailing-space/cross-issuer uniqueness, concurrent last-admin removal, disabled-user versus membership-enable, atomic rollback after audit insert failure, and duplicate concurrent bootstrap. They compile but are NOT RUN. Actual SQL upgrade, repeat migration, permission probes, concurrent races, rollback and SQL execution plans remain NOT VERIFIED. Approval and Test execution are required before integral PASS.

## Operator tooling

No automatic migrations or seed business users on API startup. No external identities created. Operator tools accept only the two KDI databases; runtime SqlTarget remains restricted to kdi_app. Both use an interactive protected console to collect kdi_migrator's password; password never appears in arguments, environment, evidence or files. Encryption remains enabled, with TrustServerCertificate for this existing local development server only.

Generate reviewed migration SQL offline:

```text
dotnet run --project tools/Kulani.KDI.Migrations -- KulaniKDI_Test <review-output>
```

Apply only after express SQL authorization, one module at a time, passing the companion `.migration.sha256` value (hash of exact EF script) as the final argument:

```text
dotnet run --project tools/Kulani.KDI.Migrations -- --apply KulaniKDI_Test identity --approve-sha256 <approved-hash>
```

Migration runner requires an existing nonempty approved history prefix and EF version 10.0.13, then applies only known migrations. The scaffolding command is developer-only/offline and never runs in the API.

Administration uses request JSON (identifiers and desired state only): commands `bootstrap`, `membership`, `role`, `user-state`, `organization-state`, `sessions-revoke`, `purge`. Fields are `Command`, `UserId`, `OrganizationId`, `Code`, `Name`, `Active`, `Role`, `ProjectId`; use only command-relevant fields. Fixed roles are Administrator, Manager, Reader, Auditor and RestrictedReader. Bootstrap references an existing active local identity and creates the first organization/membership/admin grants; a replacement administrator is assigned before removing the last administrator.

```text
dotnet run --project tools/Kulani.KDI.AccessAdmin -- plan KulaniKDI_Test request.json plan.json
dotnet run --project tools/Kulani.KDI.AccessAdmin -- apply plan.json <reviewed-plan-sha256>
```

Plan is non-mutating SQL, produces a 30-minute receipt containing actual Windows SID, database, request and exact administrative rowversions. Apply requires identical SID, hash, time and rowversions. All admin mutations acquire transaction-owned exclusive `KDI:IdentityAdmin` lock with 5000ms timeout and validate last-active-admin after writes before commit. Both EF contexts share the SQL transaction; errors roll back. This is an operator boundary, not a product authorization API. A privileged operator can regenerate/approve a plan and must already have express stage/SQL authorization.

Purge clears expired tickets, deletes session metadata seven days after idle expiry, absolute expiry or revocation (whichever ends the session first), and deletes audit older than 30 days. Each application is audited with actual operator SID and correlation; no raw subject/ticket/credential is copied into audit.

## Recovery and limitations

Generated Down migrations drop new D2-B tables only, never shared schemas; they are destructive and MUST NOT run without separate explicit authorization and backup/recovery decision. Preferred runtime rollback is disabling Access configuration while preserving data, then evaluating a forward corrective migration. No automatic downgrade is implemented. Administrator recovery uses the same locked CLI: enable replacement identity/membership, assign Administrator, then disable old identity. All commands preserve last-active-admin invariant for active organizations.

No SQL connections, DDL, DML, GRANT, CREATE LOGIN/USER or ALTER ROLE executed during this implementation. PROPOSED-RUNTIME-GRANTS.sql is review-only. Historical evidence is not re-certified; current observations above are offline validations, not SQL runtime results.

Status: implementation prepared; connected SQL validation BLOCKED pending specific authorization. Not a substitute for independent QA or Code Review.
