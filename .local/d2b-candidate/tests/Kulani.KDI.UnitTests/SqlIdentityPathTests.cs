using System.Security;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.UnitTests;

// The identity rule of the two paths of SqlTarget.
//
// The table of the rule is the whole contract of the guard:
//
//   path          | credential        | accepted
//   --------------+-------------------+---------------------------------------------------
//   Runtime       | kdi_app           | the SQL identity of the product runtime
//   Runtime       | none              | the Windows identity of the process
//   TestHarness   | kdi_test_runner   | the SQL identity of the SQL fixtures
//   TestHarness   | none              | the Windows identity of the process
//
// Every other identity is refused in both paths, and neither path accepts the identity of the other
// one. The rule is closed by default: a path that is not named is the runtime one, and a path that is
// not known is refused.
public sealed class SqlIdentityPathTests
{
    private const string TestTarget = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=false;Encrypt=true";
    private const string DevelopmentTarget = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Dev;Integrated Security=false;Encrypt=true";
    private const string IntegratedTestTarget = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=true;Encrypt=true";

    private static SqlCredential Credential(string user)
    {
        var secret = new SecureString();
        secret.AppendChar('x');
        secret.MakeReadOnly();
        return new SqlCredential(user, secret);
    }

    private static void AssertTarget(string validated, string database, bool integrated)
    {
        var value = new SqlConnectionStringBuilder(validated);
        Assert.Equal(SqlTarget.Server, value.DataSource);
        Assert.Equal(database, value.InitialCatalog);
        Assert.Equal(integrated, value.IntegratedSecurity);
        Assert.Empty(value.UserID);
        Assert.Empty(value.Password);
    }

    [Fact]
    public void Runtime_path_accepts_the_runtime_identity()
    {
        var validated = SqlTarget.Validate(TestTarget, SqlIdentityPath.Runtime, credential: Credential("kdi_app"));
        AssertTarget(validated, SqlTarget.TestDatabase, false);
    }

    [Theory]
    [InlineData("kdi_test_runner")]
    [InlineData("kdi_migrator")]
    [InlineData("KDI_APP")]
    [InlineData("kdi_app ")]
    [InlineData("")]
    public void Runtime_path_refuses_every_other_identity(string user) =>
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, SqlIdentityPath.Runtime, credential: Credential(user)));

    [Fact]
    public void Harness_path_accepts_the_harness_identity()
    {
        var validated = SqlTarget.Validate(TestTarget, SqlIdentityPath.TestHarness, testOnly: true, credential: Credential("kdi_test_runner"));
        AssertTarget(validated, SqlTarget.TestDatabase, false);
    }

    [Theory]
    [InlineData("kdi_app")]
    [InlineData("kdi_migrator")]
    [InlineData("KDI_TEST_RUNNER")]
    [InlineData("kdi_test_runner ")]
    [InlineData("")]
    public void Harness_path_refuses_every_other_identity(string user) =>
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, SqlIdentityPath.TestHarness, testOnly: true, credential: Credential(user)));

    [Fact]
    public void The_two_paths_refuse_each_other()
    {
        // The identity of the runtime is not an identity of the harness.
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, SqlIdentityPath.TestHarness, testOnly: true, credential: Credential("kdi_app")));
        // The identity of the harness is not an identity of the runtime.
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, SqlIdentityPath.Runtime, credential: Credential("kdi_test_runner")));
    }

    [Fact]
    public void The_runtime_entry_point_of_the_api_is_not_relaxed()
    {
        // The overload that the API uses keeps the runtime rules: it accepts the runtime identity and
        // it refuses the identity of the harness and any other one.
        AssertTarget(SqlTarget.Validate(TestTarget, credential: Credential("kdi_app")), SqlTarget.TestDatabase, false);
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, credential: Credential("kdi_test_runner")));
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, credential: Credential("kdi_migrator")));
    }

    [Fact]
    public void The_harness_path_only_reaches_the_test_database()
    {
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(DevelopmentTarget, SqlIdentityPath.TestHarness, credential: Credential("kdi_test_runner")));
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(DevelopmentTarget, SqlIdentityPath.TestHarness, testOnly: false, credential: Credential("kdi_test_runner")));
    }

    [Fact]
    public void The_runtime_path_keeps_its_database_rule()
    {
        // The runtime reaches the development database only when the call is not restricted, exactly
        // as it did before the identity path existed.
        AssertTarget(SqlTarget.Validate(DevelopmentTarget, SqlIdentityPath.Runtime, credential: Credential("kdi_app")), SqlTarget.DevelopmentDatabase, false);
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(DevelopmentTarget, SqlIdentityPath.Runtime, testOnly: true, credential: Credential("kdi_app")));
    }

    [Fact]
    public void Without_a_credential_both_paths_keep_the_windows_integrated_contract()
    {
        AssertTarget(SqlTarget.Validate(IntegratedTestTarget, SqlIdentityPath.Runtime), SqlTarget.TestDatabase, true);
        AssertTarget(SqlTarget.Validate(IntegratedTestTarget, SqlIdentityPath.TestHarness, testOnly: true), SqlTarget.TestDatabase, true);
        // A connection string that asks for SQL authentication without a credential is refused in both
        // paths: there is no way to reach the server without an identity.
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, SqlIdentityPath.Runtime));
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, SqlIdentityPath.TestHarness, testOnly: true));
    }

    [Theory]
    [InlineData(";Integrated Security=true")]
    [InlineData(";User ID=kdi_app")]
    [InlineData(";Password=synthetic")]
    public void The_identity_of_a_path_never_travels_in_the_connection_string(string extra)
    {
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget + extra, SqlIdentityPath.Runtime, credential: Credential("kdi_app")));
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget + extra, SqlIdentityPath.TestHarness, testOnly: true, credential: Credential("kdi_test_runner")));
    }

    [Fact]
    public void An_unknown_identity_path_is_refused()
    {
        var error = Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(TestTarget, (SqlIdentityPath)7, credential: Credential("kdi_app")));
        Assert.Contains("identity path", error.Message);
    }
}
