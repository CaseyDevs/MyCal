using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyCal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodLogEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Food_FoodLog_FoodLogId",
                table: "Food");

            migrationBuilder.DropIndex(
                name: "IX_Food_FoodLogId",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "FoodLogId",
                table: "Food");

            migrationBuilder.CreateTable(
                name: "FoodLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FoodLogId = table.Column<int>(type: "integer", nullable: false),
                    FoodId = table.Column<int>(type: "integer", nullable: false),
                    QuantityInGrams = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogEntries_FoodLog_FoodLogId",
                        column: x => x.FoodLogId,
                        principalTable: "FoodLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoodLogEntries_Food_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Food",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_FoodId",
                table: "FoodLogEntries",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_FoodLogId",
                table: "FoodLogEntries",
                column: "FoodLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodLogEntries");

            migrationBuilder.AddColumn<int>(
                name: "FoodLogId",
                table: "Food",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Food_FoodLogId",
                table: "Food",
                column: "FoodLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_Food_FoodLog_FoodLogId",
                table: "Food",
                column: "FoodLogId",
                principalTable: "FoodLog",
                principalColumn: "Id");
        }
    }
}
