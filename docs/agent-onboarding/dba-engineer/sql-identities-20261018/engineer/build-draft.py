"""Generate review-only patch. Never writes product files."""
import difflib
from pathlib import Path

root = Path(__file__).resolve().parents[5]
changes = {}

path = 'src/Kulani.KDI.Infrastructure/Persistence/SqlTarget.cs'
old = (root / path).read_text(encoding='utf-8-sig')
new = old.replace('bool testOnly = false)', 'bool testOnly = false, SqlCredential? credential = null)')
new = new.replace('|| !value.IntegratedSecurity || value.UserID.Length != 0 || value.Password.Length != 0',
    '|| (credential is null ? !value.IntegratedSecurity : value.IntegratedSecurity || credential.UserId != "kdi_app")\n            || value.UserID.Length != 0 || value.Password.Length != 0')
new = new.replace('requires an authorized target and Windows integrated authentication.',
    'requires an authorized target and approved authentication mode.')
changes[path] = (old, new)

path = 'src/Kulani.KDI.Infrastructure/Persistence/PersistenceRegistration.cs'
old = (root / path).read_text(encoding='utf-8-sig')
new = 'using Microsoft.Data.SqlClient;\n' + old
new = new.replace('string? connectionString)', 'string? connectionString, SqlCredential? credential = null)')
new = new.replace('if (string.IsNullOrWhiteSpace(connectionString)) return services;',
    'if (string.IsNullOrWhiteSpace(connectionString))\n        {\n            if (credential is not null) throw new InvalidOperationException("SQL credential requires explicit target metadata.");\n            return services;\n        }')
new = new.replace('SqlTarget.Validate(connectionString)', 'SqlTarget.Validate(connectionString, credential: credential)')
for schema in ['identity', 'organizations', 'portfolio']:
    new = new.replace(f'validated, "{schema}", provider)', f'validated, "{schema}", provider, credential)')
start = new.index('    private static void ConfigureRuntime(')
end = new.index('    // Readiness', start)
new = new[:start] + '''    private static void ConfigureRuntime(DbContextOptionsBuilder options, string connection, string schema,
        IServiceProvider provider, SqlCredential? credential)
    {
        if (credential is null)
            options.UseSqlServer(connection, sql => sql.UseCompatibilityLevel(150).MigrationsHistoryTable("__EFMigrationsHistory", schema));
        else
            options.UseSqlServer(new SqlConnection(connection, credential), contextOwnsConnection: true,
                sql => sql.UseCompatibilityLevel(150).MigrationsHistoryTable("__EFMigrationsHistory", schema));
        options.AddInterceptors(provider.GetRequiredService<RuntimePrivilegeInterceptor>());
    }

''' + new[end:]
changes[path] = (old, new)

path = 'src/Kulani.KDI.Api/Program.cs'
old = (root / path).read_text(encoding='utf-8-sig')
new = 'using System.Security;\nusing Microsoft.Data.SqlClient;\n' + old
new = new.replace('var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddKdiPersistence(builder.Configuration.GetConnectionString("Kdi"));', '''const string credentialFlag = "--kdi-sql-console-credential";
var consoleCredential = args.Contains(credentialFlag, StringComparer.Ordinal);
using var password = consoleCredential ? ReadRuntimePassword() : null;
var credential = password is null ? null : new SqlCredential("kdi_app", password);
var builder = WebApplication.CreateBuilder(args.Where(value => value != credentialFlag).ToArray());
builder.Services.AddKdiPersistence(builder.Configuration.GetConnectionString("Kdi"), credential);''')
new = new.replace('public partial class Program;', '''static SecureString ReadRuntimePassword()
{
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
        throw new InvalidOperationException("Private local interactive console required.");
    var result = new SecureString();
    try
    {
        Console.Write("Password for kdi_app (not echoed): ");
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Escape) throw new InvalidOperationException("Credential entry cancelled.");
            if (key.Key == ConsoleKey.Backspace)
            {
                if (result.Length > 0) result.RemoveAt(result.Length - 1);
            }
            else if (!char.IsControl(key.KeyChar)) result.AppendChar(key.KeyChar);
        }
        Console.WriteLine();
        if (result.Length == 0) throw new InvalidOperationException("Credential required.");
        result.MakeReadOnly();
        return result;
    }
    catch { result.Dispose(); throw; }
}

public partial class Program;''')
changes[path] = (old, new)

path = 'tests/Kulani.KDI.UnitTests/SqlCredentialTargetTests.cs'
new = '''using System.Security;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Kulani.KDI.UnitTests;

public sealed class SqlCredentialTargetTests
{
    private const string Target = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=false;Encrypt=true";

    [Fact]
    public void Runtime_credential_accepts_metadata_without_embedded_identity_or_password()
    {
        using var secret = new SecureString();
        secret.AppendChar('x'); secret.MakeReadOnly();
        var credential = new SqlCredential("kdi_app", secret);
        var value = new SqlConnectionStringBuilder(SqlTarget.Validate(Target, credential: credential));
        Assert.False(value.IntegratedSecurity);
        Assert.Empty(value.UserID);
        Assert.Empty(value.Password);
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(Target));
    }

    [Theory]
    [InlineData("kdi_migrator", "")]
    [InlineData("kdi_test_runner", "")]
    [InlineData("kdi_app", ";Integrated Security=true")]
    [InlineData("kdi_app", ";User ID=kdi_app")]
    [InlineData("kdi_app", ";Password=synthetic")]
    [InlineData("kdi_app", ";Persist Security Info=true")]
    [InlineData("kdi_app", ";Encrypt=false")]
    [InlineData("kdi_app", ";Database=master")]
    [InlineData("kdi_app", ";Server=other")]
    [InlineData("kdi_app", ";Failover Partner=other")]
    public void Runtime_credential_rejects_wrong_identity_or_unsafe_metadata(string user, string extra)
    {
        using var secret = new SecureString();
        secret.AppendChar('x'); secret.MakeReadOnly();
        var credential = new SqlCredential(user, secret);
        var error = Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(Target + extra, credential: credential));
        Assert.DoesNotContain("synthetic", error.Message);
    }
}
'''
changes[path] = ('', new)

patch = ''.join(''.join(difflib.unified_diff(old.splitlines(True), new.splitlines(True),
    fromfile='a/' + path if old else '/dev/null', tofile='b/' + path)) for path, (old, new) in changes.items())
Path(__file__).with_name('sql-runtime-credential.patch').write_text(patch, encoding='utf-8')
print('Prepared review-only patch; product files unchanged.')
