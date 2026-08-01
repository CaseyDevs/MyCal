using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCal.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddGoaltype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoalType",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoalType",
                table: "Users");
        }
    }
}
