using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepositoryLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionCancelledStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PrescriptionSpecs_PrescriptionStatus",
                table: "PrescriptionSpecs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrescriptionSpecs_PrescriptionStatus",
                table: "PrescriptionSpecs",
                sql: "[PrescriptionStatus] IN (1, 2, 3, 4, 5, 6, 7)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PrescriptionSpecs_PrescriptionStatus",
                table: "PrescriptionSpecs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrescriptionSpecs_PrescriptionStatus",
                table: "PrescriptionSpecs",
                sql: "[PrescriptionStatus] IN (1, 2, 3, 4, 5, 6)");
        }
    }
}
