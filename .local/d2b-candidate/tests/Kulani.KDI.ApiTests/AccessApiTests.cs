using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Kulani.KDI.Api.Auth;
using Kulani.KDI.Application.Access;
using Kulani.KDI.Contracts.Access;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kulani.KDI.ApiTests;

public sealed class AccessApiTests
{
    private sealed class Store : IAccessStore
    {
        public AccessUser User = new(Guid.NewGuid(), "https://issuer/", "subject", "Test", true);
        public StoredSession? Session;
        public bool ThrowSql;
        public bool UserExists = true;
        public int CreatedUsers;
        public (string Issuer, string Subject)? IdentityObserved;
        public Task<AccessUser?> FindUserAsync(string i, string s, CancellationToken ct) { IdentityObserved = (i, s); return Task.FromResult(UserExists ? User : null); }
        public Task<AccessUser?> FindUserByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<AccessUser?>(User);
        public Task<AccessUser> CreateUserAsync(string i, string s, string name, DateTime now, string correlation, CancellationToken ct) { CreatedUsers++; return Task.FromResult(User); }
        public Task<IReadOnlyList<OrganizationSummary>> GetOrganizationsAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<OrganizationSummary>>([]);
        public Task<OrganizationAccess?> GetOrganizationAccessAsync(Guid user, Guid org, CancellationToken ct) => Task.FromResult<OrganizationAccess?>(null);
        public Task CreateSessionAsync(StoredSession s, string correlation, CancellationToken ct) { Session = s with { Version = [1] }; return Task.CompletedTask; }
        public Task<StoredSession?> GetSessionAsync(byte[] hash, CancellationToken ct)
        {
            if (ThrowSql) throw (Microsoft.Data.SqlClient.SqlException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Microsoft.Data.SqlClient.SqlException));
            return Task.FromResult(Session);
        }
        public Task<bool> RenewSessionAsync(StoredSession s, DateTime now, DateTime expiry, byte[] ticket, CancellationToken ct)
        { Session = s with { LastSeenAtUtc = now, IdleExpiresAtUtc = expiry, ProtectedTicket = ticket }; return Task.FromResult(true); }
        public Task RevokeSessionAsync(byte[] hash, DateTime now, string correlation, CancellationToken ct)
        { Session = Session! with { RevokedAtUtc = now, ProtectedTicket = null }; return Task.CompletedTask; }
    }
    private static async Task<(WebApplication App, HttpClient Client, Store Store)> Host()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        var store = new Store(); builder.Services.AddSingleton<IAccessStore>(store);
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddSingleton(TimeProvider.System); builder.Services.AddSingleton<SqlTicketStore>();
        builder.Services.AddAntiforgery(AccessRegistration.ConfigureAntiforgery);
        builder.Services.AddAuthentication(AccessRegistration.CookieScheme).AddCookie(AccessRegistration.CookieScheme, AccessRegistration.ConfigureCookie);
        builder.Services.AddOptions<CookieAuthenticationOptions>(AccessRegistration.CookieScheme).Configure<SqlTicketStore>((options, tickets) => options.SessionStore = tickets);
        var app = builder.Build(); app.UseKdiAccess(new AccessOptions());
        // This issuer exists exclusively in the test assembly. No production bypass or endpoint is registered.
        app.MapPost("/testing/issue", async (Microsoft.AspNetCore.Http.HttpContext c) =>
            await c.SignInAsync(AccessRegistration.CookieScheme, new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(SqlTicketStore.UserClaim, store.User.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, store.User.Id.ToString())], "test")), new AuthenticationProperties { IsPersistent = false }));
        await app.StartAsync(); var client = app.GetTestClient(); client.BaseAddress = new Uri("https://localhost:7443");
        return (app, client, store);
    }
    private static async Task<string> Login(HttpClient client)
    {
        using var result = await client.PostAsync("/testing/issue", null, TestContext.Current.CancellationToken);
        return result.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }
    [Fact]
    public async Task Anonymous_session_is_401_without_html_redirect()
    {
        var (app, client, _) = await Host(); await using var cleanup = app;
        var response = await client.GetAsync("/api/v1/session", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Null(response.Headers.Location);
    }
    [Fact]
    public async Task Cookie_has_host_http_only_secure_lax_attributes_and_no_tokens()
    {
        var (app, client, _) = await Host(); await using var cleanup = app;
        using var response = await client.PostAsync("/testing/issue", null, TestContext.Current.CancellationToken);
        var cookie = response.Headers.GetValues("Set-Cookie").Single();
        Assert.StartsWith("__Host-Kdi.Session=", cookie); Assert.Contains("secure", cookie); Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=lax", cookie); Assert.Contains("path=/", cookie); Assert.DoesNotContain("domain=", cookie);
        Assert.DoesNotContain("expires=", cookie); Assert.DoesNotContain("access_token", cookie);
    }
    [Fact]
    public async Task Authenticated_session_is_minimal_and_organization_outside_membership_is_404()
    {
        var (app, client, store) = await Host(); await using var cleanup = app;
        client.DefaultRequestHeaders.Add("Cookie", await Login(client));
        var session = await client.GetFromJsonAsync<SessionResponse>("/api/v1/session", TestContext.Current.CancellationToken);
        Assert.Equal(store.User.Id, session!.User.Id);
        var orgs = await client.GetFromJsonAsync<OrganizationList>("/api/v1/organizations", TestContext.Current.CancellationToken);
        Assert.Empty(orgs!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/organizations/{Guid.NewGuid()}/access", TestContext.Current.CancellationToken)).StatusCode);
    }
    [Theory][InlineData("https://evil.example")][InlineData("null")][InlineData("")]
    public async Task Mutation_rejects_missing_or_foreign_origin(string origin)
    {
        var (app, client, _) = await Host(); await using var cleanup = app;
        if (origin.Length > 0) client.DefaultRequestHeaders.Add("Origin", origin);
        var response = await client.PostAsync("/auth/logout", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Mutation_rejects_missing_csrf_even_with_valid_origin()
    {
        var (app, client, _) = await Host(); await using var cleanup = app;
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:7443");
        var response = await client.PostAsync("/auth/logout", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Csrf_token_and_cookie_allow_validation_but_not_open_redirect()
    {
        var (app, client, _) = await Host(); await using var cleanup = app;
        var csrf = await client.GetAsync("/auth/csrf", TestContext.Current.CancellationToken);
        var token = await csrf.Content.ReadFromJsonAsync<CsrfResponse>(TestContext.Current.CancellationToken);
        var cookie = csrf.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("httponly", cookie); Assert.Contains("secure", cookie); Assert.Contains("samesite=strict", cookie);
        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:7443");
        client.DefaultRequestHeaders.Add("X-KDI-CSRF", token!.RequestToken);
        var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["returnUrl"] = "//evil.example" }), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Sql_failure_returns_503_and_no_session_fallback()
    {
        var (app, client, store) = await Host(); await using var cleanup = app;
        client.DefaultRequestHeaders.Add("Cookie", await Login(client)); store.ThrowSql = true;
        var response = await client.GetAsync("/api/v1/session", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("SqlException", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Oidc_configuration_requires_code_pkce_signed_fixed_issuer_and_audience_without_tokens()
    {
        var options = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions();
        AccessRegistration.ConfigureOidc(options, new AccessOptions { Issuer = "https://issuer/", ClientId = "client" }, "synthetic-not-a-real-secret");
        Assert.Equal("code", options.ResponseType); Assert.Equal("query", options.ResponseMode); Assert.True(options.UsePkce);
        Assert.False(options.SaveTokens); Assert.True(options.RequireHttpsMetadata); Assert.False(options.MapInboundClaims);
        Assert.DoesNotContain("offline_access", options.Scope); Assert.Equal(new[] { "openid", "profile" }, options.Scope);
        Assert.Equal("https://issuer/", options.TokenValidationParameters.ValidIssuer);
        Assert.Equal("client", options.TokenValidationParameters.ValidAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime); Assert.True(options.TokenValidationParameters.RequireSignedTokens);
        Assert.Equal(new[] { "RS256" }, options.TokenValidationParameters.ValidAlgorithms);
        Assert.Equal(TimeSpan.FromMinutes(5), options.RemoteAuthenticationTimeout);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ProtocolValidator.NonceLifetime);
    }
    [Theory][InlineData("https://foreign/", true, false)][InlineData("https://issuer/", false, false)][InlineData("https://issuer/", true, true)]
    public async Task Validated_identity_is_local_exact_and_never_reactivates_disabled_user(string issuer, bool active, bool succeeds)
    {
        var options = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions();
        AccessRegistration.ConfigureOidc(options, new AccessOptions { Issuer = "https://issuer/", ClientId = "client" }, "synthetic-not-a-real-secret");
        var store = new Store(); store.User = store.User with { IsActive = active };
        var services = new ServiceCollection().AddSingleton<IAccessStore>(store).AddSingleton(TimeProvider.System).BuildServiceProvider();
        using var cleanup = services;
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = services };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("iss", issuer), new Claim("sub", " Subject "),
            new Claim("sid", "synthetic-sid"), new Claim("roles", "admin")], "oidc"));
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(http,
            new AuthenticationScheme("oidc", null, typeof(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectHandler)), options,
            principal, new AuthenticationProperties());
        await options.Events.OnTokenValidated(context);
        Assert.Equal(succeeds, context.Result?.Failure is null);
        Assert.Equal(0, store.CreatedUsers);
        if (succeeds)
        {
            Assert.Equal(("https://issuer/", " Subject "), store.IdentityObserved);
            Assert.Null(context.Principal!.FindFirst("roles")); Assert.Null(context.Principal.FindFirst("iss"));
            Assert.Equal(store.User.Id.ToString(), context.Principal.FindFirstValue(SqlTicketStore.UserClaim));
            Assert.False(context.Properties!.IsPersistent);
        }
    }
}
