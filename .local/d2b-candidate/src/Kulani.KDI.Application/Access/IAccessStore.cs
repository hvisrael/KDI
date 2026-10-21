using Kulani.KDI.Contracts.Access;

namespace Kulani.KDI.Application.Access;

public sealed record AccessUser(Guid Id, string Issuer, string Subject, string DisplayName, bool IsActive);
public sealed record StoredSession(Guid Id, byte[] HandleHash, Guid UserId, byte[]? ProtectedTicket,
    DateTime CreatedAtUtc, DateTime LastSeenAtUtc, DateTime AbsoluteExpiresAtUtc,
    DateTime IdleExpiresAtUtc, DateTime? RevokedAtUtc, byte[] Version);

// Application orchestration port; no EF or module persistence types cross this boundary.
public interface IAccessStore
{
    Task<AccessUser?> FindUserAsync(string issuer, string subject, CancellationToken cancellationToken);
    Task<AccessUser?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccessUser> CreateUserAsync(string issuer, string subject, string displayName, DateTime nowUtc, string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrganizationSummary>> GetOrganizationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<OrganizationAccess?> GetOrganizationAccessAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken);
    Task CreateSessionAsync(StoredSession session, string correlationId, CancellationToken cancellationToken);
    Task<StoredSession?> GetSessionAsync(byte[] handleHash, CancellationToken cancellationToken);
    Task<bool> RenewSessionAsync(StoredSession observed, DateTime nowUtc, DateTime idleExpiresAtUtc, byte[] protectedTicket, CancellationToken cancellationToken);
    Task RevokeSessionAsync(byte[] handleHash, DateTime nowUtc, string correlationId, CancellationToken cancellationToken);
}
