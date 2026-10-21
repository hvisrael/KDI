# Backend TaskResult — D2B-IMPLEMENT-BACKEND / attempt 1

Effective role: engineer. Agent task: `/root/d2b_backend`. Owner: API/auth and Application Access policy, new auth unit/API tests, central OIDC package and restore-only transitive lock files.

Implemented in isolated `.local/d2b-candidate`; approved D1/D2-A snapshots were not edited. No SQL connection, DDL, permission change, provider registration, real-user creation, certificate/ACL creation, deployment or global configuration was performed by this task.

## Changes
- Conditional access module (disabled by default) preserving foundation routes/contracts and unknown-route 404; same-origin frontend static files without SPA wildcard fallback.
- Official ASP.NET Core OIDC 10.0.13, code+PKCE, query callback, issuer/audience/signature/lifetime validation, state/nonce timeout five minutes, RS256 only, no offline_access or saved tokens. Exact issuer/subject local lookup, no automatic reactivation or membership; provider role claims discarded.
- SQL ITicketStore through IAccessStore: random 256-bit reference; SHA-256 hash in SQL; encrypted server ticket; ticket-user/row-user match; raw handle excluded from encrypted SQL ticket; 30-minute idle and one-hour absolute; bounded CAS retry then fail closed; local revocation before provider logout; logout_hint from validated sid, no stored ID token.
- Host cookies HttpOnly/Secure/Lax, antiforgery HttpOnly/Secure/Strict, CSRF token/form/header and exact Origin verification. JSON 401/404/503 and no API login redirects; mutation CSRF 403. No CORS.
- Windows DPAPI keyring requires existing external directory with private ACL (current operator/SYSTEM/Administrators); app does not create or grant directory access. OIDC secret is read only at private local console using --kdi-oidc-console-secret, never an argument, environment variable or configuration value. Managed secret string is required transiently by the official middleware.
- Auth-enabled logging whitelist prevents provider-specific configuration from re-enabling framework request URLs, callback codes, cookies, HTTP-client or EF exception diagnostics.
- Application permission policy implements current eight permission codes, mandatory active user/membership, exact organization/project grants and default deny. No Portfolio implementation or HTTP admin endpoint.

## Verification observed
- Escalated official NuGet restore with NuGetAudit=true succeeded. Sandbox network attempt failed and was rerun with approval; no audit disabling.
- API build: 0 warnings, 0 errors (before final added tests; final principal build to be recorded separately).
- Unit suite: 57/57 passed, zero skipped. New auth/policy scenarios include idle/absolute boundaries, ciphertext user substitution, revoked/disabled user, concurrent revoke/renew, CAS retry/exhaustion, raw-handle exclusion, local returnUrl and scope denial.
- API suite: 20/20 passed, zero skipped before final principal run. Includes production cookie/CSRF configurators and endpoints on test host with test-only in-memory IAccessStore and test-only issuer route; no production testing switch. Checks SQL failure 503, organization 404, cookie flags, open redirect, Origin/CSRF denial, OIDC options and exact identity/disabled identity handling.

## Omissions / blocked external validation
- Auth0 tenant/application/synthetic accounts are not provisioned. Real OIDC interoperability, real browser login/logout, provider-session behavior and TLS browser checks are BLOCKED, not PASS.
- SQL migrations/GRANT, real session storage/concurrency and runtime SQL readiness are not executed; require separate reviewed authorization and credentials. Fakes do not substitute for SQL evidence.
- HTTPS certificate/trust and external key-directory/ACL provisioning are not executed. An operator must explicitly authorize and prepare these before access-enabled launch.
- No claim of final approval, independent QA or Code Review is made by this implementer.
