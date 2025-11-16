using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonaWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserProfileEditings2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserProfiles",
                newName: "Platform");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Platform",
                table: "UserProfiles",
                newName: "UserId");
        }
    }
}
