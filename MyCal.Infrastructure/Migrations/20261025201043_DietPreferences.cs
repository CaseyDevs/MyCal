using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DietPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Diet",
                table: "Targets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProteinPreference",
                table: "Targets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Training",
                table: "Targets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Diet",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "ProteinPreference",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "Training",
                table: "Targets");
        }
    }
}
