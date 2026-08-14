using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "Users",
                type: "text",
                nullable: true);

            // Existing application profiles predate their link to ASP.NET Identity.
            // Give each one a unique placeholder so the new required column and
            // unique index can be introduced without deleting existing data.
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "IdentityUserId" = 'legacy-profile-' || "Id"::text
                WHERE "IdentityUserId" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdentityUserId",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityUserId",
                table: "Users",
                column: "IdentityUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OnboardingStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
