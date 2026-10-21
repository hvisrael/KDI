using System.Security.Cryptography;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace Kulani.KDI.Infrastructure.Access;

// Privileged operator engine. Never registered in runtime dependency injection.
public static class AccessAdministration
{
    public static async Task ApplyWithinTransactionAsync(IdentityDbContext i, OrganizationsDbContext o, Request request, string operatorSid)
    {
        if (i.Database.CurrentTransaction is null || o.Database.CurrentTransaction is null || !ReferenceEquals(i.Database.GetDbConnection(), o.Database.GetDbConnection()) || !ReferenceEquals(i.Database.CurrentTransaction.GetDbTransaction(), o.Database.CurrentTransaction.GetDbTransaction())) throw new InvalidOperationException("Shared operator transaction required.");
        await using var command = i.Database.GetDbConnection().CreateCommand();
        command.Transaction = i.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = "SELECT APPLOCK_MODE('public','KDI:IdentityAdmin','Transaction');";
        if ((string?)await command.ExecuteScalarAsync() != "Exclusive") throw new InvalidOperationException("Exclusive administration lock required.");
    var r = request; Validate(r); var now = DateTime.UtcNow; var correlation = Guid.NewGuid().ToString("N");
    var user = r.UserId is { } userId ? await i.Users.SingleAsync(x => x.Id == userId) : null;
    switch (r.Command)
    {
        case "bootstrap":
            if (await o.Organizations.AnyAsync(x => x.Id == r.OrganizationId)) throw new InvalidOperationException("Organization already exists.");
            if (user is not { IsActive: true }) throw new InvalidOperationException("Bootstrap needs an active existing local identity.");
            o.Organizations.Add(new() { Id = r.OrganizationId!.Value, Code = r.Code!, Name = r.Name! });
            o.Memberships.Add(new() { Id = Guid.NewGuid(), OrganizationId = r.OrganizationId.Value, UserId = user.Id });
            await SetRole(i, user.Id, r.OrganizationId.Value, "Administrator", null, now); break;
        case "membership":
            if (user is null || !await o.Organizations.AnyAsync(x => x.Id == r.OrganizationId)) throw new InvalidOperationException("User and organization required.");
            var member = await o.Memberships.SingleOrDefaultAsync(x => x.OrganizationId == r.OrganizationId && x.UserId == user.Id);
            if (member is null) o.Memberships.Add(new() { Id = Guid.NewGuid(), OrganizationId = r.OrganizationId!.Value, UserId = user.Id, IsActive = r.Active!.Value }); else member.IsActive = r.Active!.Value;
            break;
        case "role":
            if (user is null || !await o.Memberships.AnyAsync(x => x.OrganizationId == r.OrganizationId && x.UserId == user.Id && x.IsActive)) throw new InvalidOperationException("Active membership required.");
            await SetRole(i, user.Id, r.OrganizationId!.Value, r.Role!, r.ProjectId, now); break;
        case "user-state":
            user!.IsActive = r.Active!.Value;
            if (!user.IsActive) await Revoke(i, user.Id, now);
            break;
        case "organization-state":
            (await o.Organizations.SingleAsync(x => x.Id == r.OrganizationId)).IsActive = r.Active!.Value; break;
        case "sessions-revoke": await Revoke(i, user!.Id, now); break;
        case "purge":
            await i.Sessions.Where(x => x.AbsoluteExpiresAtUtc <= now || x.IdleExpiresAtUtc <= now).ExecuteUpdateAsync(s => s.SetProperty(x => x.ProtectedTicket, (byte[]?)null));
            await i.Sessions.Where(x => (x.RevokedAtUtc != null && x.RevokedAtUtc < now.AddDays(-7)) || (x.AbsoluteExpiresAtUtc < now.AddDays(-7)) || (x.IdleExpiresAtUtc < now.AddDays(-7))).ExecuteDeleteAsync();
            await i.AuditEntries.Where(x => x.AtUtc < now.AddDays(-30)).ExecuteDeleteAsync();
            await o.AuditEntries.Where(x => x.AtUtc < now.AddDays(-30)).ExecuteDeleteAsync(); break;
    }
    i.AuditEntries.Add(new() { Id = Guid.NewGuid(), ActorKind = "Operator", ActorId = operatorSid, Action = "Admin." + r.Command, TargetId = r.UserId ?? r.OrganizationId ?? Guid.Empty, OrganizationId = r.OrganizationId, AtUtc = now, CorrelationId = correlation });
    if (r.OrganizationId is { } orgId) o.AuditEntries.Add(new() { Id = Guid.NewGuid(), ActorId = operatorSid, Action = "Admin." + r.Command, TargetId = r.UserId ?? orgId, OrganizationId = orgId, AtUtc = now, CorrelationId = correlation });
    await i.SaveChangesAsync(); await o.SaveChangesAsync();
    // Read after all writes, inside the exclusive transaction; every active organization retains an active administrator.
    foreach (var org in await o.Organizations.Where(x => x.IsActive).Select(x => x.Id).ToArrayAsync())
    {
        var members = await o.Memberships.Where(x => x.OrganizationId == org && x.IsActive).Select(x => x.UserId).ToArrayAsync();
        var valid = await i.Users.Where(u => u.IsActive && members.Contains(u.Id) && i.Grants.Any(g => g.UserId == u.Id && g.OrganizationId == org && g.ScopeType == "Organization" && g.ScopeId == org && g.PermissionCode == "members.manage" && g.RevokedAtUtc == null) && i.Grants.Any(g => g.UserId == u.Id && g.OrganizationId == org && g.ScopeType == "Organization" && g.ScopeId == org && g.PermissionCode == "access.manage" && g.RevokedAtUtc == null)).AnyAsync();
        if (!valid) throw new InvalidOperationException("Last active administrator invariant violated.");
    }
    }
public static void Validate(Request r)
{
    if (r.Command is not ("bootstrap" or "membership" or "role" or "user-state" or "organization-state" or "sessions-revoke" or "purge")) throw new ArgumentException("Unknown command.");
    if (r.Command is "bootstrap" or "membership" or "role" or "user-state" or "sessions-revoke" && r.UserId is null) throw new ArgumentException("User required.");
    if (r.Command is "bootstrap" or "membership" or "role" or "organization-state" && r.OrganizationId is null) throw new ArgumentException("Organization required.");
    if (r.Command is "membership" or "user-state" or "organization-state" && r.Active is null) throw new ArgumentException("State required.");
    if (r.Command == "bootstrap" && (r.Code is null || !System.Text.RegularExpressions.Regex.IsMatch(r.Code, "^[A-Z0-9_-]{1,32}$") || string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 200)) throw new ArgumentException("Invalid organization.");
    if (r.Command == "role" && (r.Role is not ("Administrator" or "Manager" or "Reader" or "Auditor" or "RestrictedReader") || (r.Role == "RestrictedReader" && r.ProjectId is null))) throw new ArgumentException("Invalid fixed role.");
}
public static async Task<string[]> Versions(IdentityDbContext i, OrganizationsDbContext o)
{
    var list = new List<string>();
    foreach (var x in await i.Users.AsNoTracking().ToArrayAsync()) list.Add("u:" + x.Id + ":" + Convert.ToHexString(x.Version));
    foreach (var x in await i.Grants.AsNoTracking().ToArrayAsync()) list.Add("g:" + x.Id + ":" + Convert.ToHexString(x.Version));
    foreach (var x in await o.Organizations.AsNoTracking().ToArrayAsync()) list.Add("o:" + x.Id + ":" + Convert.ToHexString(x.Version));
    foreach (var x in await o.Memberships.AsNoTracking().ToArrayAsync()) list.Add("m:" + x.Id + ":" + Convert.ToHexString(x.Version));
    return list.Order(StringComparer.Ordinal).ToArray();
}
static async Task Revoke(IdentityDbContext i, Guid user, DateTime now) => await i.Sessions.Where(x => x.UserId == user && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now).SetProperty(x => x.ProtectedTicket, (byte[]?)null));
static async Task SetRole(IdentityDbContext i, Guid user, Guid org, string role, Guid? project, DateTime now)
{
    await i.Grants.Where(x => x.UserId == user && x.OrganizationId == org && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now));
    string[] permissions = role switch { "Administrator" => IdentityDbContext.Permissions, "Manager" => ["clients.read", "clients.manage", "projects.read", "projects.manage"], "Reader" => ["clients.read", "projects.read"], "Auditor" => ["audit.read"], "RestrictedReader" => ["projects.read"], _ => throw new ArgumentException("Unknown role.") };
    foreach (var p in permissions) i.Grants.Add(new() { Id = Guid.NewGuid(), UserId = user, OrganizationId = org, PermissionCode = p, ScopeType = role == "RestrictedReader" ? "Project" : "Organization", ScopeId = role == "RestrictedReader" ? project!.Value : org });
}
}
public sealed record Request(string Command, Guid? UserId = null, Guid? OrganizationId = null, string? Code = null, string? Name = null, bool? Active = null, string? Role = null, Guid? ProjectId = null);
public sealed record Plan(string Database, Request Request, DateTime CreatedAtUtc, string[] Versions, string OperatorSid);
