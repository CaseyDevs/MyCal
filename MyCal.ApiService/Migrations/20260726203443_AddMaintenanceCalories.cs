using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCal.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceCalories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaintenanceCalories",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaintenanceCalories",
                table: "Users");
        }
    }
}
