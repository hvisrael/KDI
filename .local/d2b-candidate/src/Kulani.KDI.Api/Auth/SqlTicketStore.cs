using System.Security.Claims;
using System.Security.Cryptography;
using Kulani.KDI.Application.Access;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Kulani.KDI.Api.Auth;

public sealed class SqlTicketStore(IAccessStore store, IDataProtectionProvider protection, TimeProvider clock) : ITicketStore
{
    private readonly IDataProtector protector = protection.CreateProtector("KDI.Identity.Session.v1");
    public const string UserClaim = "kdi:user";
    public const string SessionHandle = "kdi:session-handle";
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var handle = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var userId = Guid.Parse(ticket.Principal.FindFirstValue(UserClaim)!);
        ticket.Properties.IssuedUtc = now;
        ticket.Properties.ExpiresUtc = now.AddMinutes(30);
        await store.CreateSessionAsync(new(Guid.NewGuid(), Hash(handle), userId, Protect(ticket), now, now,
            now.AddHours(1), now.AddMinutes(30), null, []), "session-create", CancellationToken.None);
        return handle;
    }
    public async Task RenewAsync(string key, AuthenticationTicket ticket) => await TryRenewAsync(key, ticket);
    private async Task<bool> TryRenewAsync(string key, AuthenticationTicket ticket)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var observed = await ValidSession(key);
            if (observed is null || !MatchesUser(ticket, observed.UserId)) return false;
            var now = clock.GetUtcNow().UtcDateTime;
            var expiry = Min(now.AddMinutes(30), observed.AbsoluteExpiresAtUtc);
            ticket.Properties.IssuedUtc = now;
            ticket.Properties.ExpiresUtc = expiry;
            if (await store.RenewSessionAsync(observed, now, expiry, Protect(ticket), CancellationToken.None)) return true;
        }
        return false;
    }
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var session = await ValidSession(key);
        if (session?.ProtectedTicket is null) return null;
        AuthenticationTicket? ticket;
        try { ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(session.ProtectedTicket)); }
        catch (CryptographicException) { return null; }
        if (ticket is null || !MatchesUser(ticket, session.UserId)) return null;
        if (!await TryRenewAsync(key, ticket)) return null;
        session = await ValidSession(key);
        if (session is null) return null;
        ticket.Properties.Items[SessionHandle] = key;
        ticket.Properties.IssuedUtc = session.LastSeenAtUtc;
        ticket.Properties.ExpiresUtc = Min(session.IdleExpiresAtUtc, session.AbsoluteExpiresAtUtc);
        return ticket;
    }
    public Task RemoveAsync(string key) => store.RevokeSessionAsync(Hash(key), clock.GetUtcNow().UtcDateTime, "session-revoke", CancellationToken.None);
    public Task<StoredSession?> GetCurrentAsync(string key) => ValidSession(key);
    private async Task<StoredSession?> ValidSession(string key)
    {
        if (key.Length != 64 || !key.All(Uri.IsHexDigit)) return null;
        var session = await store.GetSessionAsync(Hash(key), CancellationToken.None);
        var now = clock.GetUtcNow().UtcDateTime;
        if (session is null || session.RevokedAtUtc is not null || session.ProtectedTicket is null ||
            now >= session.AbsoluteExpiresAtUtc || now >= session.IdleExpiresAtUtc) return null;
        var user = await store.FindUserByIdAsync(session.UserId, CancellationToken.None);
        return user?.IsActive == true ? session : null;
    }
    private byte[] Protect(AuthenticationTicket ticket)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>(ticket.Properties.Items));
        properties.Items.Remove(SessionHandle);
        return protector.Protect(TicketSerializer.Default.Serialize(new AuthenticationTicket(ticket.Principal, properties, ticket.AuthenticationScheme)));
    }
    private static bool MatchesUser(AuthenticationTicket ticket, Guid userId) => ticket.AuthenticationScheme == AccessRegistration.CookieScheme
        && ticket.Principal.Identity?.IsAuthenticated == true
        && Guid.TryParse(ticket.Principal.FindFirstValue(UserClaim), out var ticketUserId) && ticketUserId == userId;
    private static byte[] Hash(string key) => SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(key));
    private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
}
