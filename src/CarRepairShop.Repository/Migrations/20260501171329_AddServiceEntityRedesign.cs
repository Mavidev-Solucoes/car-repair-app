using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRepairShop.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceEntityRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ServiceOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "ServiceOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "ServiceOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOrderId",
                table: "ServiceJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ServiceStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStatusHistory_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_AssignedUserId",
                table: "ServiceOrders",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CustomerId",
                table: "ServiceOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobs_ServiceOrderId",
                table: "ServiceJobs",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatusHistory_ServiceOrderId",
                table: "ServiceStatusHistory",
                column: "ServiceOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobs_ServiceOrders_ServiceOrderId",
                table: "ServiceJobs",
                column: "ServiceOrderId",
                principalTable: "ServiceOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Customers_CustomerId",
                table: "ServiceOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Users_AssignedUserId",
                table: "ServiceOrders",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobs_ServiceOrders_ServiceOrderId",
                table: "ServiceJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Customers_CustomerId",
                table: "ServiceOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Users_AssignedUserId",
                table: "ServiceOrders");

            migrationBuilder.DropTable(
                name: "ServiceStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_AssignedUserId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_CustomerId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobs_ServiceOrderId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "ServiceOrderId",
                table: "ServiceJobs");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ServiceOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ServiceOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ServiceOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
