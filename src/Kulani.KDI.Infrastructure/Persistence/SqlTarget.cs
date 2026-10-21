using Microsoft.Data.SqlClient;

namespace Kulani.KDI.Infrastructure.Persistence;

public static class SqlTarget
{
    public const string Server = "DESKTOP-F25O3MF";
    public const string DevelopmentDatabase = "KulaniKDI_Dev";
    public const string TestDatabase = "KulaniKDI_Test";

    public static string Validate(string connectionString, bool testOnly = false)
    {
        SqlConnectionStringBuilder value;
        try { value = new(connectionString); }
        catch (ArgumentException) { throw new InvalidOperationException("Invalid KDI persistence configuration."); }
        if (!string.Equals(value.DataSource, Server, StringComparison.OrdinalIgnoreCase)
            || !(value.InitialCatalog == TestDatabase || (!testOnly && value.InitialCatalog == DevelopmentDatabase))
            || !value.IntegratedSecurity || value.UserID.Length != 0 || value.Password.Length != 0
            || value.Authentication != SqlAuthenticationMethod.NotSpecified
            || value.AttachDBFilename.Length != 0 || value.UserInstance || value.PersistSecurityInfo
            || value.FailoverPartner.Length != 0 || value.Encrypt == SqlConnectionEncryptOption.Optional)
            throw new InvalidOperationException("KDI persistence requires an authorized target and Windows integrated authentication.");
        return value.ConnectionString;
    }
}
