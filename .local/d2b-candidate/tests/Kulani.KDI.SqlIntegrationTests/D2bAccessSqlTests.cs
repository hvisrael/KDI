using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using Kulani.KDI.Application.Access;
using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.SqlIntegrationTests;

// Requires separate express approval for Test fixtures and DML cleanup; never enabled by the baseline test switch.
public sealed class D2bAccessSqlTests
{
    private const string CaseName = "D2bAccessSqlTests.Revoke_wins_over_stale_renewal_and_organization_membership_is_authoritative";

    [Fact]
    public async Task Revoke_wins_over_stale_renewal_and_organization_membership_is_authoritative()
    {
        if (Environment.GetEnvironmentVariable("KDI_D2B_SQL_AUTHORIZED") != "TEST-FIXTURES-AND-CLEANUP")
            throw new InvalidOperationException("BLOCKED: express D2-B SQL fixture authorization required.");
        // The identity of the harness is resolved explicitly: the Windows identity of the process or
        // the login kdi_test_runner, whose password is read from the private interactive console. The
        // credential travels with the connection and never with the connection string.
        var resolved = SqlTestIdentity.Resolve();
        var connection = resolved.ConnectionString;
        var credential = resolved.Credential;
        var run = SqlTestRun.Current;
        // The known warnings of the stage are written to the output of the case before anything else, so that
        // they are visible even when the case fails before it writes the rest of its evidence.
        run.WriteKnownWarnings();
        var ct = TestContext.Current.CancellationToken;
        await using var i = Identity(connection, credential); await using var o = Organizations(connection, credential);
        var store = new SqlAccessStore(i, o); var now = DateTime.UtcNow; var orgId = Guid.NewGuid();
        // The temporary organization is registered before it exists, so that an interrupted run still leaves
        // the exact identifier of the row that has to be removed by hand.
        run.RegisterOrganization(orgId, CaseName);
        // The subject of the synthetic user is a random unique identifier and it is registered before the
        // user is created: if the process dies while the user is being created, the row can still be located
        // by its subject in identity.Users, even when its identifier was never returned to the harness.
        var subject = Guid.NewGuid().ToString("N");
        run.RegisterSubject(subject, CaseName);
        var userId = Guid.Empty;
        var sessionId = Guid.Empty;
        Exception? failure = null;
        try
        {
            var user = await store.CreateUserAsync("https://synthetic.invalid/", subject, "Synthetic fixture", now, run.Correlation, ct);
            userId = user.Id;
            run.RegisterUser(user.Id, CaseName);
            var session = new StoredSession(Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), user.Id, [1, 2], now, now, now.AddHours(1), now.AddMinutes(30), null, []);
            sessionId = session.Id;
            o.Organizations.Add(new() { Id = orgId, Code = "TEST_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), Name = "Synthetic fixture" });
            o.Memberships.Add(new() { Id = Guid.NewGuid(), OrganizationId = orgId, UserId = user.Id });
            await o.SaveChangesAsync(ct);
            i.Grants.Add(new() { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = orgId, ScopeId = orgId, PermissionCode = "projects.read" }); await i.SaveChangesAsync(ct);
            Assert.Single(await store.GetOrganizationsAsync(user.Id, ct));
            Assert.Single((await store.GetOrganizationAccessAsync(user.Id, orgId, ct))!.Permissions);
            Assert.Null(await store.GetOrganizationAccessAsync(user.Id, Guid.NewGuid(), ct));
            await store.CreateSessionAsync(session, run.Correlation, ct);
            var observed = (await store.GetSessionAsync(session.HandleHash, ct))!;
            await using var i2 = Identity(connection, credential); await using var o2 = Organizations(connection, credential);
            var other = new SqlAccessStore(i2, o2);
            await other.RevokeSessionAsync(session.HandleHash, now.AddSeconds(1), run.Correlation, ct);
            Assert.False(await store.RenewSessionAsync(observed, now.AddSeconds(2), now.AddMinutes(30), [3], ct));
            var revoked = (await store.GetSessionAsync(session.HandleHash, ct))!;
            Assert.NotNull(revoked.RevokedAtUtc); Assert.Null(revoked.ProtectedTicket);
            await o.Memberships.Where(x => x.UserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);
            Assert.Empty(await store.GetOrganizationsAsync(user.Id, ct));
            Assert.Null(await store.GetOrganizationAccessAsync(user.Id, orgId, ct));
        }
        catch (Exception error) { failure = error; }
        var report = await CleanSafely(i, o, orgId, userId, sessionId);
        run.RecordCleanup(report, CaseName);
        SqlTestOutput.Write("d2b-sql-run artifact: " + run.ArtifactPath + "; run=" + run.RunId.ToString("N") + "; case=" + CaseName + "; cleanupFailures=" + report.Statements.Count(x => x.Error is not null) + "; verificationFailures=" + report.Checks.Count(x => x.Error is not null) + "; counts=" + report.DescribeChecks() + "; residuals=" + report.DescribeResiduals());
        if (failure is not null)
        {
            // The original failure of the case is the one that propagates. The failures of the cleanup are not
            // lost: they are in the artifact and in the output line above.
            if (report.HasFailures || report.HasResidue) SqlTestOutput.Write("BLOCKED: the cleanup of " + CaseName + " did not complete while the case was failing: " + report.DescribeFailures() + "; residuals=" + report.DescribeResiduals());
            ExceptionDispatchInfo.Capture(failure).Throw();
            return;
        }
        // A cleanup that failed is a fact that cannot pass unnoticed, even when no residue is left.
        if (report.HasFailures || report.HasResidue) throw new InvalidOperationException("BLOCKED: the cleanup of " + CaseName + " did not complete: " + report.DescribeFailures() + "; residuals=" + report.DescribeResiduals() + ". The exact identifiers are in " + run.ArtifactPath + " and need a reviewed manual removal.");
    }

    // The cleanup of the case is the last thing that runs and its failures are captured here, so that they
    // can never replace the failure of the case, and the report is always complete enough to be written to
    // the artifact.
    private static async Task<CleanupReport> CleanSafely(IdentityDbContext identity, OrganizationsDbContext organizations, Guid orgId, Guid userId, Guid sessionId)
    {
        try { return await Clean(identity, organizations, orgId, userId, sessionId); }
        catch (Exception error)
        {
            var report = new CleanupReport();
            report.Statements.Add(new CleanupStatementResult { Statement = "clean.unhandled", Error = SqlCleanup.Describe(error) });
            return report;
        }
    }

    // The cleanup owns its token, so a cancelled or aborted case still removes its own rows. Every statement
    // and every verification is isolated, so one failure does not skip the following ones, and the residue
    // is verified afterwards. The failures of the cleanup are captured and returned in the report, so that
    // the report is always written to the artifact and the case never loses the evidence of what happened.
    //
    // The statements are not atomic either: they run in autocommit, one after the other, and an interruption
    // between them leaves a partial state. The order respects the foreign keys: sessions, grants, audit,
    // memberships, organizations and finally the users.
    private static async Task<CleanupReport> Clean(IdentityDbContext identity, OrganizationsDbContext organizations, Guid orgId, Guid userId, Guid sessionId)
    {
        var report = new CleanupReport();
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var token = cleanup.Token;
        try { await CleanCore(report, identity, organizations, orgId, userId, sessionId, token); }
        catch (Exception error)
        {
            report.Statements.Add(new CleanupStatementResult { Statement = "clean.unhandled", Error = SqlCleanup.Describe(error), Note = error is SqlScopeRefusal ? error.Message : null });
        }
        return report;
    }

    private static async Task CleanCore(CleanupReport report, IdentityDbContext identity, OrganizationsDbContext organizations, Guid orgId, Guid userId, Guid sessionId, CancellationToken token)
    {
        var run = SqlTestRun.Current;
        // The scope of the audit rows is the union of the three ways of recognizing a row of the case: the
        // user of the case as ActorId, the user or the session of the case as TargetId, or the organization
        // of the case as OrganizationId. The rows written by SqlAccessStore carry the user as ActorId and
        // the session as TargetId, so every row of the case is inside the union. The operator of the run is
        // not used as a delete key, so this case never removes the audit rows of another case of the run.
        var caseActorIds = userId == Guid.Empty ? [] : new[] { userId.ToString() };
        var targetIds = userId == Guid.Empty
            ? []
            : sessionId == Guid.Empty ? [userId] : new[] { userId, sessionId };
        var organizationIds = new[] { orgId };
        // The scopes are built here, before the first delete statement, so that the guard of the operator of
        // the run is evaluated eagerly and a malformed operator refuses the whole cleanup of the case and
        // not only the statements of the audit. The predicates are built once and reused by the delete and
        // by the verification. The same form is used by the administration class.
        var identityAuditScope = identity.AuditEntries.Where(SqlAuditScope.Identity(run.OperatorSid, caseActorIds, targetIds, organizationIds));
        var organizationAuditScope = organizations.AuditEntries.Where(SqlAuditScope.Organization(run.OperatorSid, caseActorIds, targetIds, organizationIds));
        await SqlCleanup.IsolatedAsync(report, "identity.Sessions", t => identity.Sessions.Where(x => x.UserId == userId).ExecuteDeleteAsync(t), token);
        await SqlCleanup.IsolatedAsync(report, "identity.Grants", t => identity.Grants.Where(x => x.UserId == userId || x.OrganizationId == orgId).ExecuteDeleteAsync(t), token);
        await SqlCleanup.IsolatedAsync(report, "identity.AuditEntries", t => identityAuditScope.ExecuteDeleteAsync(t), token);
        await SqlCleanup.IsolatedAsync(report, "organizations.AuditEntries", t => organizationAuditScope.ExecuteDeleteAsync(t), token);
        await SqlCleanup.IsolatedAsync(report, "organizations.OrganizationMemberships", t => organizations.Memberships.Where(x => x.UserId == userId || x.OrganizationId == orgId).ExecuteDeleteAsync(t), token);
        await SqlCleanup.IsolatedAsync(report, "organizations.Organizations", t => organizations.Organizations.Where(x => x.Id == orgId).ExecuteDeleteAsync(t), token);
        await SqlCleanup.IsolatedAsync(report, "identity.Users", t => identity.Users.Where(x => x.Id == userId).ExecuteDeleteAsync(t), token);
        await SqlCleanup.RecordResidualAsync(report, "identity.Sessions", identity.Sessions.Where(x => x.UserId == userId), x => x.Id, token);
        await SqlCleanup.RecordResidualAsync(report, "identity.Grants", identity.Grants.Where(x => x.UserId == userId || x.OrganizationId == orgId), x => x.Id, token);
        await SqlCleanup.RecordResidualAsync(report, "identity.AuditEntries", identityAuditScope, x => x.Id, token);
        await SqlCleanup.RecordResidualAsync(report, "organizations.AuditEntries", organizationAuditScope, x => x.Id, token);
        await SqlCleanup.RecordResidualAsync(report, "organizations.OrganizationMemberships", organizations.Memberships.Where(x => x.UserId == userId || x.OrganizationId == orgId), x => x.Id, token);
        await SqlCleanup.RecordResidualAsync(report, "organizations.Organizations", organizations.Organizations.Where(x => x.Id == orgId), x => x.Id, token);
        await SqlCleanup.RecordResidualAsync(report, "identity.Users", identity.Users.Where(x => x.Id == userId), x => x.Id, token);
    }

    private static IdentityDbContext Identity(string c, SqlCredential? credential) => new(Options<IdentityDbContext>(c, credential, "identity"));
    private static OrganizationsDbContext Organizations(string c, SqlCredential? credential) => new(Options<OrganizationsDbContext>(c, credential, "organizations"));
    // The context never receives the password in a string: with a credential of the harness the
    // connection is built with the credential and the connection string stays free of any identity.
    private static DbContextOptions<T> Options<T>(string c, SqlCredential? credential, string schema) where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        if (credential is null) builder.UseSqlServer(c, s => s.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        else builder.UseSqlServer(new SqlConnection(c, credential), s => s.MigrationsHistoryTable("__EFMigrationsHistory", schema));
        return builder.Options;
    }
}
