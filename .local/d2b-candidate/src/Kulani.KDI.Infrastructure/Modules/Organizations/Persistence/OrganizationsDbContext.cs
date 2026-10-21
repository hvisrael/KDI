using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;

public sealed class OrganizationsDbContext(DbContextOptions<OrganizationsDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<MembershipEntity> Memberships => Set<MembershipEntity>();
    public DbSet<OrganizationAuditEntity> AuditEntries => Set<OrganizationAuditEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);
    internal static void ConfigureModel(ModelBuilder b)
    {
        b.HasDefaultSchema("organizations");
        var o = b.Entity<OrganizationEntity>(); o.ToTable("Organizations", t => t.HasCheckConstraint("CK_Organizations_Code", "[Code] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9_-]%' AND DATALENGTH([Code])>0")); o.HasKey(x => x.Id); o.Property(x => x.Code).HasMaxLength(32).IsUnicode(false).UseCollation("Latin1_General_100_BIN2"); o.Property(x => x.Name).HasMaxLength(200); o.HasIndex(x => x.Code).IsUnique(); o.Property(x => x.Version).IsRowVersion();
        var m = b.Entity<MembershipEntity>(); m.ToTable("OrganizationMemberships"); m.HasKey(x => x.Id); m.Property(x => x.Version).IsRowVersion(); m.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique(); m.HasIndex(x => new { x.UserId, x.IsActive }); m.HasOne<OrganizationEntity>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        var a = b.Entity<OrganizationAuditEntity>(); a.ToTable("AuditEntries"); a.HasKey(x => x.Id); a.Property(x => x.ActorKind).HasMaxLength(16); a.Property(x => x.ActorId).HasMaxLength(184); a.Property(x => x.Action).HasMaxLength(64); a.Property(x => x.CorrelationId).HasMaxLength(64); a.HasIndex(x => new { x.OrganizationId, x.AtUtc });
        foreach (var entity in b.Model.GetEntityTypes()) foreach (var property in entity.GetProperties())
            if (property.ClrType == typeof(DateTime)) property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
    }
}
