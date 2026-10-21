using Kulani.KDI.Application.Foundation;
using Kulani.KDI.Contracts.Foundation.V1;

namespace Kulani.KDI.Api.Endpoints.Foundation;

public static class FoundationEndpoints
{
    public static void MapFoundation(this WebApplication app)
    {
        app.MapGet("/health/live", () => TypedResults.Ok(new LivenessResponse("healthy")))
            .WithName("GetLiveness").WithTags("Foundation")
            .WithSummary("Process liveness only; does not check database readiness.");

        app.MapGet("/api/v1/system/info", () => TypedResults.Ok(SystemInfo.Describe()))
            .WithName("GetSystemInfo").WithTags("Foundation")
            .WithSummary("Public product metadata without environment or user details.");
    }
}
