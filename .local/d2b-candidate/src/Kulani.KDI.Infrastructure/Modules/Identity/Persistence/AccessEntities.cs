namespace Kulani.KDI.Infrastructure.Modules.Identity.Persistence;

public sealed class AccessUserEntity
{
    public Guid Id { get; set; }
    public string Issuer { get; set; } = "";
    public string Subject { get; set; } = "";
    public int IssuerBytes { get; private set; }
    public int SubjectBytes { get; private set; }
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class PermissionEntity { public string Code { get; set; } = ""; }
public sealed class GrantEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PermissionCode { get; set; } = "";
    public string ScopeType { get; set; } = "Organization";
    public Guid ScopeId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class SessionEntity
{
    public Guid Id { get; set; }
    public byte[] HandleHash { get; set; } = [];
    public Guid UserId { get; set; }
    public byte[]? ProtectedTicket { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime AbsoluteExpiresAtUtc { get; set; }
    public DateTime IdleExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
public sealed class IdentityAuditEntity
{
    public Guid Id { get; set; }
    public string ActorKind { get; set; } = "System";
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public Guid TargetId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime AtUtc { get; set; }
    public string CorrelationId { get; set; } = "";
}
