using Kulani.KDI.Api.Endpoints.Foundation;
using Kulani.KDI.Api.Http;
using Kulani.KDI.Infrastructure.Persistence;
using Kulani.KDI.Contracts.Foundation.V1;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKdiPersistence(builder.Configuration.GetConnectionString("Kdi"));
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

public partial class Program;
