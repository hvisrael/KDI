using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence.Migrations;

[DbContext(typeof(PortfolioDbContext))]
public sealed class PortfolioDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
        modelBuilder.HasDefaultSchema("portfolio");
    }
}
