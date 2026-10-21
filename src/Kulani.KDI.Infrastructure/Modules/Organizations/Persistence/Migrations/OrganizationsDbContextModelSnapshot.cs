using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kulani.KDI.Infrastructure.Modules.Organizations.Persistence.Migrations;

[DbContext(typeof(OrganizationsDbContext))]
public sealed class OrganizationsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
        modelBuilder.HasDefaultSchema("organizations");
    }
}
