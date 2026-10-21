using Kulani.KDI.Contracts.Foundation.V1;

namespace Kulani.KDI.Application.Foundation;

public static class SystemInfo
{
    public static SystemInfoResponse Describe() => new("Kulani Delivery Intelligence", "v1");
}
