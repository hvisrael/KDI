using System.Security;
using Microsoft.Data.SqlClient;
using Kulani.KDI.Api.Endpoints.Foundation;
using Kulani.KDI.Api.Http;
using Kulani.KDI.Infrastructure.Persistence;
using Kulani.KDI.Contracts.Foundation.V1;
using Microsoft.AspNetCore.Mvc;

const string credentialFlag = "--kdi-sql-console-credential";
var consoleCredential = args.Contains(credentialFlag, StringComparer.Ordinal);
using var password = consoleCredential ? ReadRuntimePassword() : null;
var credential = password is null ? null : new SqlCredential("kdi_app", password);
var builder = WebApplication.CreateBuilder(args.Where(value => value != credentialFlag).ToArray());
if (consoleCredential)
{
    // SQL provider diagnostics can contain connection details or exception payloads.
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
}
builder.Services.AddKdiPersistence(builder.Configuration.GetConnectionString("Kdi"), credential);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var status = context.ProblemDetails.Status ?? 500;
        // Override exception detail even in Development; never expose stack/SQL/configuration.
        context.ProblemDetails.Title = status >= 500 ? "Unexpected server error" : "Request failed";
        context.ProblemDetails.Detail = null;
        context.ProblemDetails.Extensions["code"] = status switch
        {
            404 => "not_found",
            405 => "method_not_allowed",
            _ => "request_failed"
        };
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "KDI Foundation API";
        document.Info.Version = "v1";
        document.Info.Description = "D1: public liveness and product metadata. No business endpoints.";
        document.Servers = [new() { Url = "/" }];
        return Task.CompletedTask;
    });
});

var app = builder.Build();
app.Use(async (context, next) =>
{
    var supplied = context.Request.Headers[CorrelationId.Header];
    context.TraceIdentifier = CorrelationId.Resolve(supplied.Count == 1 ? supplied[0] : null);
    context.Response.OnStarting(() =>
    {
        context.Response.Headers[CorrelationId.Header] = context.TraceIdentifier;
        context.Response.Headers.CacheControl = "no-store";
        return Task.CompletedTask;
    });
    await next(context);
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapFoundation();
app.MapGet("/health/ready", async (IServiceProvider services, CancellationToken cancellationToken) =>
    await services.IsKdiPersistenceReadyAsync(cancellationToken)
        ? Results.Json(new ReadinessResponse("ready"))
        : Results.Json(new ReadinessResponse("not_ready"), statusCode: StatusCodes.Status503ServiceUnavailable))
    .Produces<ReadinessResponse>().Produces<ReadinessResponse>(StatusCodes.Status503ServiceUnavailable)
    .WithName("GetReadiness").WithTags("Foundation")
    .WithSummary("Checks the configured SQL target and least-privilege runtime identity; never migrates.");
app.MapOpenApi("/openapi/{documentName}.json");
app.Run();

static SecureString ReadRuntimePassword()
{
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
        throw new InvalidOperationException("Private local interactive console required.");
    var result = new SecureString();
    try
    {
        Console.Write("Password for kdi_app (not echoed): ");
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Escape) throw new InvalidOperationException("Credential entry cancelled.");
            if (key.Key == ConsoleKey.Backspace)
            {
                if (result.Length > 0) result.RemoveAt(result.Length - 1);
            }
            else if (!char.IsControl(key.KeyChar)) result.AppendChar(key.KeyChar);
        }
        Console.WriteLine();
        if (result.Length == 0) throw new InvalidOperationException("Credential required.");
        result.MakeReadOnly();
        return result;
    }
    catch { result.Dispose(); throw; }
}

public partial class Program;
