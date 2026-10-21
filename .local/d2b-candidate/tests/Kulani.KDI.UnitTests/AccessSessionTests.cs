using System.Security.Claims;
using Kulani.KDI.Api.Auth;
using Kulani.KDI.Application.Access;
using Kulani.KDI.Contracts.Access;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Kulani.KDI.UnitTests;

public sealed class AccessSessionTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Store : IAccessStore
    {
        public AccessUser User = new(Guid.NewGuid(), "https://issuer/", "subject", "User", true);
        public StoredSession? Session;
        public OrganizationAccess? Access;
        public bool RevokeDuringRenew;
        public int RenewAttempts;
        public int FailCas;
        public Task<AccessUser?> FindUserAsync(string issuer, string subject, CancellationToken ct) => Task.FromResult<AccessUser?>(User);
        public Task<AccessUser?> FindUserByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<AccessUser?>(User.Id == id ? User : null);
        public Task<AccessUser> CreateUserAsync(string issuer, string subject, string name, DateTime now, string correlation, CancellationToken ct) => Task.FromResult(User);
        public Task<IReadOnlyList<OrganizationSummary>> GetOrganizationsAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<OrganizationSummary>>([]);
        public Task<OrganizationAccess?> GetOrganizationAccessAsync(Guid user, Guid org, CancellationToken ct) => Task.FromResult(Access);
        public Task CreateSessionAsync(StoredSession session, string correlation, CancellationToken ct) { Session = session with { Version = [1] }; return Task.CompletedTask; }
        public Task<StoredSession?> GetSessionAsync(byte[] hash, CancellationToken ct) => Task.FromResult(Session?.HandleHash.SequenceEqual(hash) == true ? Session : null);
        public Task<bool> RenewSessionAsync(StoredSession observed, DateTime now, DateTime expiry, byte[] ticket, CancellationToken ct)
        {
            RenewAttempts++;
            if (RevokeDuringRenew) { Session = Session! with { RevokedAtUtc = now, ProtectedTicket = null }; return Task.FromResult(false); }
            if (FailCas-- > 0) return Task.FromResult(false);
            if (Session?.RevokedAtUtc is not null || Session is null) return Task.FromResult(false);
            Session = Session with { LastSeenAtUtc = now, IdleExpiresAtUtc = expiry, ProtectedTicket = ticket, Version = [(byte)(Session.Version[0] + 1)] };
            return Task.FromResult(true);
        }
        public Task RevokeSessionAsync(byte[] hash, DateTime now, string correlation, CancellationToken ct)
        { Session = Session! with { RevokedAtUtc = now, ProtectedTicket = null }; return Task.CompletedTask; }
    }
    private static AuthenticationTicket Ticket(Store store) => new(new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(SqlTicketStore.UserClaim, store.User.Id.ToString())], "test")), new AuthenticationProperties(), AccessRegistration.CookieScheme);
    [Fact]
    public async Task Session_is_opaque_hashed_encrypted_and_bounded()
    {
        var store = new Store(); var clock = new Clock(); var sessions = new SqlTicketStore(store, new EphemeralDataProtectionProvider(), clock);
        var handle = await sessions.StoreAsync(Ticket(store));
        Assert.Equal(64, handle.Length); Assert.Equal(32, store.Session!.HandleHash.Length);
        Assert.DoesNotContain(store.User.Id.ToString(), System.Text.Encoding.UTF8.GetString(store.Session.ProtectedTicket!));
        Assert.Equal(clock.Now.UtcDateTime.AddMinutes(30), store.Session.IdleExpiresAtUtc);
        Assert.Equal(clock.Now.UtcDateTime.AddHours(1), store.Session.AbsoluteExpiresAtUtc);
        Assert.NotNull(await sessions.RetrieveAsync(handle));
        Assert.Null(await sessions.RetrieveAsync(new string('0', 64)));
    }
    [Fact]
    public async Task Activity_slides_idle_but_never_absolute_limit()
    {
        var store = new Store(); var clock = new Clock(); var sessions = new SqlTicketStore(store, new EphemeralDataProtectionProvider(), clock);
        var key = await sessions.StoreAsync(Ticket(store)); var absolute = store.Session!.AbsoluteExpiresAtUtc;
        clock.Now = clock.Now.AddMinutes(20); Assert.NotNull(await sessions.RetrieveAsync(key));
        Assert.Equal(clock.Now.UtcDateTime.AddMinutes(30), store.Session.IdleExpiresAtUtc);
        clock.Now = clock.Now.AddMinutes(20); Assert.NotNull(await sessions.RetrieveAsync(key));
        Assert.Equal(absolute, store.Session.IdleExpiresAtUtc);
        clock.Now = clock.Now.AddMinutes(20); Assert.Null(await sessions.RetrieveAsync(key));
    }
    [Fact]
    public async Task Idle_expiration_is_exclusive_at_boundary()
    {
        var store = new Store(); var clock = new Clock(); var sessions = new SqlTicketStore(store, new EphemeralDataProtectionProvider(), clock);
        var key = await sessions.StoreAsync(Ticket(store)); clock.Now = clock.Now.AddMinutes(30);
        Assert.Null(await sessions.RetrieveAsync(key)); Assert.Equal(0, store.RenewAttempts);
    }
    [Fact]
    public async Task Disabled_user_and_revoked_session_fail_closed()
    {
        var store = new Store(); var sessions = new SqlTicketStore(store, new EphemeralDataProtectionProvider(), new Clock());
        var key = await sessions.StoreAsync(Ticket(store)); store.User = store.User with { IsActive = false };
        Assert.Null(await sessions.RetrieveAsync(key)); store.User = store.User with { IsActive = true };
        await sessions.RemoveAsync(key); Assert.Null(await sessions.RetrieveAsync(key)); Assert.Null(store.Session!.ProtectedTicket);
    }
    [Fact]
    public async Task Revocation_racing_renewal_cannot_resurrect_session()
    {
        var store = new Store(); var sessions = new SqlTicketStore(store, new EphemeralDataProtectionProvider(), new Clock());
        var key = await sessions.StoreAsync(Ticket(store)); store.RevokeDuringRenew = true;
        Assert.Null(await sessions.RetrieveAsync(key)); Assert.Null(store.Session!.ProtectedTicket);
        Assert.Equal(1, store.RenewAttempts);
    }
    [Fact]
    public async Task Cas_conflict_retries_once_only()
    {
        var store = new Store(); var sessions = new SqlTicketStore(store, new EphemeralDataProtectionProvider(), new Clock());
        var key = await sessions.StoreAsync(Ticket(store)); store.FailCas = 10;
        await sessions.RenewAsync(key, Ticket(store)); Assert.Equal(2, store.RenewAttempts);
    }
    [Theory]
    [InlineData("//evil.example")][InlineData("https://evil.example")][InlineData("/\\evil")][InlineData("/%2f%2fevil")][InlineData("/\r\nevil")]
    public void Return_url_rejects_external_ambiguous_or_control_input(string target) => Assert.False(AccessEndpoints.IsLocalReturnUrl(target));
    [Theory][InlineData("/")][InlineData("/workspace?tab=1")]
    public void Return_url_accepts_local_path(string target) => Assert.True(AccessEndpoints.IsLocalReturnUrl(target));

    [Fact]
    public async Task Permission_policy_requires_membership_and_denies_cross_org_and_unknown_codes()
    {
        var store = new Store(); var policy = new AccessPermissionPolicy(store); var org = Guid.NewGuid(); var other = Guid.NewGuid();
        Assert.Equal(AccessDecision.Unauthenticated, await policy.EvaluateAsync(null, org, "projects.read", "Organization", org, CancellationToken.None));
        Assert.Equal(AccessDecision.OrganizationNotFound, await policy.EvaluateAsync(store.User.Id, org, "projects.read", "Organization", org, CancellationToken.None));
        store.Access = new(org, [new("projects.read", "Organization", org)]);
        Assert.Equal(AccessDecision.Allowed, await policy.EvaluateAsync(store.User.Id, org, "projects.read", "Organization", org, CancellationToken.None));
        Assert.Equal(AccessDecision.Forbidden, await policy.EvaluateAsync(store.User.Id, org, "projects.read", "Organization", other, CancellationToken.None));
        Assert.Equal(AccessDecision.Forbidden, await policy.EvaluateAsync(store.User.Id, org, "projects.delete", "Organization", org, CancellationToken.None));
        Assert.Equal(AccessDecision.Forbidden, await policy.EvaluateAsync(store.User.Id, org, "access.manage", "Organization", org, CancellationToken.None));
    }
    [Fact]
    public async Task Project_grant_does_not_cover_other_projects_or_organization_management()
    {
        var store = new Store(); var org = Guid.NewGuid(); var project = Guid.NewGuid();
        store.Access = new(org, [new("projects.read", "Project", project)]); var policy = new AccessPermissionPolicy(store);
        Assert.Equal(AccessDecision.Allowed, await policy.EvaluateAsync(store.User.Id, org, "projects.read", "Project", project, CancellationToken.None));
        Assert.Equal(AccessDecision.Forbidden, await policy.EvaluateAsync(store.User.Id, org, "projects.read", "Project", Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(AccessDecision.Forbidden, await policy.EvaluateAsync(store.User.Id, org, "projects.read", "Organization", org, CancellationToken.None));
    }

    [Fact]
    public async Task Valid_ciphertext_for_other_user_cannot_be_substituted_into_session()
    {
        var provider = new EphemeralDataProtectionProvider(); var store = new Store(); var sessions = new SqlTicketStore(store, provider, new Clock());
        var key = await sessions.StoreAsync(Ticket(store)); var original = store.Session!;
        var other = new Store(); var otherTickets = new SqlTicketStore(other, provider, new Clock());
        await otherTickets.StoreAsync(Ticket(other)); store.Session = original with { ProtectedTicket = other.Session!.ProtectedTicket };
        Assert.Null(await sessions.RetrieveAsync(key)); Assert.Equal(0, store.RenewAttempts);
    }
    [Fact]
    public async Task Raw_handle_never_enters_protected_ticket_and_cas_exhaustion_fails_closed()
    {
        var provider = new EphemeralDataProtectionProvider(); var store = new Store(); var sessions = new SqlTicketStore(store, provider, new Clock());
        var key = await sessions.StoreAsync(Ticket(store)); var ticket = await sessions.RetrieveAsync(key);
        Assert.Equal(key, ticket!.Properties.Items[SqlTicketStore.SessionHandle]);
        await sessions.RenewAsync(key, ticket);
        var bytes = provider.CreateProtector("KDI.Identity.Session.v1").Unprotect(store.Session!.ProtectedTicket!);
        var storedTicket = TicketSerializer.Default.Deserialize(bytes)!;
        Assert.False(storedTicket.Properties.Items.ContainsKey(SqlTicketStore.SessionHandle));
        store.RenewAttempts = 0; store.FailCas = 10;
        Assert.Null(await sessions.RetrieveAsync(key)); Assert.Equal(2, store.RenewAttempts);
    }
}
