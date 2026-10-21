using System.Security.Claims;
using Kulani.KDI.Application.Access;
using Kulani.KDI.Contracts.Access;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.Api.Auth;

public static class AccessEndpoints
{
    public static void UseKdiAccess(this WebApplication app, AccessOptions options)
    {
        // Return controlled failures even when authentication storage is unavailable; never use a stale session.
        app.Use(async (context, next) =>
        {
            try
            {
                if (context.Request.Path.StartsWithSegments("/auth") || context.Request.Path.StartsWithSegments("/api"))
                {
                    if (!context.Request.IsHttps || context.Request.Host.Value != new Uri(options.Origin).Authority)
                    { await Failure(context, 400, "invalid_origin"); return; }
                    if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method)
                        || HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
                    {
                        if (context.Request.Headers.Origin.Count != 1 || context.Request.Headers.Origin[0] != options.Origin)
                        { await Failure(context, 403, "invalid_origin"); return; }
                        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
                        catch (AntiforgeryValidationException) { await Failure(context, 403, "invalid_csrf"); return; }
                    }
                }
                await next(context);
            }
            catch (SqlException) when (!context.Response.HasStarted) { await Failure(context, 503, "storage_unavailable"); }
        });
        app.UseAuthentication();
        app.MapGet("/auth/csrf", (HttpContext c, IAntiforgery csrf) =>
            Results.Json(new CsrfResponse(csrf.GetAndStoreTokens(c).RequestToken!)));
        app.MapPost("/auth/login", async (HttpContext c) =>
        {
            if (!c.Request.HasFormContentType) return Results.BadRequest(new { code = "form_required" });
            var form = await c.Request.ReadFormAsync(c.RequestAborted);
            var target = form["returnUrl"].ToString();
            if (target.Length == 0) target = "/";
            if (!IsLocalReturnUrl(target)) return Results.BadRequest(new { code = "invalid_return_url" });
            return Results.Challenge(new AuthenticationProperties { RedirectUri = target, IsPersistent = false }, [AccessRegistration.OidcScheme]);
        });
        app.MapPost("/auth/logout", async (HttpContext c) =>
        {
            var auth = await c.AuthenticateAsync(AccessRegistration.CookieScheme);
            if (!auth.Succeeded) return Results.Unauthorized();
            var hint = c.User.FindFirstValue("sid");
            // Cookie sign-out revokes SQL first. Only then initiate remote sign-out.
            await c.SignOutAsync(AccessRegistration.CookieScheme);
            var properties = new AuthenticationProperties { RedirectUri = "/" };
            if (!string.IsNullOrEmpty(hint)) properties.Items["logout_hint"] = hint;
            return Results.SignOut(properties, [AccessRegistration.OidcScheme]);
        });
        app.MapGet("/api/v1/session", async (HttpContext c, IAccessStore store, SqlTicketStore tickets) =>
        {
            var auth = await c.AuthenticateAsync(AccessRegistration.CookieScheme);
            if (!auth.Succeeded || !TryUser(c, out var userId) || auth.Properties is null ||
                !auth.Properties.Items.TryGetValue(SqlTicketStore.SessionHandle, out var handle) || handle is null)
                return Results.Unauthorized();
            var session = await tickets.GetCurrentAsync(handle);
            var user = await store.FindUserByIdAsync(userId, c.RequestAborted);
            if (session is null || user?.IsActive != true) return Results.Unauthorized();
            return Results.Json(new SessionResponse(new(user.Id, user.DisplayName), session.CreatedAtUtc,
                session.IdleExpiresAtUtc, session.AbsoluteExpiresAtUtc));
        });
        app.MapGet("/api/v1/organizations", async (HttpContext c, IAccessStore store) =>
            !TryUser(c, out var userId) ? Results.Unauthorized() :
                Results.Json(new OrganizationList(await store.GetOrganizationsAsync(userId, c.RequestAborted))));
        app.MapGet("/api/v1/organizations/{orgId:guid}/access", async (Guid orgId, HttpContext c, IAccessStore store) =>
        {
            if (!TryUser(c, out var userId)) return Results.Unauthorized();
            var access = await store.GetOrganizationAccessAsync(userId, orgId, c.RequestAborted);
            return access is null ? Results.NotFound(new { code = "not_found" }) : Results.Json(access);
        });
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }
    public static bool IsLocalReturnUrl(string value) => value.StartsWith('/') && !value.StartsWith("//")
        && !value.Contains('\\') && !value.Any(char.IsControl) && !value.Contains('%');
    private static bool TryUser(HttpContext context, out Guid id) =>
        Guid.TryParse(context.User.Identity?.IsAuthenticated == true ? context.User.FindFirstValue(SqlTicketStore.UserClaim) : null, out id);
    private static async Task Failure(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { code, correlationId = context.TraceIdentifier });
    }
}
