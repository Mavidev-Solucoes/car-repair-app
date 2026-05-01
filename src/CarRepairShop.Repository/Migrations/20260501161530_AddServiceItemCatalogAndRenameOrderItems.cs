using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRepairShop.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceItemCatalogAndRenameOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceItems_ServiceOrders_ServiceOrderId",
                table: "ServiceItems");

            migrationBuilder.DropIndex(
                name: "IX_ServiceItems_ServiceOrderId",
                table: "ServiceItems");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ServiceItems");

            migrationBuilder.DropColumn(
                name: "ServiceOrderId",
                table: "ServiceItems");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "ServiceItems",
                newName: "UnitCost");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceItems",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ServiceItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ServiceOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderItems_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderItems_ServiceOrderId",
                table: "ServiceOrderItems",
                column: "ServiceOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceOrderItems");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ServiceItems");

            migrationBuilder.RenameColumn(
                name: "UnitCost",
                table: "ServiceItems",
                newName: "Quantity");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ServiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOrderId",
                table: "ServiceItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_ServiceOrderId",
                table: "ServiceItems",
                column: "ServiceOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceItems_ServiceOrders_ServiceOrderId",
                table: "ServiceItems",
                column: "ServiceOrderId",
                principalTable: "ServiceOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
