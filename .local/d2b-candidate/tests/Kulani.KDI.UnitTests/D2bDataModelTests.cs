using Kulani.KDI.Infrastructure.Modules.Identity.Persistence;
using Kulani.KDI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kulani.KDI.UnitTests;

public sealed class D2bDataModelTests
{
    private const string Connection = "Server=DESKTOP-F25O3MF;Database=KulaniKDI_Test;Integrated Security=true;Encrypt=true";
    [Fact]
    public void Exact_identity_and_active_grants_have_database_constraints()
    {
        using var context = MigrationContexts.Create("identity", Connection);
        var user = context.Model.FindEntityType(typeof(AccessUserEntity))!;
        Assert.Equal("Latin1_General_100_BIN2", context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.FindEntityType(typeof(AccessUserEntity))!.FindProperty("Issuer")!.GetCollation());
        Assert.Contains(user.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] { "Issuer", "Subject", "IssuerBytes", "SubjectBytes" }));
        var grant = context.Model.FindEntityType(typeof(GrantEntity))!;
        Assert.Contains(grant.GetIndexes(), x => x.IsUnique && x.GetFilter() == "[RevokedAtUtc] IS NULL");
        Assert.All(context.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()), fk => Assert.Equal("identity", fk.PrincipalEntityType.GetSchema()));
    }
    [Theory]
    [InlineData("identity")]
    [InlineData("organizations")]
    public void Additive_migration_script_is_idempotent_and_model_matches_snapshot(string module)
    {
        using var context = MigrationContexts.Create(module, Connection);
        Assert.False(context.Database.HasPendingModelChanges());
        var sql = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE", sql); Assert.Contains("IF NOT EXISTS", sql);
        Assert.DoesNotContain("DROP TABLE", sql); Assert.DoesNotContain("GRANT ", sql); Assert.DoesNotContain("[portfolio]", sql);
    }
    [Fact]
    public void Permission_catalog_is_fixed_and_does_not_grant_global_administration()
    {
        Assert.Equal(8, IdentityDbContext.Permissions.Length);
        Assert.Equal(8, IdentityDbContext.Permissions.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("sysadmin", IdentityDbContext.Permissions);
    }
}
