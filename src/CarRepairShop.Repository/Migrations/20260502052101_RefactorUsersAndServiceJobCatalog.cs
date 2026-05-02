using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRepairShop.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUsersAndServiceJobCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string defaultCustomerPasswordHash = "$2y$12$aR.mHqRyx33yA.34Xa1fHezPuMPNSps9lIJe4F/Yd4jV.kqFH7nsS";

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobs_ServiceOrders_ServiceOrderId",
                table: "ServiceJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobs_Users_AssignedUserId",
                table: "ServiceJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Customers_CustomerId",
                table: "ServiceOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Customers_CustomerId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobs_AssignedUserId",
                table: "ServiceJobs");

            migrationBuilder.DropIndex(
                name: "IX_ServiceJobs_ServiceOrderId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "PersonalId",
                table: "Users",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Users",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserKind",
                table: "Users",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "Employee");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceItemId",
                table: "ServiceOrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ServiceJobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ServiceItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Users] SET [UserKind] = 'Employee' WHERE [UserKind] IS NULL OR [UserKind] = '';");

            migrationBuilder.Sql($@"
WITH [CustomersWithResolvedEmail] AS
(
    SELECT
        [c].[Id],
        [c].[Name],
        [c].[Email],
        [c].[CreatedAt],
        [c].[UpdatedAt],
        [c].[CreatedUserId],
        [c].[LastUpdatedUserId],
        [c].[PersonalId],
        [c].[Telephone],
        ROW_NUMBER() OVER (PARTITION BY [c].[Email] ORDER BY [c].[CreatedAt], [c].[Id]) AS [CustomerEmailRank]
    FROM [Customers] AS [c]
)
INSERT INTO [Users] ([Id], [Name], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt], [UpdatedAt], [CreatedUserId], [LastUpdatedUserId], [PersonalId], [Telephone], [UserKind])
SELECT
    [c].[Id],
    [c].[Name],
    CASE
        WHEN [c].[CustomerEmailRank] > 1
            OR EXISTS (SELECT 1 FROM [Users] AS [u] WHERE [u].[Email] = [c].[Email])
        THEN CONCAT('migrated-customer-', CONVERT(varchar(36), [c].[Id]), '@local.invalid')
        ELSE [c].[Email]
    END,
    '{defaultCustomerPasswordHash}',
    4,
    1,
    [c].[CreatedAt],
    [c].[UpdatedAt],
    [c].[CreatedUserId],
    [c].[LastUpdatedUserId],
    [c].[PersonalId],
    [c].[Telephone],
    'Customer'
FROM [CustomersWithResolvedEmail] AS [c];");

            migrationBuilder.Sql(@"
UPDATE [ServiceItems]
SET [Price] = CONVERT(decimal(18,2), [UnitCost])
WHERE [Price] IS NULL;

UPDATE [ServiceJobs]
SET [Price] = CONVERT(decimal(18,2), [UnitCost])
WHERE [Price] IS NULL;");

            migrationBuilder.CreateTable(
                name: "ServiceOrderJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderJobs_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOrderJobs_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceOrderJobs_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.Sql(@"
INSERT INTO [ServiceOrderJobs] ([Id], [ServiceOrderId], [ServiceJobId], [Name], [Description], [Price], [Status], [AssignedUserId], [CreatedAt], [UpdatedAt], [CreatedUserId], [LastUpdatedUserId])
SELECT
    [sj].[Id],
    [sj].[ServiceOrderId],
    [sj].[Id],
    [sj].[Name],
    [sj].[Description],
    [sj].[Price],
    [sj].[Status],
    [sj].[AssignedUserId],
    [sj].[CreatedAt],
    [sj].[UpdatedAt],
    [sj].[CreatedUserId],
    [sj].[LastUpdatedUserId]
FROM [ServiceJobs] AS [sj]
INNER JOIN [ServiceOrders] AS [so] ON [so].[Id] = [sj].[ServiceOrderId];");

            migrationBuilder.CreateTable(
                name: "ServiceOrderJobStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOrderJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ServiceOrderJobStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderJobStatusHistory_ServiceOrderJobs_ServiceOrderJobId",
                        column: x => x.ServiceOrderJobId,
                        principalTable: "ServiceOrderJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
INSERT INTO [ServiceOrderJobStatusHistory] ([Id], [ServiceOrderJobId], [FromStatus], [ToStatus], [ChangedAt], [ChangedByUserId], [CreatedAt], [UpdatedAt], [CreatedUserId], [LastUpdatedUserId])
SELECT
    [h].[Id],
    [h].[ServiceJobId],
    [h].[FromStatus],
    [h].[ToStatus],
    [h].[ChangedAt],
    [h].[ChangedByUserId],
    [h].[CreatedAt],
    [h].[UpdatedAt],
    [h].[CreatedUserId],
    [h].[LastUpdatedUserId]
FROM [ServiceJobStatusHistory] AS [h]
INNER JOIN [ServiceOrderJobs] AS [oj] ON [oj].[Id] = [h].[ServiceJobId];");

            migrationBuilder.Sql(@"
UPDATE [soi]
SET [ServiceItemId] = [matched].[Id]
FROM [ServiceOrderItems] AS [soi]
CROSS APPLY (
    SELECT TOP (1) [si].[Id]
    FROM [ServiceItems] AS [si]
    WHERE [si].[Description] = [soi].[Description]
      AND [si].[Price] = [soi].[Price]
    ORDER BY [si].[CreatedAt], [si].[Id]
) AS [matched]
WHERE [soi].[ServiceItemId] IS NULL;

CREATE TABLE #SyntheticServiceItems
(
    [Description] nvarchar(200) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [ServiceItemId] uniqueidentifier NOT NULL
);

INSERT INTO #SyntheticServiceItems ([Description], [Price], [ServiceItemId])
SELECT DISTINCT
    [soi].[Description],
    [soi].[Price],
    NEWID()
FROM [ServiceOrderItems] AS [soi]
WHERE [soi].[ServiceItemId] IS NULL;

INSERT INTO [ServiceItems] ([Id], [Name], [Description], [UnitCost], [Price], [CreatedAt], [UpdatedAt], [CreatedUserId], [LastUpdatedUserId])
SELECT
    [s].[ServiceItemId],
    LEFT([s].[Description], 100),
    LEFT([s].[Description], 400),
    CONVERT(int, ROUND([s].[Price], 0)),
    [s].[Price],
    SYSUTCDATETIME(),
    NULL,
    NULL,
    NULL
FROM #SyntheticServiceItems AS [s];

UPDATE [soi]
SET [ServiceItemId] = [s].[ServiceItemId]
FROM [ServiceOrderItems] AS [soi]
INNER JOIN #SyntheticServiceItems AS [s]
    ON [s].[Description] = [soi].[Description]
   AND [s].[Price] = [soi].[Price]
WHERE [soi].[ServiceItemId] IS NULL;

DROP TABLE #SyntheticServiceItems;");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "ServiceJobStatusHistory");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceItemId",
                table: "ServiceOrderItems",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ServiceJobs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ServiceItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "ServiceOrderId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ServiceJobs");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalId",
                table: "Users",
                column: "PersonalId",
                unique: true,
                filter: "[PersonalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderItems_ServiceItemId",
                table: "ServiceOrderItems",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderJobs_AssignedUserId",
                table: "ServiceOrderJobs",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderJobs_ServiceJobId",
                table: "ServiceOrderJobs",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderJobs_ServiceOrderId",
                table: "ServiceOrderJobs",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderJobStatusHistory_ServiceOrderJobId",
                table: "ServiceOrderJobStatusHistory",
                column: "ServiceOrderJobId");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "ServiceItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderItems_ServiceItems_ServiceItemId",
                table: "ServiceOrderItems",
                column: "ServiceItemId",
                principalTable: "ServiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Users_CustomerId",
                table: "ServiceOrders",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_CustomerId",
                table: "Vehicles",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderItems_ServiceItems_ServiceItemId",
                table: "ServiceOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Users_CustomerId",
                table: "ServiceOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_CustomerId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "ServiceOrderJobStatusHistory");

            migrationBuilder.DropTable(
                name: "ServiceOrderJobs");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonalId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrderItems_ServiceItemId",
                table: "ServiceOrderItems");

            migrationBuilder.DropColumn(
                name: "PersonalId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserKind",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ServiceItemId",
                table: "ServiceOrderItems");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ServiceItems");

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "ServiceJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOrderId",
                table: "ServiceJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ServiceJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitCost",
                table: "ServiceJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitCost",
                table: "ServiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonalId = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceJobStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceJobStatusHistory_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobs_AssignedUserId",
                table: "ServiceJobs",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobs_ServiceOrderId",
                table: "ServiceJobs",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PersonalId",
                table: "Customers",
                column: "PersonalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobStatusHistory_ServiceJobId",
                table: "ServiceJobStatusHistory",
                column: "ServiceJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobs_ServiceOrders_ServiceOrderId",
                table: "ServiceJobs",
                column: "ServiceOrderId",
                principalTable: "ServiceOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobs_Users_AssignedUserId",
                table: "ServiceJobs",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Customers_CustomerId",
                table: "ServiceOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Customers_CustomerId",
                table: "Vehicles",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
