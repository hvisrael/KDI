using Microsoft.EntityFrameworkCore;

namespace Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence;

// D2-A baseline intentionally has no business entities; those belong to D2-B/C.
public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("portfolio");
}
