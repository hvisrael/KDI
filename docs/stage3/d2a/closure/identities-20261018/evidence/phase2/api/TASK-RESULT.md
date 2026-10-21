# TaskResult1.0 — D2A-P2-API

Agent: engineer, `/root/d2a_phase2_api_implementation`. Run: phase2-20261018 attempt1.
Status: IMPLEMENTED_AND_OFFLINE_VALIDATED; connected SQL/API verification pending. No overall D2-A PASS claimed.

## Ownership and scope

Exclusive writes: `.local/d2a-phase2-candidate/` and this `api/` evidence directory. All 121 manifest entries were hash checked then copied byte-for-byte before applying the external draft. Original sources, tests, tools, manifest and frozen evidence were not edited. Manifest SHA256: `66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f`. All 121 original entry hashes were verified again after build/tests. `source-delta.json` lists the three modified source paths and original/new hashes. Added test: `tests/Kulani.KDI.UnitTests/SqlCredentialTargetTests.cs` in the snapshot.

## Changes

- `SqlTarget`: allows SQL runtime identity only via a separate `SqlCredential` restricted to `kdi_app`; continues rejecting embedded user/password, integrated+SQL combination, unsafe targets/options, optional encryption, and non-Test targets in test-only mode. Windows integrated path remains available.
- `PersistenceRegistration`: requires explicit target with credentials; each of three DbContexts owns its own SqlConnection while sharing the runtime credential. Existing privilege interceptor stays attached to every runtime context.
- `Program`: explicit `--kdi-sql-console-credential` reads a non-echoed password directly into read-only SecureString; input/output redirection rejected. SecureString lives until the API stops and is disposed afterward. No secret in file, args, environment, configuration or report. For this mode, EF database/update and exception-handler diagnostic categories are suppressed to avoid SQL exception payloads in logs. Public error/readiness contracts unchanged.
- `Start-KdiSqlApi.ps1`: local visible interactive launcher; Production, loopback only, Test by default, exact approved server. Metadata contains no password. Local development certificate exception (`TrustServerCertificate=true`) matches the reviewed proposal; no global TLS change. Never enable transcripts, input capture, diagnostic dumps or sensitive EF logging during credential entry/runtime.

## Verification

`restore.log`: first sandbox attempt failed NU1900 reaching NuGet vulnerability service. Approved elevated retry `restore-elevated.log` succeeded with locked dependencies and audit enabled. No package/stack changes.

- Release full solution build: exit 0, zero warnings/errors (`build.log`).
- Unit tests: 36/36, zero skipped (`unit.log`). New tests check valid/invalid metadata, runtime identity, explicit target, test-only preservation, and distinct owned closed connections/credential references across all three contexts. No connection is opened by these tests.
- API tests: 7/7, zero skipped (`api-tests.log`), unconfigured readiness/public contracts.
- Architecture tests: 24/24, zero skipped (`architecture.log`).
- Launcher PowerShell AST parse: zero errors.
- Original D2-A: all121 SHA256 entries match. `Verify-Original.ps1` contains repeatable read-only verification; direct invocation was blocked by existing unsigned-script execution policy. Policy was not changed; equivalent hash checks were executed inline successfully.

## Local start and remaining work

From an authorized visible local interactive PowerShell console at workspace root:

```powershell
& .\docs\agent-onboarding\dba-engineer\sql-identities-phase2-20261018\api\Start-KdiSqlApi.ps1 -Database KulaniKDI_Test -Port 5088
```

The existing host policy currently requires signed scripts, so the principal must choose an authorized execution host or approved process-only invocation. Do not change global execution policy. Enter the password only in the visible API prompt, never through tooling stdin or conversation. Verify `/health/ready` returns200 only after identities are provisioned. The source preserves Windows integrated authentication for callers that omit the opt-in flag.

Not executed: launcher/runtime prompt, SQL connections, SQL integration tests, migrations, readiness200, connected smoke. These await provisioning and local protected credential entry. No IDI access, global configuration, deployments, or redelegation. Principal owns integration, freeze, independent QA/review and acceptance; this TaskResult does not self-approve the candidate.
