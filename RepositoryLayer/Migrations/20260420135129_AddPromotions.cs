using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepositoryLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_PaymentMethod",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "ProductVariants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "OrderItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitPrice",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PromotionNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "CartItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "CartItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitPrice",
                table: "CartItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "CartItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.PromotionId);
                    table.CheckConstraint("CK_Promotions_DiscountPercent", "[DiscountPercent] > 0 AND [DiscountPercent] <= 100");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_PromotionId",
                table: "ProductVariants",
                column: "PromotionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_PaymentMethod",
                table: "Payments",
                sql: "[PaymentMethod] IN (1, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Promotions_PromotionId",
                table: "ProductVariants",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("UPDATE CartItems SET OriginalUnitPrice = UnitPrice, FinalUnitPrice = UnitPrice;");
            migrationBuilder.Sql("UPDATE OrderItems SET OriginalUnitPrice = UnitPrice, FinalUnitPrice = UnitPrice;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Promotions_PromotionId",
                table: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_PromotionId",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_PaymentMethod",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "FinalUnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "FinalUnitPrice",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "CartItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_PaymentMethod",
                table: "Payments",
                sql: "[PaymentMethod] IN (1, 2, 3)");
        }
    }
}
