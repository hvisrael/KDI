using System.Runtime.ExceptionServices;
using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.SqlIntegrationTests;

public sealed class D2bAdministrationSqlTests
{
    [Fact]
    public async Task Identity_uniqueness_distinguishes_case_trailing_spaces_and_issuer()
    {
        await using var f = await Fixture.Create("D2bAdministrationSqlTests.Identity_uniqueness_distinguishes_case_trailing_spaces_and_issuer");
        await RunCase(f, async () =>
        {
            var stem = "subjecta" + Guid.NewGuid().ToString("N"); var ids = new HashSet<Guid>();
            foreach (var pair in new[] { ("https://synthetic.invalid/", stem), ("https://synthetic.invalid/", stem + " "), ("https://synthetic.invalid/", stem.ToUpperInvariant()), ("https://other.invalid/", stem) })
            {
                // The subject is registered before the user is created, so that a process that dies during the
                // creation still leaves the values that locate the rows in identity.Users.
                SqlTestRun.Current.RegisterSubject(pair.Item2, f.CaseName);
                var user = await f.Store.CreateUserAsync(pair.Item1, pair.Item2, "Synthetic fixture", DateTime.UtcNow, SqlTestRun.Current.Correlation, TestContext.Current.CancellationToken);
                f.Users.Add(user.Id); SqlTestRun.Current.RegisterUser(user.Id, f.CaseName); Assert.True(ids.Add(user.Id));
                Assert.Equal(user.Id, (await f.Store.FindUserAsync(pair.Item1, pair.Item2, TestContext.Current.CancellationToken))!.Id);
            }
        });
    }

    [Fact]
    public async Task Concurrent_administrators_cannot_remove_each_other_leaving_zero_admins()
    {
        await using var f = await Fixture.Create("D2bAdministrationSqlTests.Concurrent_administrators_cannot_remove_each_other_leaving_zero_admins");
        await RunCase(f, async () =>
        {
            var a = await f.User(); var b = await f.User(); var org = Guid.NewGuid(); f.Organizations.Add(org);
            await f.Apply(new("bootstrap", a, org, "TEST_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), "Synthetic fixture"));
            await f.Apply(new("membership", b, org, Active: true)); await f.Apply(new("role", b, org, Role: "Administrator"));
            var results = await Task.WhenAll(TryApply(f, new("role", a, org, Role: "Reader")), TryApply(f, new("role", b, org, Role: "Reader")));
            Assert.Single(results, x => x); Assert.Single(results, x => !x);
            await using var verify = await Fixture.Create(captureInvariant: false);
            Assert.Equal(1, await verify.Identity.Grants.Where(x => x.OrganizationId == org && x.PermissionCode == "access.manage" && x.RevokedAtUtc == null).CountAsync(TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task Disabled_user_does_not_gain_access_when_membership_is_enabled_concurrently()
    {
        await using var f = await Fixture.Create("D2bAdministrationSqlTests.Disabled_user_does_not_gain_access_when_membership_is_enabled_concurrently");
        await RunCase(f, async () =>
        {
            var admin = await f.User(); var member = await f.User(); var org = Guid.NewGuid(); f.Organizations.Add(org);
            await f.Apply(new("bootstrap", admin, org, "TEST_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), "Synthetic fixture"));
            await Task.WhenAll(f.Apply(new("user-state", member, Active: false)), f.Apply(new("membership", member, org, Active: true)));
            Assert.Null(await f.Store.GetOrganizationAccessAsync(member, org, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task Audit_insert_failure_rolls_back_admin_mutations()
    {
        await using var f = await Fixture.Create("D2bAdministrationSqlTests.Audit_insert_failure_rolls_back_admin_mutations");
        await RunCase(f, async () =>
        {
            var user = await f.User(); var org = Guid.NewGuid(); f.Organizations.Add(org);
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Apply(new("bootstrap", user, org, "TEST_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), "Synthetic fixture"), new string('X', 300)));
            Assert.False(await f.Organization.Organizations.AnyAsync(x => x.Id == org, TestContext.Current.CancellationToken));
            Assert.False(await f.Identity.Grants.AnyAsync(x => x.OrganizationId == org, TestContext.Current.CancellationToken));
            Assert.False(await f.Organization.Memberships.AnyAsync(x => x.OrganizationId == org, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task Concurrent_bootstrap_creates_exactly_one_organization()
    {
        await using var f = await Fixture.Create("D2bAdministrationSqlTests.Concurrent_bootstrap_creates_exactly_one_organization");
        await RunCase(f, async () =>
        {
            var user = await f.User(); var org = Guid.NewGuid(); f.Organizations.Add(org);
            var request = new Request("bootstrap", user, org, "TEST_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), "Synthetic fixture");
            var results = await Task.WhenAll(TryApply(f, request), TryApply(f, request));
            Assert.Single(results, x => x); Assert.Single(results, x => !x);
            Assert.Equal(1, await f.Organization.Organizations.CountAsync(x => x.Id == org, TestContext.Current.CancellationToken));
        });
    }

    private static async Task<bool> TryApply(Fixture f, Request r)
    { try { await f.Apply(r); return true; } catch (InvalidOperationException) { return false; } }

    // Runs the body of a case and then the cleanup. The failure of the body is the one that propagates, the
    // failures of the cleanup are captured in its report, and that report is always written to the artifact,
    // so that the evidence of the cleanup survives both the failure of the case and the failure of the
    // cleanup itself.
    private static async Task RunCase(Fixture f, Func<Task> body)
    {
        Exception? failure = null;
        try { await body(); }
        catch (Exception error) { failure = error; }
        var report = await CleanSafely(f);
        SqlTestRun.Current.RecordCleanup(report, f.CaseName);
        SqlTestOutput.Write("d2b-sql-run artifact: " + SqlTestRun.Current.ArtifactPath + "; run=" + SqlTestRun.Current.RunId.ToString("N") + "; case=" + f.CaseName + "; cleanupFailures=" + report.Statements.Count(x => x.Error is not null) + "; verificationFailures=" + report.Checks.Count(x => x.Error is not null) + "; counts=" + report.DescribeChecks() + "; residuals=" + report.DescribeResiduals());
        if (failure is not null)
        {
            // The original failure of the case is the one that propagates. The failures of the cleanup are not
            // lost: they are in the artifact and in the output line above.
            if (report.HasFailures || report.HasResidue) SqlTestOutput.Write("BLOCKED: the cleanup of " + f.CaseName + " did not complete while the case was failing: " + report.DescribeFailures() + "; residuals=" + report.DescribeResiduals());
            ExceptionDispatchInfo.Capture(failure).Throw();
            return;
        }
        // A cleanup that failed is a fact that cannot pass unnoticed, even when no residue is left.
        if (report.HasFailures || report.HasResidue) throw new InvalidOperationException("BLOCKED: the cleanup of " + f.CaseName + " did not complete: " + report.DescribeFailures() + "; residuals=" + report.DescribeResiduals() + ". The exact identifiers are in " + SqlTestRun.Current.ArtifactPath + " and need a reviewed manual removal.");
    }

    // The cleanup of a case is the last thing that runs and its failures are captured here, so that they
    // can never replace the failure of the case, and the report is always complete enough to be written to
    // the artifact.
    private static async Task<CleanupReport> CleanSafely(Fixture f)
    {
        try { return await f.Clean(); }
        catch (Exception error)
        {
            var report = new CleanupReport();
            report.Statements.Add(new CleanupStatementResult { Statement = "clean.unhandled", Error = SqlCleanup.Describe(error) });
            return report;
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required string Connection { get; init; }
        public required SqlConnection Sql { get; init; }
        public required IdentityDbContext Identity { get; init; }
        public required OrganizationsDbContext Organization { get; init; }
        public string CaseName { get; init; } = "";
        public SqlAccessStore Store => new(Identity, Organization);
        public List<Guid> Users { get; } = [];
        public List<Guid> Organizations { get; } = [];
        public InvariantSnapshot? Invariant { get; private set; }
        public static async Task<Fixture> Create(string caseName = "", bool captureInvariant = true)
        {
            if (Environment.GetEnvironmentVariable("KDI_D2B_SQL_AUTHORIZED") != "TEST-FIXTURES-AND-CLEANUP") throw new InvalidOperationException("BLOCKED: express SQL fixture authorization required.");
            // The identity of the harness is resolved explicitly: the Windows identity of the process
            // or the login kdi_test_runner, whose password is read from the private interactive
            // console. The credential travels with the connection and never with the connection
            // string.
            var resolved = SqlTestIdentity.Resolve();
            var connection = resolved.ConnectionString;
            var sql = SqlTestIdentity.Connect(connection, resolved.Credential);
            await sql.OpenAsync(TestContext.Current.CancellationToken);
            var fixture = new Fixture { Connection = connection, Sql = sql, CaseName = caseName, Identity = new(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(sql).Options), Organization = new(new DbContextOptionsBuilder<OrganizationsDbContext>().UseSqlServer(sql).Options) };
            // The known warnings of the stage are written to the output of the case before anything else, so
            // that they are visible even when the case fails before it writes the rest of its evidence.
            SqlTestRun.Current.WriteKnownWarnings();
            // The photo of the invariant is taken before the case mutates anything, so that a violation that
            // already existed in the database can be told apart from one introduced by this run.
            if (captureInvariant)
            {
                fixture.Invariant = await SqlAdministratorInvariant.CaptureAsync(fixture.Identity, fixture.Organization, SqlTestRun.Current, caseName, TestContext.Current.CancellationToken);
                SqlTestRun.Current.RecordInvariant(fixture.Invariant);
            }
            return fixture;
        }
        public async Task<Guid> User()
        {
            // The subject of the synthetic user is registered before the user is created, so that a process
            // that dies while the user is being created still leaves the value that locates the row.
            var subject = Guid.NewGuid().ToString("N");
            SqlTestRun.Current.RegisterSubject(subject, CaseName);
            var user = await Store.CreateUserAsync("https://synthetic.invalid/", subject, "Synthetic fixture", DateTime.UtcNow, SqlTestRun.Current.Correlation, TestContext.Current.CancellationToken);
            Users.Add(user.Id); SqlTestRun.Current.RegisterUser(user.Id, CaseName);
            return user.Id;
        }
        public async Task Apply(Request request, string? actor = null)
        {
            if (request.OrganizationId is { } organization) SqlTestRun.Current.RegisterOrganization(organization, CaseName);
            await using var other = await Create(captureInvariant: false);
            await using var transaction = await other.Sql.BeginTransactionAsync();
            await other.Identity.Database.UseTransactionAsync(transaction); await other.Organization.Database.UseTransactionAsync(transaction);
            await using var cmd = other.Sql.CreateCommand(); cmd.Transaction = (SqlTransaction)transaction;
            cmd.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'KDI:IdentityAdmin',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000,@DbPrincipal='public'; SELECT @r;";
            if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) is not (0 or 1)) throw new InvalidOperationException("Administration lock unavailable.");
            try { await AccessAdministration.ApplyWithinTransactionAsync(other.Identity, other.Organization, request, actor ?? SqlTestRun.Current.OperatorSid); }
            catch (InvalidOperationException error) when (error.Message.Contains("Last active administrator invariant", StringComparison.Ordinal))
            { throw new InvalidOperationException(DescribeInvariantViolation(request, error), error); }
            await transaction.CommitAsync();
        }
        // The rejection of a command by the global invariant is reported with the exact organizations that
        // violate it, split by origin, so that the cause is actionable and not a generic invariant message.
        // The origin matters: an organization that a case of this run left behind has to be removed by exact
        // identifier, while an organization that was already in the database needs a decision of the operator.
        //
        // This is also the mitigation of the order sensitivity of the harness: xUnit does not guarantee the
        // order of the classes, and the temporary organization of the access case can only still be active
        // when an administration case starts if the cleanup of the access case failed. In that case the
        // organization appears here as a fixture of the run, and the case fails with an actionable message
        // instead of a generic invariant violation.
        private string DescribeInvariantViolation(Request request, Exception error)
        {
            var foreign = Invariant?.ForeignWithoutAdministrator.Select(x => x.Id).ToList() ?? [];
            var fixtures = Invariant?.FixturesWithoutAdministrator.Select(x => x.Id).ToList() ?? [];
            return "BLOCKED: the global administrator invariant was violated while applying the command '" + request.Command + "'. " +
                "AccessAdministration validates every active organization of the database, so the command is rejected when any of them has no active administrator. " +
                "Organizations without an active administrator when this case started, preexisting and foreign to this run (they were in the database before the case, they need a decision of the operator): [" + string.Join(", ", foreign) + "]. " +
                "Of those, the organizations that are fixtures of this run (a case of the run left them behind, remove them by exact identifier): [" + string.Join(", ", fixtures) + "]. " +
                "Organizations created by this case: [" + string.Join(", ", Organizations) + "]. " +
                "The identifiers of the run and the cleanup evidence are in " + SqlTestRun.Current.ArtifactPath + ". " +
                "Original error: " + error.Message;
        }
        // The cleanup owns its token, so a cancelled or aborted case still removes its own rows. Every statement
        // and every verification is isolated, so one failure does not skip the following ones, and the residue
        // is verified afterwards.
        //
        // The seven statements are NOT atomic: they run in autocommit, one after the other. An interruption
        // between them leaves a partial state, for example the temporary organization removed while its user
        // is still present, or a membership left behind when the delete of the memberships fails while the
        // delete of the organizations succeeds. Such an organization would survive without an administrator
        // and would break the global administrator invariant of the following administration cases.
        // Making the cleanup transactional is a decision of the plan and of the human operator, not of the
        // harness, and it is not applied here. What the harness guarantees is the evidence: the report below
        // lists the sequence that was executed, with the affected rows and the error of every statement, and
        // it is written to the artifact of the run, which is the evidence of the manual recovery by exact
        // identifier. The order of the statements respects the foreign keys: sessions, grants, audit,
        // memberships, organizations and finally the users.
        public async Task<CleanupReport> Clean()
        {
            var report = new CleanupReport();
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = cleanup.Token;
            try { await CleanCore(report, token); }
            catch (Exception error)
            {
                // The failures of the cleanup are captured like any other, so that the report is always written
                // and the case never loses the evidence of what happened.
                report.Statements.Add(new CleanupStatementResult { Statement = "clean.unhandled", Error = SqlCleanup.Describe(error), Note = error is SqlScopeRefusal ? error.Message : null });
            }
            return report;
        }
        private async Task CleanCore(CleanupReport report, CancellationToken token)
        {
            var run = SqlTestRun.Current;
            var users = Users.ToArray();
            var organizations = Organizations.ToArray();
            run.RegisterUsers(users, CaseName);
            run.RegisterOrganizations(organizations, CaseName);
            // The scope of the audit rows is the union of the three ways of recognizing a row of the case: a
            // user of the case as ActorId, a user of the case as TargetId, or an organization of the case as
            // OrganizationId. An intersection would leave the rows of the commands without organization,
            // "user-state" and "sessions-revoke", out of the cleanup, and the operator of the run is not used
            // as a delete key, so a case never removes the audit rows of another case of the same run.
            // SqlAuditScope drops the empty identifier sets, so no statement is translated with an empty
            // collection, which would widen the scope instead of narrowing it.
            //
            // The scopes are built here, before the first delete statement, so that the guard of the operator
            // of the run is evaluated eagerly and a malformed operator refuses the whole cleanup of the case
            // and not only the statements of the audit. The predicates are built once and reused by the
            // delete and by the verification. The same form is used by the access class.
            var caseActorIds = users.Select(x => x.ToString()).ToArray();
            var auditScope = Identity.AuditEntries.Where(SqlAuditScope.Identity(run.OperatorSid, caseActorIds, users, organizations));
            var organizationAuditScope = Organization.AuditEntries.Where(SqlAuditScope.Organization(run.OperatorSid, caseActorIds, users, organizations));
            // The grants and the memberships are scoped with the same rule: only the non empty identifier
            // sets are composed, so that no statement is translated with an empty collection.
            IQueryable<GrantEntity> GrantsScope() => users.Length > 0 && organizations.Length > 0
                ? Identity.Grants.Where(x => users.Contains(x.UserId) || organizations.Contains(x.OrganizationId))
                : users.Length > 0
                    ? Identity.Grants.Where(x => users.Contains(x.UserId))
                    : Identity.Grants.Where(x => organizations.Contains(x.OrganizationId));
            IQueryable<MembershipEntity> MembershipsScope() => users.Length > 0 && organizations.Length > 0
                ? Organization.Memberships.Where(x => users.Contains(x.UserId) || organizations.Contains(x.OrganizationId))
                : users.Length > 0
                    ? Organization.Memberships.Where(x => users.Contains(x.UserId))
                    : Organization.Memberships.Where(x => organizations.Contains(x.OrganizationId));
            if (users.Length > 0) await SqlCleanup.IsolatedAsync(report, "identity.Sessions", t => Identity.Sessions.Where(x => users.Contains(x.UserId)).ExecuteDeleteAsync(t), token);
            if (users.Length > 0 || organizations.Length > 0) await SqlCleanup.IsolatedAsync(report, "identity.Grants", t => GrantsScope().ExecuteDeleteAsync(t), token);
            await SqlCleanup.IsolatedAsync(report, "identity.AuditEntries", t => auditScope.ExecuteDeleteAsync(t), token);
            await SqlCleanup.IsolatedAsync(report, "organizations.AuditEntries", t => organizationAuditScope.ExecuteDeleteAsync(t), token);
            if (users.Length > 0 || organizations.Length > 0) await SqlCleanup.IsolatedAsync(report, "organizations.OrganizationMemberships", t => MembershipsScope().ExecuteDeleteAsync(t), token);
            if (organizations.Length > 0) await SqlCleanup.IsolatedAsync(report, "organizations.Organizations", t => Organization.Organizations.Where(x => organizations.Contains(x.Id)).ExecuteDeleteAsync(t), token);
            if (users.Length > 0) await SqlCleanup.IsolatedAsync(report, "identity.Users", t => Identity.Users.Where(x => users.Contains(x.Id)).ExecuteDeleteAsync(t), token);
            if (users.Length > 0) await SqlCleanup.RecordResidualAsync(report, "identity.Sessions", Identity.Sessions.Where(x => users.Contains(x.UserId)), x => x.Id, token);
            if (users.Length > 0 || organizations.Length > 0) await SqlCleanup.RecordResidualAsync(report, "identity.Grants", GrantsScope(), x => x.Id, token);
            await SqlCleanup.RecordResidualAsync(report, "identity.AuditEntries", auditScope, x => x.Id, token);
            await SqlCleanup.RecordResidualAsync(report, "organizations.AuditEntries", organizationAuditScope, x => x.Id, token);
            if (users.Length > 0 || organizations.Length > 0) await SqlCleanup.RecordResidualAsync(report, "organizations.OrganizationMemberships", MembershipsScope(), x => x.Id, token);
            if (organizations.Length > 0) await SqlCleanup.RecordResidualAsync(report, "organizations.Organizations", Organization.Organizations.Where(x => organizations.Contains(x.Id)), x => x.Id, token);
            if (users.Length > 0) await SqlCleanup.RecordResidualAsync(report, "identity.Users", Identity.Users.Where(x => users.Contains(x.Id)), x => x.Id, token);
        }
        public async ValueTask DisposeAsync() { await Identity.DisposeAsync(); await Organization.DisposeAsync(); await Sql.DisposeAsync(); }
    }
}
