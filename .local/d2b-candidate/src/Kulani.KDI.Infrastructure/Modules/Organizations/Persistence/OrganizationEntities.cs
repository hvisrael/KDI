namespace Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;

public sealed class OrganizationEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public byte[] Version { get; set; } = [];
}
public sealed class MembershipEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] Version { get; set; } = [];
}
public sealed class OrganizationAuditEntity
{
    public Guid Id { get; set; }
    public string ActorKind { get; set; } = "Operator";
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public Guid TargetId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime AtUtc { get; set; }
    public string CorrelationId { get; set; } = "";
}
