using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kulani.KDI.Infrastructure.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Accessidentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(184)", maxLength: 184, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionCatalog",
                schema: "identity",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionCatalog", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    IssuerBytes = table.Column<int>(type: "int", nullable: false, computedColumnSql: "DATALENGTH([Issuer])", stored: true),
                    SubjectBytes = table.Column<int>(type: "int", nullable: false, computedColumnSql: "DATALENGTH([Subject])", stored: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Grants",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grants", x => x.Id);
                    table.CheckConstraint("CK_Grants_Scope", "([ScopeType]='Organization' AND [ScopeId]=[OrganizationId]) OR ([ScopeType]='Project' AND [PermissionCode]='projects.read')");
                    table.ForeignKey(
                        name: "FK_Grants_PermissionCatalog_PermissionCode",
                        column: x => x.PermissionCode,
                        principalSchema: "identity",
                        principalTable: "PermissionCatalog",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grants_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandleHash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProtectedTicket = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AbsoluteExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdleExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.CheckConstraint("CK_Sessions_Times", "[CreatedAtUtc]<=[LastSeenAtUtc] AND [LastSeenAtUtc]<=[IdleExpiresAtUtc] AND [IdleExpiresAtUtc]<=[AbsoluteExpiresAtUtc] AND ([RevokedAtUtc] IS NULL OR [ProtectedTicket] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "PermissionCatalog",
                column: "Code",
                values: new object[]
                {
                    "access.manage",
                    "audit.read",
                    "clients.manage",
                    "clients.read",
                    "members.manage",
                    "members.read",
                    "projects.manage",
                    "projects.read"
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OrganizationId_AtUtc",
                schema: "identity",
                table: "AuditEntries",
                columns: new[] { "OrganizationId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Grants_PermissionCode",
                schema: "identity",
                table: "Grants",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_Grants_UserId_OrganizationId_PermissionCode_ScopeType_ScopeId",
                schema: "identity",
                table: "Grants",
                columns: new[] { "UserId", "OrganizationId", "PermissionCode", "ScopeType", "ScopeId" },
                unique: true,
                filter: "[RevokedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HandleHash",
                schema: "identity",
                table: "Sessions",
                column: "HandleHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_AbsoluteExpiresAtUtc",
                schema: "identity",
                table: "Sessions",
                columns: new[] { "UserId", "AbsoluteExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Issuer_Subject_IssuerBytes_SubjectBytes",
                schema: "identity",
                table: "Users",
                columns: new[] { "Issuer", "Subject", "IssuerBytes", "SubjectBytes" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Grants",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "PermissionCatalog",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");
        }
    }
}
