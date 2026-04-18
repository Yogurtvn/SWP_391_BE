using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepositoryLayer.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithDocSchema20260415_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_VariantId",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "FramePrice",
                table: "OrderItems",
                newName: "UnitPrice");

            migrationBuilder.AddColumn<bool>(
                name: "PrescriptionCompatible",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "ProductType",
                table: "Products",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoatingPrice",
                table: "PrescriptionSpecs",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Coatings",
                table: "PrescriptionSpecs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LensBasePrice",
                table: "PrescriptionSpecs",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LensMaterial",
                table: "PrescriptionSpecs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LensTypeCode",
                table: "PrescriptionSpecs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LensTypeId",
                table: "PrescriptionSpecs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLensPrice",
                table: "PrescriptionSpecs",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SelectedColor",
                table: "OrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LensCode",
                table: "LensTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CartItems",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<byte>(
                name: "ItemType",
                table: "CartItems",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "OrderType",
                table: "CartItems",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedColor",
                table: "CartItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "CartItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "CartItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CartItems",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.CreateTable(
                name: "CartPrescriptionDetails",
                columns: table => new
                {
                    CartPrescriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartItemId = table.Column<int>(type: "int", nullable: false),
                    LensTypeId = table.Column<int>(type: "int", nullable: false),
                    LensTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LensMaterial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Coatings = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LensBasePrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    CoatingPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    TotalLensPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    SPH_Left = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SPH_Right = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CYL_Left = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CYL_Right = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Axis_Left = table.Column<int>(type: "int", nullable: true),
                    Axis_Right = table.Column<int>(type: "int", nullable: true),
                    PD = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PrescriptionImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartPrescriptionDetails", x => x.CartPrescriptionId);
                    table.ForeignKey(
                        name: "FK_CartPrescriptionDetails_CartItems_CartItemId",
                        column: x => x.CartItemId,
                        principalTable: "CartItems",
                        principalColumn: "CartItemId");
                    table.ForeignKey(
                        name: "FK_CartPrescriptionDetails_LensTypes_LensTypeId",
                        column: x => x.LensTypeId,
                        principalTable: "LensTypes",
                        principalColumn: "LensTypeId");
                });

            migrationBuilder.Sql(
                """
                UPDATE [Products]
                SET [ProductType] = 1
                WHERE [ProductType] IS NULL;
                """);

            migrationBuilder.AlterColumn<byte>(
                name: "ProductType",
                table: "Products",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [CartItems]
                SET [ItemType] = 1
                WHERE [ItemType] IS NULL;

                UPDATE [CartItems]
                SET [OrderType] = 1
                WHERE [OrderType] IS NULL;
                """);

            migrationBuilder.AlterColumn<byte>(
                name: "ItemType",
                table: "CartItems",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "OrderType",
                table: "CartItems",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [LensTypes]
                SET [LensCode] = CONCAT('LENS-', [LensTypeId])
                WHERE [LensCode] IS NULL OR [LensCode] = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "LensCode",
                table: "LensTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.Sql(
                """
                DECLARE @DefaultLensTypeId INT = (
                    SELECT TOP (1) [LensTypeId]
                    FROM [LensTypes]
                    ORDER BY [LensTypeId]);

                IF @DefaultLensTypeId IS NULL
                BEGIN
                    INSERT INTO [LensTypes] ([LensCode], [LensName], [Description], [Price], [IsActive], [CreatedAt])
                    VALUES ('LENS-MIGRATION-DEFAULT', N'Tròng kính mặc định chuyển đổi', N'Tròng kính được tạo tự động để tương thích với các đơn theo toa cũ.', 0, 0, GETDATE());

                    SET @DefaultLensTypeId = CAST(SCOPE_IDENTITY() AS INT);
                END

                UPDATE [PrescriptionSpecs]
                SET [LensTypeId] = @DefaultLensTypeId
                WHERE [LensTypeId] IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "LensTypeId",
                table: "PrescriptionSpecs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_PrescriptionCompatible",
                table: "Products",
                column: "PrescriptionCompatible");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductType",
                table: "Products",
                column: "ProductType");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_ProductType",
                table: "Products",
                sql: "[ProductType] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionSpecs_LensTypeId",
                table: "PrescriptionSpecs",
                column: "LensTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LensTypes_LensCode",
                table: "LensTypes",
                column: "LensCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_IsPreOrderAllowed",
                table: "Inventory",
                column: "IsPreOrderAllowed");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ItemType",
                table: "CartItems",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_OrderType",
                table: "CartItems",
                column: "OrderType");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems",
                sql: "[ItemType] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_OrderType",
                table: "CartItems",
                sql: "[OrderType] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_CartPrescriptionDetails_CartItemId",
                table: "CartPrescriptionDetails",
                column: "CartItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartPrescriptionDetails_LensTypeId",
                table: "CartPrescriptionDetails",
                column: "LensTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionSpecs_LensTypes_LensTypeId",
                table: "PrescriptionSpecs",
                column: "LensTypeId",
                principalTable: "LensTypes",
                principalColumn: "LensTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionSpecs_LensTypes_LensTypeId",
                table: "PrescriptionSpecs");

            migrationBuilder.DropTable(
                name: "CartPrescriptionDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_PrescriptionCompatible",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductType",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_ProductType",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionSpecs_LensTypeId",
                table: "PrescriptionSpecs");

            migrationBuilder.DropIndex(
                name: "IX_LensTypes_LensCode",
                table: "LensTypes");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_IsPreOrderAllowed",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ItemType",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_OrderType",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_OrderType",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "PrescriptionCompatible",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CoatingPrice",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "Coatings",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "LensBasePrice",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "LensMaterial",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "LensTypeCode",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "LensTypeId",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "TotalLensPrice",
                table: "PrescriptionSpecs");

            migrationBuilder.DropColumn(
                name: "SelectedColor",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LensCode",
                table: "LensTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "SelectedColor",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "FramePrice");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_VariantId",
                table: "CartItems",
                columns: new[] { "CartId", "VariantId" },
                unique: true);
        }
    }
}
