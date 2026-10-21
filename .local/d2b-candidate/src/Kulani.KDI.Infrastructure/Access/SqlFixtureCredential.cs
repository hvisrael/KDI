using System.Security;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.Infrastructure.Access;

// The identity that the SQL test harness uses for a run. The two modes are explicit: the mode is
// never guessed from the environment and an unknown value stops the run instead of falling back to
// another identity.
internal enum SqlFixtureIdentityMode
{
    // The Windows identity of the process that runs the harness. It is the historical contract of
    // KDI_SQL_TEST_CONNECTION and it keeps working.
    WindowsIntegrated = 0,
    // The SQL login kdi_test_runner, member of the role kdi_tests. The password is read from the
    // private interactive console of the operator.
    SqlTestRunner = 1
}

// Fixture-only credential of the SQL test harness.
//
// The harness of the D2-B stage writes and deletes rows in the three schemas of KulaniKDI_Test. That
// work belongs to the role kdi_tests and to nothing else, so the harness authenticates with the login
// kdi_test_runner, member of that role, and never with the runtime identity kdi_app.
//
// The password is read from a private interactive console, once per process, with the same protected
// read of OperatorConnection: the keys are intercepted, nothing is echoed, and the typed buffer is
// destroyed when the read finishes. The password never travels through an argument, an environment
// variable, a file or a log, and it never appears in an output, in the artifact of a run or in a
// message. The only thing that leaves this type is the SqlCredential that the connection needs, and
// it keeps a SecureString by reference, never a plain text.
//
// The type is internal: the product never builds this credential, and the harness reaches it through
// the InternalsVisibleTo of the assembly.
internal static class SqlFixtureCredential
{
    internal const string PasswordPrompt = "kdi_test_runner password (protected input): ";
    // The name of the switch that chooses the identity of the harness. The value is explicit and it
    // has no implicit default other than the historical Windows identity of the process.
    internal const string IdentitySwitch = "KDI_SQL_TEST_IDENTITY";
    internal const string WindowsIntegratedMode = "WINDOWS-INTEGRATED";
    internal const string SqlTestRunnerMode = "SQL-KDI-TEST-RUNNER";
    internal const string NotInteractiveMessage = "BLOCKED: the SQL test harness needs a private interactive console to read the password of the kdi_test_runner login. Run the harness from an interactive console session of the operator, with the express D2-B fixture authorization (KDI_D2B_SQL_AUTHORIZED=TEST-FIXTURES-AND-CLEANUP) and with the explicit identity switch " + IdentitySwitch + "=" + SqlTestRunnerMode + ". The password is never read from an argument, an environment variable or a file.";
    internal const string EmptyPasswordMessage = "BLOCKED: the password of the kdi_test_runner login is required. The harness authenticates with SQL authentication and the password is read from the private interactive console of the operator.";
    internal const string TooLongPasswordMessage = "BLOCKED: the password of the kdi_test_runner login is longer than the 128 characters that SQL Server accepts. The typed value is never shown.";
    internal const string UnknownModeMessage = "BLOCKED: unknown value of " + IdentitySwitch + ". The accepted values are " + WindowsIntegratedMode + " (the Windows identity of the process that runs the harness) and " + SqlTestRunnerMode + " (SQL authentication with the login kdi_test_runner, whose password is read from a private interactive console).";

    // Test seam for the console facts of the fixture path. It can only force the "not interactive"
    // state, and that state makes the provider refuse to build the credential and to read the
    // keyboard. It is internal and the product never sets it.
    private static volatile bool _notInteractiveForced;

    internal static void ForceNotInteractive(bool forced) => _notInteractiveForced = forced;

    // One provider per process: the password is asked once and it is reused by the eight cases of a
    // run, so the operator is never asked twice and no two cases read a key at the same time.
    private static readonly SqlFixtureCredentialProvider Provider = new(NotInteractive, ReadKey);

    internal static SqlCredential Current => Provider.Current;

    // The mode of the harness. An absent switch keeps the historical Windows identity and an unknown
    // value is refused, so no run can end up on an identity that nobody chose.
    internal static SqlFixtureIdentityMode ResolveMode(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) return SqlFixtureIdentityMode.WindowsIntegrated;
        if (string.Equals(configured, WindowsIntegratedMode, StringComparison.Ordinal)) return SqlFixtureIdentityMode.WindowsIntegrated;
        if (string.Equals(configured, SqlTestRunnerMode, StringComparison.Ordinal)) return SqlFixtureIdentityMode.SqlTestRunner;
        throw new InvalidOperationException(UnknownModeMessage);
    }

    private static bool NotInteractive() => Console.IsInputRedirected || Console.IsOutputRedirected || _notInteractiveForced;

    private static ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);
}

// The provider that reads the password once and keeps the result.
//
// The console state and the key source are seams, so the fixture path can be exercised without an
// interactive console; the prompt, the key flow and every message stay exactly the same in every
// case. The read happens inside a Lazy with the publication guarantee, so the factory runs once
// whatever the number of threads that ask for the credential, and the threads that arrive later wait
// for the first one instead of reading a key of their own.
internal sealed class SqlFixtureCredentialProvider
{
    // The maximum length of a password of SQL Server. The typed value is refused before it reaches
    // the server and the message never shows it.
    private const int MaxPasswordLength = 128;
    private readonly Func<bool> consoleRedirected;
    private readonly Func<ConsoleKeyInfo> readKey;
    private readonly Lazy<Outcome> lazy;

    internal SqlFixtureCredentialProvider(Func<bool> consoleRedirected, Func<ConsoleKeyInfo> readKey)
    {
        this.consoleRedirected = consoleRedirected;
        this.readKey = readKey;
        lazy = new Lazy<Outcome>(() => Read(this.consoleRedirected, this.readKey), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal SqlCredential Current
    {
        get
        {
            var outcome = lazy.Value;
            // The refusal is raised here and not inside the factory, so that every caller of a
            // refused provider gets the same explicit message and no key is ever read twice.
            return outcome.Credential ?? throw new InvalidOperationException(outcome.Failure!);
        }
    }

    private static Outcome Read(Func<bool> consoleRedirected, Func<ConsoleKeyInfo> readKey)
    {
        // The console facts are checked BEFORE the first key is read, so a console that is not
        // interactive never waits for a key that nobody is going to type.
        if (consoleRedirected()) return Outcome.Failed(SqlFixtureCredential.NotInteractiveMessage);
        Console.Write(SqlFixtureCredential.PasswordPrompt);
        var typed = new SecureString();
        try
        {
            while (true)
            {
                var key = readKey();
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace) { if (typed.Length > 0) typed.RemoveAt(typed.Length - 1); }
                else if (!char.IsControl(key.KeyChar)) typed.AppendChar(key.KeyChar);
            }
            Console.WriteLine();
            if (typed.Length == 0) return Outcome.Failed(SqlFixtureCredential.EmptyPasswordMessage);
            if (typed.Length > MaxPasswordLength) return Outcome.Failed(SqlFixtureCredential.TooLongPasswordMessage);
            typed.MakeReadOnly();
            // Lifecycle invariant, the same one of OperatorConnection: SqlCredential keeps the
            // reference of the SecureString that it receives and never copies it, so the copy that is
            // handed over stays alive and read-only for as long as the credential is used.
            var owned = typed.Copy();
            owned.MakeReadOnly();
            return Outcome.Succeeded(new SqlCredential(SqlTarget.TestHarnessIdentity, owned));
        }
        finally { typed.Dispose(); }
    }

    // The result of the read. It carries the credential or the reason of the refusal, and never the
    // password itself.
    private sealed class Outcome
    {
        internal SqlCredential? Credential { get; private init; }
        internal string? Failure { get; private init; }
        internal static Outcome Succeeded(SqlCredential credential) => new() { Credential = credential };
        internal static Outcome Failed(string message) => new() { Failure = message };
    }
}
