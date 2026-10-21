namespace Kulani.KDI.Application.Access;

public enum AccessDecision { Allowed, Unauthenticated, OrganizationNotFound, Forbidden }

// Membership is mandatory even with a grant. Organization-wide grants cover a project within that organization.
// The caller must resolve the project's ownership before passing a project scope; no Portfolio dependency exists here.
public sealed class AccessPermissionPolicy(IAccessStore store)
{
    private static readonly HashSet<string> Catalog = new(StringComparer.Ordinal)
    { "members.read", "members.manage", "access.manage", "audit.read", "clients.read", "clients.manage", "projects.read", "projects.manage" };
    public async Task<AccessDecision> EvaluateAsync(Guid? userId, Guid organizationId, string permission,
        string scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        if (userId is null || (await store.FindUserByIdAsync(userId.Value, cancellationToken))?.IsActive != true)
            return AccessDecision.Unauthenticated;
        var access = await store.GetOrganizationAccessAsync(userId.Value, organizationId, cancellationToken);
        if (access is null || access.OrganizationId != organizationId) return AccessDecision.OrganizationNotFound;
        if (!Catalog.Contains(permission) || scopeType is not ("Organization" or "Project") || scopeId == Guid.Empty
            || scopeType == "Organization" && scopeId != organizationId) return AccessDecision.Forbidden;
        return access.Permissions.Any(p => p.Code == permission &&
            (p.ScopeType == "Organization" && p.ScopeId == organizationId ||
             scopeType == "Project" && p.ScopeType == "Project" && p.ScopeId == scopeId))
            ? AccessDecision.Allowed : AccessDecision.Forbidden;
    }
}
