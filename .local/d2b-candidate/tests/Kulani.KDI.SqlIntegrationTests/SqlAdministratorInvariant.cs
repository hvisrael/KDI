using System.Text.Json.Serialization;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.SqlIntegrationTests;

// One active organization that does not satisfy the invariant of the administrator, as it was when the case
// started. Only these organizations are kept: they are the ones that make a command fail, and they are the
// only ones that the diagnostic needs. The origin is explicit, so that the operator can tell apart an
// organization that a case of this run left behind from one that was already in the database.
internal sealed record ActiveOrganizationState
{
    public Guid Id { get; init; }
    // "run-fixture" when the organization was created by a case of this run, "preexisting-foreign" when it was
    // already in the database before the case started.
    public string Origin { get; init; } = "preexisting-foreign";
    public string? RegisteredBy { get; init; }
}

// Photo of the global administrator invariant taken before the mutations of a case. It separates a
// violation that already existed in the database from a violation introduced by this run. The artifact only
// carries the violating organizations and the number of active organizations that were inspected, so that no
// identifier of a foreign organization is written without need.
internal sealed record InvariantSnapshot
{
    public string Case { get; init; } = "";
    public DateTime CapturedUtc { get; init; }
    public int ActiveOrganizationCount { get; init; }
    public IReadOnlyList<ActiveOrganizationState> WithoutAdministrator { get; init; } = [];
    [JsonIgnore] public IReadOnlyList<ActiveOrganizationState> FixturesWithoutAdministrator => WithoutAdministrator.Where(x => x.Origin == "run-fixture").ToList();
    [JsonIgnore] public IReadOnlyList<ActiveOrganizationState> ForeignWithoutAdministrator => WithoutAdministrator.Where(x => x.Origin != "run-fixture").ToList();
}

internal static class SqlAdministratorInvariant
{
    // Mirrors the invariant of AccessAdministration.ApplyWithinTransactionAsync: every active organization
    // needs one active member that holds the organization scoped members.manage and access.manage grants
    // and no revoked grant. The check is read only and runs before the case mutates anything.
    //
    // The queries do not track the entities: the same context is the one that creates the users of the case,
    // and a tracked copy of a foreign user would be an obsolete copy in its change tracker.
    public static async Task<InvariantSnapshot> CaptureAsync(IdentityDbContext identity, OrganizationsDbContext organizations, SqlTestRun run, string caseName, CancellationToken token)
    {
        var active = await organizations.Organizations.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToArrayAsync(token);
        var violations = new List<ActiveOrganizationState>();
        foreach (var id in active)
        {
            var members = await organizations.Memberships.AsNoTracking().Where(x => x.OrganizationId == id && x.IsActive).Select(x => x.UserId).ToArrayAsync(token);
            var valid = await identity.Users.AsNoTracking().Where(u => u.IsActive && members.Contains(u.Id)
                && identity.Grants.Any(g => g.UserId == u.Id && g.OrganizationId == id && g.ScopeType == "Organization" && g.ScopeId == id && g.PermissionCode == "members.manage" && g.RevokedAtUtc == null)
                && identity.Grants.Any(g => g.UserId == u.Id && g.OrganizationId == id && g.ScopeType == "Organization" && g.ScopeId == id && g.PermissionCode == "access.manage" && g.RevokedAtUtc == null)).AnyAsync(token);
            if (!valid)
            {
                var owner = run.OwnerOfOrganization(id);
                violations.Add(new ActiveOrganizationState { Id = id, Origin = owner is null ? "preexisting-foreign" : "run-fixture", RegisteredBy = owner });
            }
        }
        return new InvariantSnapshot { Case = caseName, CapturedUtc = DateTime.UtcNow, ActiveOrganizationCount = active.Length, WithoutAdministrator = violations };
    }
}
