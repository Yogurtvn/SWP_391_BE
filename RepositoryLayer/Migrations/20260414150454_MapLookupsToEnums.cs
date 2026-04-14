using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepositoryLayer.Migrations
{
    /// <inheritdoc />
    public partial class MapLookupsToEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderStatuses_OrderStatusId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderTypes_OrderTypeId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingStatuses_ShippingStatusId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusHistory_OrderStatuses_OrderStatusId",
                table: "OrderStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentHistory_PaymentStatuses_PaymentStatusId",
                table: "PaymentHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionSpecs_PrescriptionStatuses_PrescriptionStatusId",
                table: "PrescriptionSpecs");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "OrderStatuses");

            migrationBuilder.DropTable(
                name: "OrderTypes");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "PaymentStatuses");

            migrationBuilder.DropTable(
                name: "PrescriptionStatuses");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ShippingStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionSpecs_PrescriptionStatusId",
                table: "PrescriptionSpecs");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentMethodId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentHistory_PaymentStatusId",
                table: "PaymentHistory");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistory_OrderStatusId",
                table: "OrderStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderStatusId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderTypeId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingStatusId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "PrescriptionStatusId",
                table: "PrescriptionSpecs",
                newName: "PrescriptionStatus");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodId",
                table: "Payments",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "PaymentStatusId",
                table: "Payments",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "PaymentStatusId",
                table: "PaymentHistory",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "OrderStatusId",
                table: "OrderStatusHistory",
                newName: "OrderStatus");

            migrationBuilder.RenameColumn(
                name: "OrderStatusId",
                table: "Orders",
                newName: "OrderStatus");

            migrationBuilder.RenameColumn(
                name: "OrderTypeId",
                table: "Orders",
                newName: "OrderType");

            migrationBuilder.RenameColumn(
                name: "ShippingStatusId",
                table: "Orders",
                newName: "ShippingStatus");

            migrationBuilder.AlterColumn<byte>(
                name: "Role",
                table: "Users",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "PrescriptionStatus",
                table: "PrescriptionSpecs",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "PaymentMethod",
                table: "Payments",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "PaymentStatus",
                table: "Payments",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "PaymentStatus",
                table: "PaymentHistory",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "OrderStatus",
                table: "OrderStatusHistory",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "OrderStatus",
                table: "Orders",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "OrderType",
                table: "Orders",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "ShippingStatus",
                table: "Orders",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrescriptionSpecs_PrescriptionStatus",
                table: "PrescriptionSpecs",
                sql: "[PrescriptionStatus] IN (1, 2, 3, 4, 5, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_PaymentMethod",
                table: "Payments",
                sql: "[PaymentMethod] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_PaymentStatus",
                table: "Payments",
                sql: "[PaymentStatus] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentHistory_PaymentStatus",
                table: "PaymentHistory",
                sql: "[PaymentStatus] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderStatusHistory_OrderStatus",
                table: "OrderStatusHistory",
                sql: "[OrderStatus] IN (1, 2, 3, 4, 5, 6, 7)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_OrderStatus",
                table: "Orders",
                sql: "[OrderStatus] IN (1, 2, 3, 4, 5, 6, 7)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_OrderType",
                table: "Orders",
                sql: "[OrderType] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingStatus",
                table: "Orders",
                sql: "[ShippingStatus] IS NULL OR [ShippingStatus] IN (1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PrescriptionSpecs_PrescriptionStatus",
                table: "PrescriptionSpecs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_PaymentMethod",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_PaymentStatus",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentHistory_PaymentStatus",
                table: "PaymentHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderStatusHistory_OrderStatus",
                table: "OrderStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_OrderStatus",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_OrderType",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingStatus",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "PrescriptionStatus",
                table: "PrescriptionSpecs",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentStatus",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentStatus",
                table: "PaymentHistory",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "OrderStatus",
                table: "OrderStatusHistory",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "OrderStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "OrderType",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "ShippingStatus",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "PrescriptionStatus",
                table: "PrescriptionSpecs",
                newName: "PrescriptionStatusId");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "Payments",
                newName: "PaymentMethodId");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "Payments",
                newName: "PaymentStatusId");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "PaymentHistory",
                newName: "PaymentStatusId");

            migrationBuilder.RenameColumn(
                name: "OrderStatus",
                table: "OrderStatusHistory",
                newName: "OrderStatusId");

            migrationBuilder.RenameColumn(
                name: "OrderStatus",
                table: "Orders",
                newName: "OrderStatusId");

            migrationBuilder.RenameColumn(
                name: "OrderType",
                table: "Orders",
                newName: "OrderTypeId");

            migrationBuilder.RenameColumn(
                name: "ShippingStatus",
                table: "Orders",
                newName: "ShippingStatusId");

            migrationBuilder.CreateTable(
                name: "OrderStatuses",
                columns: table => new
                {
                    OrderStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatuses", x => x.OrderStatusId);
                });

            migrationBuilder.CreateTable(
                name: "OrderTypes",
                columns: table => new
                {
                    OrderTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypes", x => x.OrderTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.PaymentMethodId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatuses",
                columns: table => new
                {
                    PaymentStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatuses", x => x.PaymentStatusId);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionStatuses",
                columns: table => new
                {
                    PrescriptionStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionStatuses", x => x.PrescriptionStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "ShippingStatuses",
                columns: table => new
                {
                    ShippingStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingStatuses", x => x.ShippingStatusId);
                });

            migrationBuilder.InsertData(
                table: "OrderStatuses",
                columns: new[] { "OrderStatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Confirmed" },
                    { 3, "AwaitingStock" },
                    { 4, "Processing" },
                    { 5, "Shipped" },
                    { 6, "Completed" },
                    { 7, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "OrderTypes",
                columns: new[] { "OrderTypeId", "OrderTypeName" },
                values: new object[,]
                {
                    { 1, "Ready" },
                    { 2, "PreOrder" },
                    { 3, "Prescription" }
                });

            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "PaymentMethodId", "MethodName" },
                values: new object[,]
                {
                    { 1, "COD" },
                    { 2, "VNPay" },
                    { 3, "Momo" }
                });

            migrationBuilder.InsertData(
                table: "PaymentStatuses",
                columns: new[] { "PaymentStatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Failed" }
                });

            migrationBuilder.InsertData(
                table: "PrescriptionStatuses",
                columns: new[] { "PrescriptionStatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "Submitted" },
                    { 2, "Reviewing" },
                    { 3, "NeedMoreInfo" },
                    { 4, "Approved" },
                    { 5, "Rejected" },
                    { 6, "InProduction" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Staff" },
                    { 3, "Customer" }
                });

            migrationBuilder.InsertData(
                table: "ShippingStatuses",
                columns: new[] { "ShippingStatusId", "StatusName" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Picking" },
                    { 3, "Delivering" },
                    { 4, "Delivered" },
                    { 5, "Failed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionSpecs_PrescriptionStatusId",
                table: "PrescriptionSpecs",
                column: "PrescriptionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethodId",
                table: "Payments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatusId",
                table: "Payments",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistory_PaymentStatusId",
                table: "PaymentHistory",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistory_OrderStatusId",
                table: "OrderStatusHistory",
                column: "OrderStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderStatusId",
                table: "Orders",
                column: "OrderStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderTypeId",
                table: "Orders",
                column: "OrderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingStatusId",
                table: "Orders",
                column: "ShippingStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_StatusName",
                table: "OrderStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypes_OrderTypeName",
                table: "OrderTypes",
                column: "OrderTypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_MethodName",
                table: "PaymentMethods",
                column: "MethodName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatuses_StatusName",
                table: "PaymentStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionStatuses_StatusName",
                table: "PrescriptionStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingStatuses_StatusName",
                table: "ShippingStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderStatuses_OrderStatusId",
                table: "Orders",
                column: "OrderStatusId",
                principalTable: "OrderStatuses",
                principalColumn: "OrderStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderTypes_OrderTypeId",
                table: "Orders",
                column: "OrderTypeId",
                principalTable: "OrderTypes",
                principalColumn: "OrderTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingStatuses_ShippingStatusId",
                table: "Orders",
                column: "ShippingStatusId",
                principalTable: "ShippingStatuses",
                principalColumn: "ShippingStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusHistory_OrderStatuses_OrderStatusId",
                table: "OrderStatusHistory",
                column: "OrderStatusId",
                principalTable: "OrderStatuses",
                principalColumn: "OrderStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentHistory_PaymentStatuses_PaymentStatusId",
                table: "PaymentHistory",
                column: "PaymentStatusId",
                principalTable: "PaymentStatuses",
                principalColumn: "PaymentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodId",
                table: "Payments",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "PaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentStatuses_PaymentStatusId",
                table: "Payments",
                column: "PaymentStatusId",
                principalTable: "PaymentStatuses",
                principalColumn: "PaymentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionSpecs_PrescriptionStatuses_PrescriptionStatusId",
                table: "PrescriptionSpecs",
                column: "PrescriptionStatusId",
                principalTable: "PrescriptionStatuses",
                principalColumn: "PrescriptionStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "RoleId");
        }
    }
}
