using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boekhouding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAmountToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OpenAmount",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
            
            // Initialize OpenAmount = Total for all existing unpaid invoices
            migrationBuilder.Sql(@"
                UPDATE ""SalesInvoices""
                SET ""OpenAmount"" = ""Total""
                WHERE ""Status"" IN (0, 1, 2) -- Draft, Sent, Posted (not yet Paid)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenAmount",
                table: "SalesInvoices");
        }
    }
}
