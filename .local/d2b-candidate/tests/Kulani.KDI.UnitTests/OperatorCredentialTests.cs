using System.Reflection;
using System.Security;
using Kulani.KDI.Infrastructure.Access;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.UnitTests;

// The operator credential tests capture console output, so they never run in parallel with other tests.
[CollectionDefinition("KdiOperatorConsole", DisableParallelization = true)]
public sealed class KdiOperatorConsoleCollection;

[Collection("KdiOperatorConsole")]
public sealed class OperatorCredentialTests
{
    private const string SyntheticSecret = "synthetic-operator-secret";

    private static SecureString Synthetic()
    {
        var secret = new SecureString();
        foreach (var character in SyntheticSecret) secret.AppendChar(character);
        secret.MakeReadOnly();
        return secret;
    }

    private static ConsoleKeyInfo Key(char character, ConsoleKey key = ConsoleKey.A) =>
        new(character, key, false, false, false);

    private static string Capture(Action action)
    {
        var original = Console.Out;
        var writer = new StringWriter();
        try { Console.SetOut(writer); action(); return writer.ToString(); }
        finally { Console.SetOut(original); }
    }

    [Fact]
    public void Operator_credential_is_built_from_a_read_only_copy_and_never_throws()
    {
        using var secret = Synthetic();
        using var session = OperatorConnection.Create(SqlTarget.TestDatabase, secret);
        var credential = session.Connection.Credential;
        Assert.NotNull(credential);
        Assert.Equal("kdi_migrator", credential!.UserId);
        var handed = credential.Password;
        Assert.NotNull(handed);
        Assert.NotSame(secret, handed);
        Assert.True(handed!.IsReadOnly());
        Assert.Equal(SyntheticSecret.Length, handed.Length);
    }

    [Fact]
    public void Operator_credential_copy_survives_the_disposal_of_the_typed_buffer()
    {
        using var secret = Synthetic();
        using var session = OperatorConnection.Create(SqlTarget.TestDatabase, secret);
        secret.Dispose();
        var handed = session.Connection.Credential!.Password;
        Assert.NotNull(handed);
        Assert.True(handed!.IsReadOnly());
        Assert.Equal(SyntheticSecret.Length, handed.Length);
    }

    [Fact]
    public void Operator_credential_keeps_the_authorized_target_and_opens_no_connection()
    {
        using var secret = Synthetic();
        using var session = OperatorConnection.Create(SqlTarget.TestDatabase, secret);
        var connection = session.Connection;
        var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
        Assert.Equal(SqlTarget.Server, builder.DataSource);
        Assert.Equal(SqlTarget.TestDatabase, builder.InitialCatalog);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, builder.Encrypt);
        Assert.False(builder.PersistSecurityInfo);
        Assert.Empty(builder.UserID);
        Assert.Empty(builder.Password);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    [Fact]
    public void Operator_credential_still_rejects_an_unauthorized_database()
    {
        using var secret = Synthetic();
        Assert.Throws<ArgumentException>(() => OperatorConnection.Create("KulaniKDI_Prod", secret));
    }

    [Fact]
    public void Operator_session_destroys_the_credential_password_when_disposed()
    {
        using var secret = Synthetic();
        var session = OperatorConnection.Create(SqlTarget.TestDatabase, secret);
        var handed = session.Connection.Credential!.Password!;
        Assert.Equal(SyntheticSecret.Length, handed.Length);
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { _ = handed.Length; });
    }

    [Fact]
    public async Task Operator_session_destroys_the_credential_password_when_disposed_asynchronously()
    {
        using var secret = Synthetic();
        var session = OperatorConnection.Create(SqlTarget.TestDatabase, secret);
        var handed = session.Connection.Credential!.Password!;
        Assert.Equal(SyntheticSecret.Length, handed.Length);
        await session.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => { _ = handed.Length; });
    }

    [Fact]
    public void Operator_console_guard_refuses_a_redirected_console()
    {
        var error = Assert.Throws<InvalidOperationException>(() => OperatorConnection.Create(SqlTarget.TestDatabase, () => true, () => throw new InvalidOperationException("The keyboard must not be read.")));
        Assert.Equal("A private interactive console is required.", error.Message);
    }

    [Fact]
    public void Operator_console_guard_still_rejects_an_unauthorized_database_first()
    {
        var error = Assert.Throws<ArgumentException>(() => OperatorConnection.Create("KulaniKDI_Prod", () => true, () => throw new InvalidOperationException("The keyboard must not be read.")));
        Assert.Equal("Unauthorized database.", error.Message);
    }

    [Fact]
    public void Operator_protected_read_keeps_the_prompt_and_the_key_flow()
    {
        Assert.Equal("kdi_migrator password (protected input): ", OperatorConnection.PasswordPrompt);
        var keys = new Queue<ConsoleKeyInfo>([Key('a'), Key('b'), Key('c'), Key('\b', ConsoleKey.Backspace), Key('d'), Key('\x01'), Key('\r', ConsoleKey.Enter)]);
        var captured = Capture(() =>
        {
            using var session = OperatorConnection.Create(SqlTarget.TestDatabase, () => false, () => keys.Dequeue());
            var handed = session.Connection.Credential!.Password!;
            Assert.True(handed.IsReadOnly());
            Assert.Equal(3, handed.Length);
        });
        Assert.Contains(OperatorConnection.PasswordPrompt, captured);
    }

    [Fact]
    public void Operator_protected_read_rejects_an_empty_password()
    {
        InvalidOperationException? error = null;
        Capture(() => error = Assert.Throws<InvalidOperationException>(() => OperatorConnection.Create(SqlTarget.TestDatabase, () => false, () => Key('\r', ConsoleKey.Enter))));
        Assert.Equal("Credential required.", error!.Message);
    }

    [Fact]
    public void Operator_empty_credential_buffer_is_rejected()
    {
        using var empty = new SecureString();
        empty.MakeReadOnly();
        var error = Assert.Throws<InvalidOperationException>(() => OperatorConnection.Create(SqlTarget.TestDatabase, empty));
        Assert.Equal("Credential required.", error.Message);
    }

    [Fact]
    public void Operator_public_entry_point_refuses_to_build_a_credential_without_a_private_console()
    {
        // The public entry point is the one the operator tools use. The forced state makes the console
        // facts deterministic, whatever the handles of the test process are, and it can only make the
        // operator path stricter, so the keyboard is never read.
        OperatorConnection.ForceNotInteractive(true);
        try
        {
            var error = Assert.Throws<InvalidOperationException>(() => OperatorConnection.Create(SqlTarget.TestDatabase));
            Assert.Equal("A private interactive console is required.", error.Message);
        }
        finally { OperatorConnection.ForceNotInteractive(false); }
    }

    [Fact]
    public void Operator_typed_buffer_is_destroyed_when_the_protected_read_finishes()
    {
        SecureString? typed = null;
        var observed = -1;
        var keys = new Queue<ConsoleKeyInfo>([Key('a'), Key('b'), Key('c'), Key('\r', ConsoleKey.Enter)]);
        Capture(() =>
        {
            using var session = OperatorConnection.Create(SqlTarget.TestDatabase, () => false, () => keys.Dequeue(), buffer =>
            {
                typed = buffer;
                observed = buffer.Length;
            });
            Assert.NotNull(typed);
            Assert.Equal(3, observed);
            Assert.Throws<ObjectDisposedException>(() => { _ = typed!.Length; });
        });
    }

    [Fact]
    public void Operator_public_surface_exposes_only_the_interactive_entry_point()
    {
        var publicCreates = typeof(OperatorConnection).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "Create").ToArray();
        Assert.Single(publicCreates);
        Assert.Equal(typeof(OperatorConnection.Session), publicCreates[0].ReturnType);
        var credentialOverloads = typeof(OperatorConnection).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name == "Create" && method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType == typeof(SecureString)).ToArray();
        Assert.Single(credentialOverloads);
        Assert.True(credentialOverloads[0].IsAssembly);
        Assert.False(credentialOverloads[0].IsPublic);
    }
}
