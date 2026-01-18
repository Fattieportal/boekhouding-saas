using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boekhouding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Klanten",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "FactuurRegels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Facturen",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KvK = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTenants",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenants", x => new { x.UserId, x.TenantId });
                    table.ForeignKey(
                        name: "FK_UserTenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTenants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Klanten_TenantId",
                table: "Klanten",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FactuurRegels_TenantId",
                table: "FactuurRegels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturen_TenantId",
                table: "Facturen",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_KvK",
                table: "Tenants",
                column: "KvK");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenants_TenantId",
                table: "UserTenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenants_UserId",
                table: "UserTenants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturen_Tenants_TenantId",
                table: "Facturen",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FactuurRegels_Tenants_TenantId",
                table: "FactuurRegels",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Klanten_Tenants_TenantId",
                table: "Klanten",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturen_Tenants_TenantId",
                table: "Facturen");

            migrationBuilder.DropForeignKey(
                name: "FK_FactuurRegels_Tenants_TenantId",
                table: "FactuurRegels");

            migrationBuilder.DropForeignKey(
                name: "FK_Klanten_Tenants_TenantId",
                table: "Klanten");

            migrationBuilder.DropTable(
                name: "UserTenants");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Klanten_TenantId",
                table: "Klanten");

            migrationBuilder.DropIndex(
                name: "IX_FactuurRegels_TenantId",
                table: "FactuurRegels");

            migrationBuilder.DropIndex(
                name: "IX_Facturen_TenantId",
                table: "Facturen");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Klanten");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FactuurRegels");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Facturen");
        }
    }
}
