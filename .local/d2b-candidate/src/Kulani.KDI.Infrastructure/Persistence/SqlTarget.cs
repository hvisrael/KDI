using Microsoft.Data.SqlClient;

namespace Kulani.KDI.Infrastructure.Persistence;

// The identity path of a persistence call.
//
// The path is explicit and it is never inferred from the target database: "testOnly" only restricts
// the database that a call may reach, it never selects the identity that a call may use. The product
// runtime and the SQL test harness are two different paths with two different identities, and neither
// one can use the identity of the other one.
public enum SqlIdentityPath
{
    // The path of the product runtime. The only SQL identity it accepts is kdi_app.
    Runtime = 0,
    // The path of the SQL test harness. The only SQL identity it accepts is kdi_test_runner.
    TestHarness = 1
}

public static class SqlTarget
{
    public const string Server = "DESKTOP-F25O3MF";
    public const string DevelopmentDatabase = "KulaniKDI_Dev";
    public const string TestDatabase = "KulaniKDI_Test";
    // The SQL login of the product runtime, member of the role kdi_application.
    public const string RuntimeIdentity = "kdi_app";
    // The SQL login of the SQL test harness, member of the role kdi_tests.
    public const string TestHarnessIdentity = "kdi_test_runner";

    // The runtime entry point. It keeps the signature and the semantics that the API already had, so
    // the runtime path of the product is not relaxed by anything that follows: the only SQL identity
    // it accepts is kdi_app and, without a credential, the only authentication that it accepts is the
    // Windows integrated one. A caller of the harness has to name the harness path explicitly, and
    // the overload below is the only way to reach it.
    public static string Validate(string connectionString, bool testOnly = false, SqlCredential? credential = null) =>
        Validate(connectionString, SqlIdentityPath.Runtime, testOnly, credential);

    // The entry point that names the path. The path is a required argument, so a caller can never
    // reach the rules of one path without saying that it is that path.
    public static string Validate(string connectionString, SqlIdentityPath path, bool testOnly = false, SqlCredential? credential = null)
    {
        SqlConnectionStringBuilder value;
        try { value = new(connectionString); }
        catch (ArgumentException) { throw new InvalidOperationException("Invalid KDI persistence configuration."); }
        // An unknown path is refused before anything else is looked at: the guard is closed by
        // default and it is only opened by a path that this type knows.
        var identity = path switch
        {
            SqlIdentityPath.Runtime => RuntimeIdentity,
            SqlIdentityPath.TestHarness => TestHarnessIdentity,
            _ => throw new InvalidOperationException("KDI persistence requires an authorized identity path.")
        };
        // The database rule of the path. The harness only ever reaches the test database, whatever
        // the "testOnly" flag says, and the runtime keeps the rule that it always had.
        var databaseAllowed = path switch
        {
            SqlIdentityPath.Runtime => value.InitialCatalog == TestDatabase || (!testOnly && value.InitialCatalog == DevelopmentDatabase),
            SqlIdentityPath.TestHarness => value.InitialCatalog == TestDatabase,
            _ => false
        };
        // The identity rule of the path. A credential is only accepted when it carries exactly the
        // identity of the path and the connection is not integrated. Without a credential, only the
        // Windows integrated authentication is accepted, as it was before.
        var authenticationAllowed = credential is null
            ? value.IntegratedSecurity
            : !value.IntegratedSecurity && string.Equals(credential.UserId, identity, StringComparison.Ordinal);
        if (!string.Equals(value.DataSource, Server, StringComparison.OrdinalIgnoreCase)
            || !databaseAllowed
            || !authenticationAllowed
            || value.UserID.Length != 0 || value.Password.Length != 0
            || value.Authentication != SqlAuthenticationMethod.NotSpecified
            || value.AttachDBFilename.Length != 0 || value.UserInstance || value.PersistSecurityInfo
            || value.FailoverPartner.Length != 0 || value.Encrypt == SqlConnectionEncryptOption.Optional)
            throw new InvalidOperationException("KDI persistence requires an authorized target and approved authentication mode.");
        return value.ConnectionString;
    }
}
