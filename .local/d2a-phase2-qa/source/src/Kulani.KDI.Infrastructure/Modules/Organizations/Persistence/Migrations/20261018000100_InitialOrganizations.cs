using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kulani.KDI.Infrastructure.Modules.Organizations.Persistence.Migrations;

[DbContext(typeof(OrganizationsDbContext))]
[Migration("20261018000100_InitialOrganizations")]
public sealed class InitialOrganizations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.EnsureSchema("organizations");

    // Schemas are provisioned independently; rollback never drops a shared/preexisting schema.
    protected override void Down(MigrationBuilder migrationBuilder) { }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
        modelBuilder.HasDefaultSchema("organizations");
    }
}
