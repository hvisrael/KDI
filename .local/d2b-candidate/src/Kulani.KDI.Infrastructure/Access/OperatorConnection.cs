using Microsoft.Data.SqlClient;
using System.Security;
using Kulani.KDI.Infrastructure.Persistence;

namespace Kulani.KDI.Infrastructure.Access;

// Operator-only path. Runtime SqlTarget and its kdi_app guard remain unchanged.
public static class OperatorConnection
{
    internal const string PasswordPrompt = "kdi_migrator password (protected input): ";

    // Test seam for the console facts of the operator path. It is deliberately one-directional: it can
    // only force the "not interactive" state, and that state makes the guard refuse to build a credential
    // and the keyboard unreadable. It can never turn the guard off, so it does not open a way to build a
    // credential without a private interactive console. It is internal and the product never sets it.
    private static volatile bool _notInteractiveForced;

    internal static void ForceNotInteractive(bool forced) => _notInteractiveForced = forced;

    // Operator session: the connection plus the SecureString that its SqlCredential keeps by reference.
    // SqlCredential stores that SecureString and is not IDisposable, and SqlConnection is sealed, so the
    // session is the only owner able to destroy the credential material deterministically when the
    // operator closes the connection. The plain text of the password is never exposed; the SecureString
    // instance itself stays reachable through SqlConnection.Credential.Password, as it was before.
    public sealed class Session : IDisposable, IAsyncDisposable
    {
        private readonly SqlConnection _connection;
        private readonly SecureString _password;

        internal Session(SqlConnection connection, SecureString password)
        {
            _connection = connection;
            _password = password;
        }

        public SqlConnection Connection => _connection;

        public void Dispose()
        {
            try { _connection.Dispose(); }
            finally { _password.Dispose(); }
        }

        public async ValueTask DisposeAsync()
        {
            try { await _connection.DisposeAsync(); }
            finally { _password.Dispose(); }
        }
    }

    public static Session Create(string database) => Create(database, NotInteractive, ReadKey);

    // Protected console read. The console state and the key source are seams so the operator flow can be
    // exercised without an interactive console; the prompt, the key flow and every message stay unchanged.
    internal static Session Create(string database, Func<bool> consoleRedirected, Func<ConsoleKeyInfo> readKey) =>
        Create(database, consoleRedirected, readKey, null);

    // The observer receives the typed buffer instance, after the keys have been read, so that its
    // deterministic destruction can be asserted. It is internal, the product never supplies one, and the
    // prompt, the key flow, the messages and the order of the checks stay exactly as they are.
    internal static Session Create(string database, Func<bool> consoleRedirected, Func<ConsoleKeyInfo> readKey, Action<SecureString>? observeTypedBuffer)
    {
        if (database is not (SqlTarget.DevelopmentDatabase or SqlTarget.TestDatabase)) throw new ArgumentException("Unauthorized database.");
        if (consoleRedirected()) throw new InvalidOperationException("A private interactive console is required.");
        using var password = new SecureString();
        Console.Write(PasswordPrompt);
        while (true)
        {
            var key = readKey();
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.RemoveAt(password.Length - 1); }
            else if (!char.IsControl(key.KeyChar)) password.AppendChar(key.KeyChar);
        }
        observeTypedBuffer?.Invoke(password);
        Console.WriteLine(); password.MakeReadOnly();
        if (password.Length == 0) throw new InvalidOperationException("Credential required.");
        return Create(database, password);
    }

    // Credential path of the operator tools. The supplied buffer belongs to the caller and is never
    // disposed here; the session owns the read-only copy that the SqlCredential keeps by reference.
    internal static Session Create(string database, SecureString password)
    {
        if (database is not (SqlTarget.DevelopmentDatabase or SqlTarget.TestDatabase)) throw new ArgumentException("Unauthorized database.");
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length == 0) throw new InvalidOperationException("Credential required.");
        // Lifecycle invariant: SqlCredential keeps the reference of the SecureString it receives and never
        // copies it, so the instance handed over must stay alive and readable for as long as the
        // SqlConnection that carries the credential. SecureString.Copy() returns a deep copy that is NOT
        // read-only and SqlCredential rejects a writable password, so the copy is marked read-only here.
        // The copy is owned by the session, which destroys it deterministically when the operator closes
        // the connection; if the session cannot be built, the copy is disposed so no sensitive buffer is
        // left behind.
        var credentialPassword = password.Copy();
        try
        {
            credentialPassword.MakeReadOnly();
            var credential = new SqlCredential("kdi_migrator", credentialPassword);
            var connection = new SqlConnection($"Server={SqlTarget.Server};Database={database};Encrypt=True;TrustServerCertificate=True;Persist Security Info=False", credential);
            return new Session(connection, credentialPassword);
        }
        catch
        {
            credentialPassword.Dispose();
            throw;
        }
    }

    private static bool NotInteractive() => Console.IsInputRedirected || Console.IsOutputRedirected || _notInteractiveForced;

    private static ConsoleKeyInfo ReadKey() => _notInteractiveForced
        ? throw new InvalidOperationException("The operator console is not interactive.")
        : Console.ReadKey(intercept: true);
}
