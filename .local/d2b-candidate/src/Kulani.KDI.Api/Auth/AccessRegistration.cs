using System.Security.Claims;
using Kulani.KDI.Application.Access;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Kulani.KDI.Api.Auth;

public sealed class AccessOptions
{
    public bool Enabled { get; set; }
    public string Origin { get; set; } = "https://localhost:7443";
    public string Issuer { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string KeyDirectory { get; set; } = "";
    public void Validate()
    {
        if (Origin != "https://localhost:7443" || !Uri.TryCreate(Issuer, UriKind.Absolute, out var issuer)
            || issuer.Scheme != "https" || !string.IsNullOrEmpty(issuer.Query) || !string.IsNullOrEmpty(issuer.Fragment)
            || !Issuer.EndsWith('/') || string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(KeyDirectory)
            || !Path.IsPathFullyQualified(KeyDirectory)) throw new InvalidOperationException("Invalid KDI access configuration.");
        var repoRoot = FindRepositoryRoot();
        // Deployment must provide an existing private operational key directory; never create it during startup.
        if (!Directory.Exists(KeyDirectory) || (Path.GetFullPath(KeyDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Existing external Data Protection key directory required.");
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Pilot requires Windows DPAPI.");
        var owner = WindowsIdentity.GetCurrent().User!;
        var allowed = new[] { owner.Value, "S-1-5-18", "S-1-5-32-544" };
        var acl = new DirectoryInfo(KeyDirectory).GetAccessControl();
        var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>();
        foreach (var rule in rules)
            if (rule.AccessControlType == AccessControlType.Allow && !allowed.Contains(rule.IdentityReference.Value))
                throw new InvalidOperationException("Key directory ACL grants access beyond the current operator, SYSTEM and Administrators.");
    }
    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        string? sourceRoot = null;
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
                return current.FullName + Path.DirectorySeparatorChar;
            if (File.Exists(Path.Combine(current.FullName, "Kulani.KDI.slnx"))) sourceRoot = current.FullName + Path.DirectorySeparatorChar;
            current = current.Parent;
        }
        return sourceRoot ?? Path.GetFullPath(AppContext.BaseDirectory);
    }
}

public static class AccessRegistration
{
    public const string CookieScheme = "KdiSession";
    public const string OidcScheme = "KdiOidc";
    public static IServiceCollection AddKdiAccess(this IServiceCollection services, AccessOptions options, string secret)
    {
        options.Validate();
        if (string.IsNullOrEmpty(secret)) throw new InvalidOperationException("Private OIDC client secret required.");
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Pilot requires Windows DPAPI.");
        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<SqlTicketStore>();
        services.AddScoped<AccessPermissionPolicy>();
        services.AddDataProtection().SetApplicationName("KDI.Identity.Local.v1")
            .PersistKeysToFileSystem(new DirectoryInfo(options.KeyDirectory)).ProtectKeysWithDpapi();
        services.AddAntiforgery(ConfigureAntiforgery);
        services.AddAuthentication(o => { o.DefaultScheme = CookieScheme; o.DefaultChallengeScheme = OidcScheme; })
            .AddCookie(CookieScheme, ConfigureCookie)
            .AddOpenIdConnect(OidcScheme, o => ConfigureOidc(o, options, secret));
        services.AddOptions<CookieAuthenticationOptions>(CookieScheme).Configure<IServiceProvider>((o, provider) =>
            o.SessionStore = new ScopedTicketStore(provider));
        return services;
    }
    public static void ConfigureOidc(OpenIdConnectOptions o, AccessOptions options, string secret)
    {
                o.SignInScheme = CookieScheme;
                o.Authority = options.Issuer;
                o.ClientId = options.ClientId;
                o.ClientSecret = secret;
                o.ResponseType = OpenIdConnectResponseType.Code;
                o.ResponseMode = OpenIdConnectResponseMode.Query;
                o.UsePkce = true;
                o.SaveTokens = false;
                o.RequireHttpsMetadata = true;
                o.NonceCookie.HttpOnly = true;
                o.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                o.NonceCookie.SameSite = SameSiteMode.None;
                o.CorrelationCookie.HttpOnly = true;
                o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                o.CorrelationCookie.SameSite = SameSiteMode.None;
                o.MapInboundClaims = false;
                o.GetClaimsFromUserInfoEndpoint = false;
                o.Scope.Clear(); o.Scope.Add("openid"); o.Scope.Add("profile");
                o.CallbackPath = "/auth/callback";
                o.SignedOutCallbackPath = "/auth/signout-callback";
                o.SignedOutRedirectUri = "/";
                o.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(5);
                o.ProtocolValidator.NonceLifetime = TimeSpan.FromMinutes(5);
                o.MaxAge = TimeSpan.FromHours(1);
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = options.Issuer, ValidateAudience = true,
                    ValidAudience = options.ClientId, ValidateLifetime = true, RequireExpirationTime = true,
                    RequireSignedTokens = true, ValidAlgorithms = [SecurityAlgorithms.RsaSha256], ClockSkew = TimeSpan.FromSeconds(30)
                };
                o.Events.OnTokenValidated = async c =>
                {
                    var issuer = c.Principal!.FindFirstValue("iss");
                    var subject = c.Principal!.FindFirstValue("sub");
                    if (issuer != options.Issuer || subject is null || subject.Length is < 1 or > 255 || issuer.Length > 512)
                    { c.Fail("Invalid identity."); return; }
                    var store = c.HttpContext.RequestServices.GetRequiredService<IAccessStore>();
                    var clock = c.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                    var user = await store.FindUserAsync(issuer, subject, c.HttpContext.RequestAborted);
                    user ??= await store.CreateUserAsync(issuer, subject, "KDI user", clock.GetUtcNow().UtcDateTime,
                        c.HttpContext.TraceIdentifier, c.HttpContext.RequestAborted);
                    if (!user.IsActive) { c.Fail("Identity unavailable."); return; }
                    var claims = new List<Claim> { new(SqlTicketStore.UserClaim, user.Id.ToString()), new(ClaimTypes.NameIdentifier, user.Id.ToString()) };
                    var sid = c.Principal!.FindFirstValue("sid");
                    if (!string.IsNullOrEmpty(sid)) claims.Add(new("sid", sid));
                    c.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieScheme));
                    c.Properties!.IsPersistent = false;
                };
                o.Events.OnRedirectToIdentityProviderForSignOut = c =>
                {
                    c.ProtocolMessage.IdTokenHint = null;
                    if (c.Properties.Items.TryGetValue("logout_hint", out var hint)) c.ProtocolMessage.SetParameter("logout_hint", hint);
                    c.ProtocolMessage.ClientId = options.ClientId;
                    return Task.CompletedTask;
                };
                o.Events.OnRemoteFailure = c => { c.HandleResponse(); c.Response.StatusCode = 401; return c.Response.WriteAsJsonAsync(new { code = "authentication_failed" }); };
                }
    public static void ConfigureAntiforgery(Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions o)
    {
            o.HeaderName = "X-KDI-CSRF";
            o.Cookie.Name = "__Host-Kdi.Csrf";
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.Path = "/";
            }
    public static void ConfigureCookie(CookieAuthenticationOptions o)
    {
                o.Cookie.Name = "__Host-Kdi.Session";
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.Path = "/";
                o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                o.SlidingExpiration = true;
                o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
                o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
                }
    private sealed class ScopedTicketStore(IServiceProvider provider) : ITicketStore
    {
        private async Task<T> Invoke<T>(Func<SqlTicketStore, Task<T>> operation)
        { using var scope = provider.CreateScope(); return await operation(scope.ServiceProvider.GetRequiredService<SqlTicketStore>()); }
        public Task<string> StoreAsync(AuthenticationTicket ticket) => Invoke(s => s.StoreAsync(ticket));
        public Task<AuthenticationTicket?> RetrieveAsync(string key) => Invoke(s => s.RetrieveAsync(key));
        public async Task RenewAsync(string key, AuthenticationTicket ticket) => await Invoke(async s => { await s.RenewAsync(key, ticket); return true; });
        public async Task RemoveAsync(string key) => await Invoke(async s => { await s.RemoveAsync(key); return true; });
    }
}
