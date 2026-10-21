using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.Infrastructure.Modules.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<AccessUserEntity> Users => Set<AccessUserEntity>();
    public DbSet<PermissionEntity> PermissionCatalog => Set<PermissionEntity>();
    public DbSet<GrantEntity> Grants => Set<GrantEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<IdentityAuditEntity> AuditEntries => Set<IdentityAuditEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);
    internal static void ConfigureModel(ModelBuilder b)
    {
        b.HasDefaultSchema("identity");
        var u = b.Entity<AccessUserEntity>(); u.ToTable("Users"); u.HasKey(x => x.Id);
        u.Property(x => x.Issuer).HasMaxLength(512).UseCollation("Latin1_General_100_BIN2");
        u.Property(x => x.Subject).HasMaxLength(255).UseCollation("Latin1_General_100_BIN2");
        u.Property(x => x.IssuerBytes).HasComputedColumnSql("DATALENGTH([Issuer])", stored: true);
        u.Property(x => x.SubjectBytes).HasComputedColumnSql("DATALENGTH([Subject])", stored: true);
        u.HasIndex(x => new { x.Issuer, x.Subject, x.IssuerBytes, x.SubjectBytes }).IsUnique();
        u.Property(x => x.DisplayName).HasMaxLength(200); u.Property(x => x.Version).IsRowVersion();
        var p = b.Entity<PermissionEntity>(); p.ToTable("PermissionCatalog"); p.HasKey(x => x.Code); p.Property(x => x.Code).HasMaxLength(40);
        p.HasData(Permissions.Select(code => new PermissionEntity { Code = code }));
        var g = b.Entity<GrantEntity>(); g.ToTable("Grants", t => t.HasCheckConstraint("CK_Grants_Scope", "([ScopeType]='Organization' AND [ScopeId]=[OrganizationId]) OR ([ScopeType]='Project' AND [PermissionCode]='projects.read')")); g.HasKey(x => x.Id);
        g.Property(x => x.PermissionCode).HasMaxLength(40); g.Property(x => x.ScopeType).HasMaxLength(16); g.Property(x => x.Version).IsRowVersion();
        g.HasOne<AccessUserEntity>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        g.HasOne<PermissionEntity>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
        g.HasIndex(x => new { x.UserId, x.OrganizationId, x.PermissionCode, x.ScopeType, x.ScopeId }).IsUnique().HasFilter("[RevokedAtUtc] IS NULL");
        var s = b.Entity<SessionEntity>(); s.ToTable("Sessions", t => t.HasCheckConstraint("CK_Sessions_Times", "[CreatedAtUtc]<=[LastSeenAtUtc] AND [LastSeenAtUtc]<=[IdleExpiresAtUtc] AND [IdleExpiresAtUtc]<=[AbsoluteExpiresAtUtc] AND ([RevokedAtUtc] IS NULL OR [ProtectedTicket] IS NULL)")); s.HasKey(x => x.Id);
        s.Property(x => x.HandleHash).HasMaxLength(32).IsFixedLength(); s.HasIndex(x => x.HandleHash).IsUnique(); s.HasIndex(x => new { x.UserId, x.AbsoluteExpiresAtUtc }); s.Property(x => x.Version).IsRowVersion();
        s.HasOne<AccessUserEntity>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var a = b.Entity<IdentityAuditEntity>(); a.ToTable("AuditEntries"); a.HasKey(x => x.Id); a.Property(x => x.ActorKind).HasMaxLength(16); a.Property(x => x.ActorId).HasMaxLength(184); a.Property(x => x.Action).HasMaxLength(64); a.Property(x => x.CorrelationId).HasMaxLength(64); a.HasIndex(x => new { x.OrganizationId, x.AtUtc });
        ConfigureUtc(b);
    }
    public static readonly string[] Permissions = ["members.read", "members.manage", "access.manage", "audit.read", "clients.read", "clients.manage", "projects.read", "projects.manage"];
    internal static void ConfigureUtc(ModelBuilder b)
    {
        foreach (var e in b.Model.GetEntityTypes()) foreach (var p in e.GetProperties())
            if (p.ClrType == typeof(DateTime)) p.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
    }
}
