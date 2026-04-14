using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepositoryLayer.Migrations
{
    /// <inheritdoc />
    public partial class SyncSchemaWithOnlineEyewearSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Promotions_PromotionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_OperationsStaffId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_SalesStaffId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "Returns");

            migrationBuilder.DropTable(
                name: "SupplyReceipts");

            migrationBuilder.DropTable(
                name: "ReturnStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OperationsStaffId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PromotionId",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Inventory_ReservedQuantity",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems");

            migrationBuilder.DeleteData(
                table: "PaymentStatuses",
                keyColumn: "PaymentStatusId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "Is3D",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OperationsStaffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "Inventory");

            migrationBuilder.RenameColumn(
                name: "AxisRight",
                table: "PrescriptionSpecs",
                newName: "Axis_Right");

            migrationBuilder.RenameColumn(
                name: "AxisLeft",
                table: "PrescriptionSpecs",
                newName: "Axis_Left");

            migrationBuilder.RenameColumn(
                name: "SalesStaffId",
                table: "Orders",
                newName: "StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_SalesStaffId",
                table: "Orders",
                newName: "IX_Orders_StaffId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<decimal>(
                name: "BasePrice",
                table: "Products",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ProductImages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "ProductImages",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "PrescriptionSpecs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PrescriptionSpecs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Policies",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Policies",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Policies",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentId",
                table: "PaymentHistory",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentHistory",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "OrderStatusHistory",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderStatusHistory",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingAddress",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverPhone",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverName",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FramePrice",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LensPrice",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LensTypeId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPreOrderAllowed",
                table: "Inventory",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedRestockDate",
                table: "Inventory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreOrderNote",
                table: "Inventory",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Carts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Carts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Carts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CartId",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "LensTypes",
                columns: table => new
                {
                    LensTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LensName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LensTypes", x => x.LensTypeId);
                });

            migrationBuilder.CreateTable(
                name: "StockReceipts",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    QuantityReceived = table.Column<int>(type: "int", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    StaffId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceipts", x => x.ReceiptId);
                    table.CheckConstraint("CK_StockReceipts_QuantityReceived", "[QuantityReceived] > 0");
                    table.ForeignKey(
                        name: "FK_StockReceipts_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId");
                    table.ForeignKey(
                        name: "FK_StockReceipts_Users_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.UpdateData(
                table: "PrescriptionStatuses",
                keyColumn: "PrescriptionStatusId",
                keyValue: 3,
                column: "StatusName",
                value: "NeedMoreInfo");

            migrationBuilder.UpdateData(
                table: "PrescriptionStatuses",
                keyColumn: "PrescriptionStatusId",
                keyValue: 4,
                column: "StatusName",
                value: "Approved");

            migrationBuilder.InsertData(
                table: "PrescriptionStatuses",
                columns: new[] { "PrescriptionStatusId", "StatusName" },
                values: new object[,]
                {
                    { 5, "Rejected" },
                    { 6, "InProduction" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "RoleName",
                value: "Staff");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "RoleName",
                value: "Customer");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_LensTypeId",
                table: "OrderItems",
                column: "LensTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_VariantId",
                table: "CartItems",
                columns: new[] { "CartId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_StaffId",
                table: "StockReceipts",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_VariantId",
                table: "StockReceipts",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_LensTypes_LensTypeId",
                table: "OrderItems",
                column: "LensTypeId",
                principalTable: "LensTypes",
                principalColumn: "LensTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_StaffId",
                table: "Orders",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_LensTypes_LensTypeId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_StaffId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "LensTypes");

            migrationBuilder.DropTable(
                name: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_LensTypeId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_VariantId",
                table: "CartItems");

            migrationBuilder.DeleteData(
                table: "PrescriptionStatuses",
                keyColumn: "PrescriptionStatusId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PrescriptionStatuses",
                keyColumn: "PrescriptionStatusId",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FramePrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LensPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LensTypeId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ExpectedRestockDate",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "PreOrderNote",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Carts");

            migrationBuilder.RenameColumn(
                name: "Axis_Right",
                table: "PrescriptionSpecs",
                newName: "AxisRight");

            migrationBuilder.RenameColumn(
                name: "Axis_Left",
                table: "PrescriptionSpecs",
                newName: "AxisLeft");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "Orders",
                newName: "SalesStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_StaffId",
                table: "Orders",
                newName: "IX_Orders_SalesStaffId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: true,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<decimal>(
                name: "BasePrice",
                table: "Products",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ProductImages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "Is3D",
                table: "ProductImages",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "PrescriptionSpecs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PrescriptionSpecs",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Policies",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Policies",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentId",
                table: "PaymentHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentHistory",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "OrderStatusHistory",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderStatusHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingAddress",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverPhone",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverName",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "OperationsStaffId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "OrderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "OrderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "OrderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPreOrderAllowed",
                table: "Inventory",
                type: "bit",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "Inventory",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Carts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Carts",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "CartItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "CartItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CartId",
                table: "CartItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PromotionName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.PromotionId);
                });

            migrationBuilder.CreateTable(
                name: "ReturnStatuses",
                columns: table => new
                {
                    ReturnStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnStatuses", x => x.ReturnStatusId);
                });

            migrationBuilder.CreateTable(
                name: "SupplyReceipts",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: true),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    QuantityReceived = table.Column<int>(type: "int", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyReceipts", x => x.ReceiptId);
                    table.CheckConstraint("CK_SupplyReceipts_QuantityReceived", "[QuantityReceived] > 0");
                    table.ForeignKey(
                        name: "FK_SupplyReceipts_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId");
                    table.ForeignKey(
                        name: "FK_SupplyReceipts_Users_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Returns",
                columns: table => new
                {
                    ReturnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ReturnStatusId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Returns", x => x.ReturnId);
                    table.ForeignKey(
                        name: "FK_Returns_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_Returns_ReturnStatuses_ReturnStatusId",
                        column: x => x.ReturnStatusId,
                        principalTable: "ReturnStatuses",
                        principalColumn: "ReturnStatusId");
                });

            migrationBuilder.InsertData(
                table: "PaymentStatuses",
                columns: new[] { "PaymentStatusId", "StatusName" },
                values: new object[] { 4, "Refunded" });

            migrationBuilder.UpdateData(
                table: "PrescriptionStatuses",
                keyColumn: "PrescriptionStatusId",
                keyValue: 3,
                column: "StatusName",
                value: "Approved");

            migrationBuilder.UpdateData(
                table: "PrescriptionStatuses",
                keyColumn: "PrescriptionStatusId",
                keyValue: 4,
                column: "StatusName",
                value: "Rejected");

            migrationBuilder.InsertData(
                table: "ReturnStatuses",
                columns: new[] { "ReturnStatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Approved" },
                    { 3, "Rejected" },
                    { 4, "Refunded" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "RoleName",
                value: "SalesStaff");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "RoleName",
                value: "OperationsStaff");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 4, "Manager" },
                    { 5, "Customer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OperationsStaffId",
                table: "Orders",
                column: "OperationsStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PromotionId",
                table: "Orders",
                column: "PromotionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Inventory_ReservedQuantity",
                table: "Inventory",
                sql: "[ReservedQuantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_PromoCode",
                table: "Promotions",
                column: "PromoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_OrderId",
                table: "Returns",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ReturnStatusId",
                table: "Returns",
                column: "ReturnStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnStatuses_StatusName",
                table: "ReturnStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyReceipts_StaffId",
                table: "SupplyReceipts",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyReceipts_VariantId",
                table: "SupplyReceipts",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Promotions_PromotionId",
                table: "Orders",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_OperationsStaffId",
                table: "Orders",
                column: "OperationsStaffId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_SalesStaffId",
                table: "Orders",
                column: "SalesStaffId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
