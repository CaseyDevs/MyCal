using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyCal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateFoodsAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TotalCalories = table.Column<double>(type: "double precision", nullable: false),
                    TotalProtein = table.Column<double>(type: "double precision", nullable: true),
                    TotalCarbohydrates = table.Column<double>(type: "double precision", nullable: true),
                    TotalFats = table.Column<double>(type: "double precision", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLog_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Food",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    Calories = table.Column<double>(type: "double precision", nullable: false),
                    Protein = table.Column<double>(type: "double precision", nullable: true),
                    Carbohydrates = table.Column<double>(type: "double precision", nullable: true),
                    Fats = table.Column<double>(type: "double precision", nullable: true),
                    FoodLogId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Food", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Food_FoodLog_FoodLogId",
                        column: x => x.FoodLogId,
                        principalTable: "FoodLog",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Food_FoodLogId",
                table: "Food",
                column: "FoodLogId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLog_UserId",
                table: "FoodLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLog_UserId_Date",
                table: "FoodLog",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Food");

            migrationBuilder.DropTable(
                name: "FoodLog");
        }
    }
}
