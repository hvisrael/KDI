namespace Kulani.KDI.Contracts.Foundation.V1;

public sealed record SystemInfoResponse(string Product, string ApiVersion);

public sealed record LivenessResponse(string Status);
