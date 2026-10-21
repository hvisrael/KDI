namespace Kulani.KDI.Contracts.Access;

public sealed record OrganizationSummary(Guid Id, string Code, string Name);
public sealed record PermissionGrant(string Code, string ScopeType, Guid ScopeId);
public sealed record OrganizationAccess(Guid OrganizationId, IReadOnlyList<PermissionGrant> Permissions);
public sealed record SessionUser(Guid Id, string DisplayName);
public sealed record SessionResponse(SessionUser User, DateTime CreatedAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc);
public sealed record OrganizationList(IReadOnlyList<OrganizationSummary> Items);
public sealed record CsrfResponse(string RequestToken);
