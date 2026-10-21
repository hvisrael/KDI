using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.SqlIntegrationTests;

// The identity of the D2-B SQL harness.
//
// The harness has two identities and it chooses between them with an explicit switch:
//   - WINDOWS-INTEGRATED: the Windows identity of the process that runs the harness. It is the
//     historical contract of KDI_SQL_TEST_CONNECTION and it keeps working exactly as it was.
//   - SQL-KDI-TEST-RUNNER: the SQL login kdi_test_runner, member of the role kdi_tests, whose
//     password is read from the private interactive console of the operator. Its DML runs with
//     exactly the permissions of kdi_tests and with nothing else.
//
// The switch has no hidden default: when it is absent the harness uses the Windows identity of the
// process, and any value other than the two accepted ones stops the run with an explicit BLOCKED
// message. There is no silent fallback from one identity to the other.
internal static class SqlTestIdentity
{
    internal const string ConnectionSwitch = "KDI_SQL_TEST_CONNECTION";

    // The connection string of the harness and the credential of the chosen identity. The credential
    // is null when the harness runs with the Windows identity of the process.
    internal static (string ConnectionString, SqlCredential? Credential) Resolve()
    {
        // The missing connection string is reported before anything else, so that a run that is not
        // configured at all does not ask the operator for a password.
        var configured = Environment.GetEnvironmentVariable(ConnectionSwitch);
        if (string.IsNullOrWhiteSpace(configured)) throw new InvalidOperationException("BLOCKED: Test SQL connection required.");
        // The credential is resolved before the validation, because the validation of the target
        // depends on the identity that is going to use it. The provider reads the password once per
        // process, so the eight cases of a run share it and the operator is asked only once.
        var mode = SqlFixtureCredential.ResolveMode(Environment.GetEnvironmentVariable(SqlFixtureCredential.IdentitySwitch));
        var credential = mode == SqlFixtureIdentityMode.SqlTestRunner ? SqlFixtureCredential.Current : null;
        // The target is validated for the harness path: the test database, and a credential only when
        // it carries the identity of the harness. A credential of the runtime is refused here.
        var connectionString = SqlTarget.Validate(configured, SqlIdentityPath.TestHarness, testOnly: true, credential: credential);
        return (connectionString, credential);
    }

    // The connection of the harness. The credential travels with the connection and never with the
    // connection string, so the password is not in a string that can be logged or printed.
    internal static SqlConnection Connect(string connectionString, SqlCredential? credential) =>
        credential is null ? new SqlConnection(connectionString) : new SqlConnection(connectionString, credential);
}
