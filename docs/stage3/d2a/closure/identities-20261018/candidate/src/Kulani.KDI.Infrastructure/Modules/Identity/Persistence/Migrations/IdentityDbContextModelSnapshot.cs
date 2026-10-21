using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kulani.KDI.Infrastructure.Modules.Identity.Persistence.Migrations;

[DbContext(typeof(IdentityDbContext))]
public sealed class IdentityDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
        modelBuilder.HasDefaultSchema("identity");
    }
}
