using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonaWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserProfileEditings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "FacebookUserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "FacebookUsername",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "InstagramUserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "InstagramUsername",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastFacebookFetchUtc",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastInstagramFetchUtc",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastTikTokFetchUtc",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TikTokUserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TikTokUsername",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "XUsername",
                table: "UserProfiles",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "XUserId",
                table: "UserProfiles",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "UserProfiles",
                newName: "PersonName");

            migrationBuilder.RenameColumn(
                name: "LastXFetchUtc",
                table: "UserProfiles",
                newName: "LastFetchDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "UserProfiles",
                newName: "XUsername");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserProfiles",
                newName: "XUserId");

            migrationBuilder.RenameColumn(
                name: "PersonName",
                table: "UserProfiles",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "LastFetchDate",
                table: "UserProfiles",
                newName: "LastXFetchUtc");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacebookUserId",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUsername",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUserId",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUsername",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFacebookFetchUtc",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInstagramFetchUtc",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTikTokFetchUtc",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokUserId",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokUsername",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
