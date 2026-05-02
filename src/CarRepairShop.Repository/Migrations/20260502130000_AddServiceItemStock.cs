using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRepairShop.Repository.Migrations;

[DbContext(typeof(CarRepairShopDbContext))]
[Migration("20260502130000_AddServiceItemStock")]
public partial class AddServiceItemStock : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Stock",
            table: "ServiceItems",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Stock",
            table: "ServiceItems");
    }
}
