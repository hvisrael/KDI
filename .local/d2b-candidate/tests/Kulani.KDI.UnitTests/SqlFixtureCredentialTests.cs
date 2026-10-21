using System.Security;
using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.UnitTests;

// The fixture credential of the SQL test harness.
//
// The password of the kdi_test_runner login is read from a private interactive console, once per
// process, and it is never echoed, never reported in a message and never asked twice. These tests
// capture the console output, so they never run in parallel with the other console tests.
[Collection("KdiOperatorConsole")]
public sealed class SqlFixtureCredentialTests
{
    private const string SyntheticSecret = "synthetic-fixture-secret";

    private static ConsoleKeyInfo Key(char character, ConsoleKey key = ConsoleKey.A) =>
        new(character, key, false, false, false);

    private static Queue<ConsoleKeyInfo> SecretKeys()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        foreach (var character in SyntheticSecret) keys.Enqueue(Key(character));
        keys.Enqueue(Key('\r', ConsoleKey.Enter));
        return keys;
    }

    private static string Capture(Action action)
    {
        var original = Console.Out;
        var writer = new StringWriter();
        try { Console.SetOut(writer); action(); return writer.ToString(); }
        finally { Console.SetOut(original); }
    }

    [Fact]
    public void Fixture_credential_carries_the_harness_identity()
    {
        var keys = SecretKeys();
        var provider = new SqlFixtureCredentialProvider(() => false, () => keys.Dequeue());
        var credential = provider.Current;
        Assert.Equal(SqlTarget.TestHarnessIdentity, credential.UserId);
        Assert.Equal("kdi_test_runner", credential.UserId);
        var handed = credential.Password;
        Assert.NotNull(handed);
        Assert.True(handed!.IsReadOnly());
        Assert.Equal(SyntheticSecret.Length, handed.Length);
    }

    [Fact]
    public void Fixture_credential_is_read_once_and_reused_by_every_case()
    {
        var reads = 0;
        var keys = SecretKeys();
        var provider = new SqlFixtureCredentialProvider(() => false, () => { reads++; return keys.Dequeue(); });
        var first = provider.Current;
        var second = provider.Current;
        // The same credential for the eight cases of a run: the keys are read once and the operator is
        // never asked twice. The queue of the keys is empty after the first read, so a second read
        // would fail the test.
        Assert.Same(first, second);
        Assert.Equal(SyntheticSecret.Length + 1, reads);
    }

    [Fact]
    public void Fixture_credential_refuses_a_console_that_is_not_interactive()
    {
        var reads = 0;
        var provider = new SqlFixtureCredentialProvider(() => true, () => { reads++; return Key('\r', ConsoleKey.Enter); });
        var error = Assert.Throws<InvalidOperationException>(() => provider.Current);
        // The message is explicit and it names what is missing. The keyboard is never read, so the run
        // never waits for a key that nobody is going to type.
        Assert.Contains("BLOCKED", error.Message);
        Assert.Contains("kdi_test_runner", error.Message);
        Assert.Contains("interactive console", error.Message);
        Assert.Contains("KDI_D2B_SQL_AUTHORIZED", error.Message);
        Assert.Contains("KDI_SQL_TEST_IDENTITY", error.Message);
        Assert.Equal(0, reads);
    }

    [Fact]
    public void Fixture_credential_does_not_read_the_keyboard_again_after_a_refusal()
    {
        var reads = 0;
        var provider = new SqlFixtureCredentialProvider(() => true, () => { reads++; return Key('\r', ConsoleKey.Enter); });
        Assert.Throws<InvalidOperationException>(() => provider.Current);
        Assert.Throws<InvalidOperationException>(() => provider.Current);
        Assert.Equal(0, reads);
    }

    [Fact]
    public void Fixture_credential_never_writes_the_password()
    {
        var keys = SecretKeys();
        var provider = new SqlFixtureCredentialProvider(() => false, () => keys.Dequeue());
        var captured = Capture(() => { _ = provider.Current; });
        // The prompt is visible and the password is not: the keys are intercepted and nothing is
        // echoed, so the console of the run never carries the secret.
        Assert.Contains(SqlFixtureCredential.PasswordPrompt, captured);
        Assert.DoesNotContain(SyntheticSecret, captured);
    }

    [Fact]
    public void Fixture_credential_never_reports_the_password()
    {
        var keys = SecretKeys();
        var provider = new SqlFixtureCredentialProvider(() => false, () => keys.Dequeue());
        var credential = provider.Current;
        // The credential never carries a plain text and it never prints itself.
        Assert.DoesNotContain(SyntheticSecret, credential.ToString());
        _ = Assert.IsType<SecureString>(credential.Password);
        // A message that is raised with the credential in the hand never carries the secret either.
        var error = Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate("Server=other;Database=KulaniKDI_Test;Integrated Security=false", SqlIdentityPath.TestHarness, testOnly: true, credential: credential));
        Assert.DoesNotContain(SyntheticSecret, error.Message);
    }

    [Fact]
    public void Fixture_credential_refuses_an_empty_password()
    {
        var provider = new SqlFixtureCredentialProvider(() => false, () => Key('\r', ConsoleKey.Enter));
        var error = Assert.Throws<InvalidOperationException>(() => provider.Current);
        Assert.Contains("BLOCKED", error.Message);
        Assert.Contains("kdi_test_runner", error.Message);
    }

    [Fact]
    public void Fixture_credential_refuses_a_password_that_is_too_long()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        for (var index = 0; index < 129; index++) keys.Enqueue(Key('a'));
        keys.Enqueue(Key('\r', ConsoleKey.Enter));
        var provider = new SqlFixtureCredentialProvider(() => false, () => keys.Dequeue());
        var error = Assert.Throws<InvalidOperationException>(() => provider.Current);
        Assert.Contains("BLOCKED", error.Message);
        // The typed value never appears in the message that refuses it.
        Assert.DoesNotContain("aaaaaaaaaa", error.Message);
    }

    [Fact]
    public void The_static_fixture_credential_refuses_a_console_that_is_not_interactive()
    {
        // The forced state makes the console facts deterministic, whatever the handles of the test
        // process are, and it can only make the fixture path stricter, so the keyboard is never read.
        SqlFixtureCredential.ForceNotInteractive(true);
        try
        {
            var error = Assert.Throws<InvalidOperationException>(() => SqlFixtureCredential.Current);
            Assert.Equal(SqlFixtureCredential.NotInteractiveMessage, error.Message);
            Assert.Contains("BLOCKED", error.Message);
        }
        finally { SqlFixtureCredential.ForceNotInteractive(false); }
    }

    [Fact]
    public void The_identity_mode_of_the_harness_is_explicit()
    {
        // Without the switch the harness keeps the historical Windows identity of the process.
        Assert.Equal(SqlFixtureIdentityMode.WindowsIntegrated, SqlFixtureCredential.ResolveMode(null));
        Assert.Equal(SqlFixtureIdentityMode.WindowsIntegrated, SqlFixtureCredential.ResolveMode(""));
        Assert.Equal(SqlFixtureIdentityMode.WindowsIntegrated, SqlFixtureCredential.ResolveMode("WINDOWS-INTEGRATED"));
        Assert.Equal(SqlFixtureIdentityMode.SqlTestRunner, SqlFixtureCredential.ResolveMode("SQL-KDI-TEST-RUNNER"));
        // Any other value stops the run instead of falling back to another identity.
        var error = Assert.Throws<InvalidOperationException>(() => SqlFixtureCredential.ResolveMode("kdi_test_runner"));
        Assert.Contains("BLOCKED", error.Message);
        Assert.Contains("KDI_SQL_TEST_IDENTITY", error.Message);
    }

    [Fact]
    public void The_credential_of_the_harness_is_accepted_only_by_the_harness_path()
    {
        var keys = SecretKeys();
        var provider = new SqlFixtureCredentialProvider(() => false, () => keys.Dequeue());
        var credential = provider.Current;
        const string target = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=false;Encrypt=true";
        // The credential of the harness opens the harness path and only the harness path.
        _ = SqlTarget.Validate(target, SqlIdentityPath.TestHarness, testOnly: true, credential: credential);
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(target, SqlIdentityPath.Runtime, credential: credential));
    }
}
