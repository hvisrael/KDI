using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kulani.KDI.Infrastructure.Modules.Portfolio.Persistence.Migrations;

[DbContext(typeof(PortfolioDbContext))]
[Migration("20261018000100_InitialPortfolio")]
public sealed class InitialPortfolio : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.EnsureSchema("portfolio");

    // Schemas are provisioned independently; rollback never drops a shared/preexisting schema.
    protected override void Down(MigrationBuilder migrationBuilder) { }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");
        modelBuilder.HasDefaultSchema("portfolio");
    }
}
