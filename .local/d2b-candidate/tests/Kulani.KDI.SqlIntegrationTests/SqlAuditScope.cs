using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;

namespace Kulani.KDI.SqlIntegrationTests;

// Scope of the audit rows that belong to one case of the harness. The three ways of recognizing a row are a
// UNION, never an intersection: a row belongs to the case when ANY of them matches.
//
//   identity.AuditEntries     : ActorId is a user of the case,
//                               or TargetId is a user or the session of the case,
//                               or OrganizationId is an organization of the case.
//   organizations.AuditEntries: the same three ways, with a non nullable OrganizationId.
//
// The union is what makes the rows of the commands without an organization reachable. The audit of
// "user-state" and of "sessions-revoke" has OrganizationId null, and the audit of "Session.Created" has the
// session as target, so an intersection of the three ways would leave exactly the rows that no cleanup
// could reach.
//
// The scope is keyed by the identifiers of the case, never by the operator of the run. The operator of the
// run is shared by every case of the run, so a delete that used it as a key would let a case remove the
// audit rows of another case. The containment of the delete does NOT depend on the guard below: the operator
// is not part of the predicate, so it never reaches a delete statement, whatever its value is.
//
// The coverage of F1 does not depend on the operator either: the rows that the current commands write carry
// the user of the case as ActorId (SqlAccessStore: User.Created, Session.Created, Session.Revoked) or as
// TargetId (AccessAdministration: Admin.* with TargetId = r.UserId), so the rows of the commands without an
// organization, "user-state" and "sessions-revoke", are inside the union through the TargetId term. The
// operator of the run is still written to the audit rows, so the operator can find them by exact actor.
//
// The identifiers that are empty are excluded from the union, so that no statement is ever translated with
// an empty collection. Whether EF Core drops the empty terms or keeps them as a false predicate depends on
// the provider and on the version; the harness does not rely on it, because a set is empty only when the
// case failed before creating its fixtures, and in that case the scope matches nothing.
internal static class SqlAuditScope
{
    // The form that the operator of the run must have. The guard checks the FORM, not the uniqueness: a
    // fixed operator with a valid form would be accepted. The uniqueness is not a property of the form but
    // of the origin of the value, SqlTestRun.OperatorSid, which is derived from a new Guid per process and
    // is not configurable from the environment.
    private static readonly Regex RunActorForm = new(@"^KDI-D2B-RUN-[0-9a-f]{32}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Expression<Func<IdentityAuditEntity, bool>> Identity(string runActor, string[] caseActorIds, Guid[] targetIds, Guid[] organizationIds)
    {
        Guard(runActor);
        var byActor = caseActorIds.Length > 0;
        var byTarget = targetIds.Length > 0;
        var byOrganization = organizationIds.Length > 0;
        return x => (byActor && caseActorIds.Contains(x.ActorId))
            || (byTarget && targetIds.Contains(x.TargetId))
            || (byOrganization && x.OrganizationId != null && organizationIds.Contains(x.OrganizationId.Value));
    }

    public static Expression<Func<OrganizationAuditEntity, bool>> Organization(string runActor, string[] caseActorIds, Guid[] targetIds, Guid[] organizationIds)
    {
        Guard(runActor);
        var byActor = caseActorIds.Length > 0;
        var byTarget = targetIds.Length > 0;
        var byOrganization = organizationIds.Length > 0;
        return x => (byActor && caseActorIds.Contains(x.ActorId))
            || (byTarget && targetIds.Contains(x.TargetId))
            || (byOrganization && organizationIds.Contains(x.OrganizationId));
    }

    // The guard throws, it does not warn, and it is the only place where the operator of the run is accepted.
    // What it guarantees is the FORM of the operator: a malformed operator refuses to build the scope, and
    // with it the whole cleanup of the case. What it does NOT guarantee is the uniqueness of the operator: a
    // fixed operator with a valid form is accepted, because the uniqueness comes from the origin of the
    // value and not from its form. The containment of the delete does not rest on this guard, it rests on
    // the operator not being part of the predicate.
    private static void Guard(string runActor)
    {
        if (string.IsNullOrEmpty(runActor) || !RunActorForm.IsMatch(runActor))
            throw new SqlScopeRefusal("BLOCKED: the operator of the run does not have the run scoped form KDI-D2B-RUN-<32 hex>. The audit scope of the case is refused and the cleanup of the case is not executed. Operator: '" + runActor + "'.");
    }
}

// Refusal of the harness to build the delete scope of a case. It carries its own message because it is the
// reason why the cleanup of a case is not executed, and that reason has to reach the operator instead of
// being reduced to the type of the exception.
internal sealed class SqlScopeRefusal : InvalidOperationException
{
    public SqlScopeRefusal(string message) : base(message) { }
}
