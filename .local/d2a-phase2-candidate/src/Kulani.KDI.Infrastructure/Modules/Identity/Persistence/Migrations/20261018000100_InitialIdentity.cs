using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kulani.KDI.Infrastructure.Modules.Identity.Persistence.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20261018000100_InitialIdentity")]
public sealed class InitialIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.EnsureSchema("identity");

    // Schemas are provisioned independently; rollback never drops a shared/preexisting schema.
    protected override void Down(MigrationBuilder migrationBuilder) { }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
        modelBuilder.HasDefaultSchema("identity");
    }
}
