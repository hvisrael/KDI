using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.Infrastructure.Modules.Organizations.Persistence;

// D2-A baseline intentionally has no business entities; those belong to D2-B/C.
public sealed class OrganizationsDbContext(DbContextOptions<OrganizationsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("organizations");
}
