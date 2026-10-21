using Kulani.KDI.Application.Access;
using Kulani.KDI.Contracts.Access;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kulani.KDI.Infrastructure.Access;

public static class AccessStorageRegistration
{
    public static IServiceCollection AddKdiAccessStorage(this IServiceCollection services) => services.AddScoped<IAccessStore, SqlAccessStore>();
}

public sealed class SqlAccessStore(IdentityDbContext identity, OrganizationsDbContext organizations) : IAccessStore
{
    private static AccessUser Map(AccessUserEntity x) => new(x.Id, x.Issuer, x.Subject, x.DisplayName, x.IsActive);
    public async Task<AccessUser?> FindUserAsync(string issuer, string subject, CancellationToken cancellationToken)
    {
        var issuerBytes = checked(issuer.Length * 2); var subjectBytes = checked(subject.Length * 2);
        var user = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Issuer == issuer && x.Subject == subject && x.IssuerBytes == issuerBytes && x.SubjectBytes == subjectBytes, cancellationToken);
        return user is null ? null : Map(user);
    }
    public async Task<AccessUser?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    { var user = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken); return user is null ? null : Map(user); }
    public async Task<AccessUser> CreateUserAsync(string issuer, string subject, string displayName, DateTime nowUtc, string correlationId, CancellationToken cancellationToken)
    {
        if (issuer.Length is 0 or > 512 || subject.Length is 0 or > 255 || displayName.Length > 200 || nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Invalid identity input.");
        var existing = await FindUserAsync(issuer, subject, cancellationToken); if (existing is not null) return existing;
        var user = new AccessUserEntity { Id = Guid.NewGuid(), Issuer = issuer, Subject = subject, DisplayName = displayName, CreatedAtUtc = nowUtc };
        await using var tx = await identity.Database.BeginTransactionAsync(cancellationToken);
        identity.Users.Add(user); identity.AuditEntries.Add(Audit("User.Created", user.Id, user.Id, nowUtc, correlationId));
        try { await identity.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); return Map(user); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        { await tx.RollbackAsync(cancellationToken); identity.ChangeTracker.Clear(); return await FindUserAsync(issuer, subject, cancellationToken) ?? throw new InvalidOperationException("Concurrent identity could not be resolved."); }
    }
    public async Task<IReadOnlyList<OrganizationSummary>> GetOrganizationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await identity.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken)) return [];
        return await (from o in organizations.Organizations.AsNoTracking() join m in organizations.Memberships.AsNoTracking() on o.Id equals m.OrganizationId where o.IsActive && m.IsActive && m.UserId == userId orderby o.Code select new OrganizationSummary(o.Id, o.Code, o.Name)).ToArrayAsync(cancellationToken);
    }
    public async Task<OrganizationAccess?> GetOrganizationAccessAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken)
    {
        if (!await identity.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken) || !await (from o in organizations.Organizations join m in organizations.Memberships on o.Id equals m.OrganizationId where o.Id == organizationId && o.IsActive && m.UserId == userId && m.IsActive select m.Id).AnyAsync(cancellationToken)) return null;
        var grants = await identity.Grants.AsNoTracking().Where(x => x.UserId == userId && x.OrganizationId == organizationId && x.RevokedAtUtc == null).Select(x => new PermissionGrant(x.PermissionCode, x.ScopeType, x.ScopeId)).ToArrayAsync(cancellationToken);
        return new(organizationId, grants);
    }
    private static StoredSession Map(SessionEntity x) => new(x.Id, x.HandleHash, x.UserId, x.ProtectedTicket, x.CreatedAtUtc, x.LastSeenAtUtc, x.AbsoluteExpiresAtUtc, x.IdleExpiresAtUtc, x.RevokedAtUtc, x.Version);
    public async Task CreateSessionAsync(StoredSession s, string correlationId, CancellationToken cancellationToken)
    {
        if (s.HandleHash.Length != 32 || s.ProtectedTicket is null || s.AbsoluteExpiresAtUtc > s.CreatedAtUtc.AddHours(1) || s.IdleExpiresAtUtc > s.LastSeenAtUtc.AddMinutes(30) || s.RevokedAtUtc is not null) throw new ArgumentException("Invalid session.");
        await using var tx = await identity.Database.BeginTransactionAsync(cancellationToken);
        if (!await identity.Users.AnyAsync(x => x.Id == s.UserId && x.IsActive, cancellationToken)) throw new InvalidOperationException("Inactive user.");
        identity.Sessions.Add(new SessionEntity { Id = s.Id, HandleHash = s.HandleHash, UserId = s.UserId, ProtectedTicket = s.ProtectedTicket, CreatedAtUtc = s.CreatedAtUtc, LastSeenAtUtc = s.LastSeenAtUtc, AbsoluteExpiresAtUtc = s.AbsoluteExpiresAtUtc, IdleExpiresAtUtc = s.IdleExpiresAtUtc });
        identity.AuditEntries.Add(Audit("Session.Created", s.Id, s.UserId, s.CreatedAtUtc, correlationId)); await identity.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
    }
    public async Task<StoredSession?> GetSessionAsync(byte[] handleHash, CancellationToken cancellationToken)
    { var s = await identity.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.HandleHash == handleHash, cancellationToken); return s is null ? null : Map(s); }
    public async Task<bool> RenewSessionAsync(StoredSession observed, DateTime nowUtc, DateTime idleExpiresAtUtc, byte[] protectedTicket, CancellationToken cancellationToken)
    {
        if (nowUtc.Kind != DateTimeKind.Utc || nowUtc < observed.LastSeenAtUtc || idleExpiresAtUtc > nowUtc.AddMinutes(30) || idleExpiresAtUtc > observed.AbsoluteExpiresAtUtc || idleExpiresAtUtc < nowUtc) return false;
        return await identity.Sessions.Where(x => x.Id == observed.Id && x.Version == observed.Version && x.RevokedAtUtc == null && x.AbsoluteExpiresAtUtc > nowUtc && x.IdleExpiresAtUtc > nowUtc && x.LastSeenAtUtc <= nowUtc && identity.Users.Any(u => u.Id == x.UserId && u.IsActive)).ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAtUtc, nowUtc).SetProperty(x => x.IdleExpiresAtUtc, idleExpiresAtUtc).SetProperty(x => x.ProtectedTicket, protectedTicket), cancellationToken) == 1;
    }
    public async Task RevokeSessionAsync(byte[] handleHash, DateTime nowUtc, string correlationId, CancellationToken cancellationToken)
    {
        await using var tx = await identity.Database.BeginTransactionAsync(cancellationToken);
        var s = await identity.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.HandleHash == handleHash, cancellationToken);
        if (s is not null && await identity.Sessions.Where(x => x.Id == s.Id && x.RevokedAtUtc == null).ExecuteUpdateAsync(v => v.SetProperty(x => x.RevokedAtUtc, nowUtc).SetProperty(x => x.ProtectedTicket, (byte[]?)null), cancellationToken) == 1)
        { identity.AuditEntries.Add(Audit("Session.Revoked", s.Id, s.UserId, nowUtc, correlationId)); await identity.SaveChangesAsync(cancellationToken); }
        await tx.CommitAsync(cancellationToken);
    }
    private static IdentityAuditEntity Audit(string action, Guid target, Guid actor, DateTime at, string correlation) => new() { Id = Guid.NewGuid(), Action = action, TargetId = target, ActorKind = "User", ActorId = actor.ToString(), AtUtc = at, CorrelationId = correlation.Length <= 64 ? correlation : throw new ArgumentException("Correlation too long.") };
}
