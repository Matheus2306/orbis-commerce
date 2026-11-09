using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orbis.Inventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "stock_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_stock_items_quantities",
                table: "stock_items",
                sql: "\"Quantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"Quantity\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_stock_items_quantities",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "stock_items");
        }
    }
}
