namespace Kulani.KDI.Api.Http;

public static class CorrelationId
{
    public const string Header = "X-Correlation-ID";

    // Restrict reflected identifiers to bounded ASCII without whitespace/control characters.
    public static string Resolve(string? supplied)
    {
        if (supplied is { Length: >= 1 and <= 64 }
            && supplied.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
        {
            return supplied;
        }

        return Guid.NewGuid().ToString("N");
    }
}
