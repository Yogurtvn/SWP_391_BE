using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepositoryLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingPackageDimensionsToProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackageHeightCm",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "PackageLengthCm",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "PackageWidthCm",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "WeightGram",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 200);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_PackageHeightCm",
                table: "ProductVariants",
                sql: "[PackageHeightCm] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_PackageLengthCm",
                table: "ProductVariants",
                sql: "[PackageLengthCm] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_PackageWidthCm",
                table: "ProductVariants",
                sql: "[PackageWidthCm] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_WeightGram",
                table: "ProductVariants",
                sql: "[WeightGram] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_PackageHeightCm",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_PackageLengthCm",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_PackageWidthCm",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_WeightGram",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PackageHeightCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PackageLengthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PackageWidthCm",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WeightGram",
                table: "ProductVariants");
        }
    }
}
