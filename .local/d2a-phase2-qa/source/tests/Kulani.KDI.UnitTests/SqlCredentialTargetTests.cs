using System.Security;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;
using Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence;

namespace Kulani.KDI.UnitTests;

public sealed class SqlCredentialTargetTests
{
    private const string Target = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=false;Encrypt=true";

    [Fact]
    public void Runtime_credential_requires_explicit_target_and_test_only_stays_enforced()
    {
        using var secret = new SecureString();
        secret.AppendChar('x'); secret.MakeReadOnly();
        var credential = new SqlCredential("kdi_app", secret);
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddKdiPersistence(null, credential));
        Assert.Throws<InvalidOperationException>(() => SqlTarget.Validate(Target.Replace("KulaniKDI_Test", "KulaniKDI_Dev"), testOnly: true, credential: credential));
    }

    [Fact]
    public void Every_context_owns_its_distinct_closed_connection_with_shared_runtime_credential()
    {
        using var secret = new SecureString();
        secret.AppendChar('x'); secret.MakeReadOnly();
        var credential = new SqlCredential("kdi_app", secret);
        using var provider = new ServiceCollection().AddKdiPersistence(Target, credential).BuildServiceProvider();
        using var scope = provider.CreateScope();
        DbContext[] contexts = [scope.ServiceProvider.GetRequiredService<IdentityDbContext>(),
            scope.ServiceProvider.GetRequiredService<OrganizationsDbContext>(), scope.ServiceProvider.GetRequiredService<PortfolioDbContext>()];
        var connections = contexts.Select(context => Assert.IsType<SqlConnection>(context.Database.GetDbConnection())).ToArray();
        Assert.Equal(3, connections.Distinct().Count());
        foreach (var context in contexts)
        {
            var connection = Assert.IsType<SqlConnection>(context.Database.GetDbConnection());
            Assert.Same(credential, connection.Credential);
            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
            var options = context.GetService<IDbContextOptions>();
            Assert.True(options.Extensions.OfType<RelationalOptionsExtension>().Single().IsConnectionOwned);
        }
    }

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
