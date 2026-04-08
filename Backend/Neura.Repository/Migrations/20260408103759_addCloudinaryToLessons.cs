using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class addCloudinaryToLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicId",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudinaryVideoUrl",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVideoPrivate",
                table: "Lessons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudinaryPublicId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CloudinaryVideoUrl",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "IsVideoPrivate",
                table: "Lessons");
        }
    }
}
