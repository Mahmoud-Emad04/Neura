using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSectionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Courses_CourseId1",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Sections_CourseId1",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "CourseId1",
                table: "Sections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId1",
                table: "Sections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_CourseId1",
                table: "Sections",
                column: "CourseId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Courses_CourseId1",
                table: "Sections",
                column: "CourseId1",
                principalTable: "Courses",
                principalColumn: "Id");
        }
    }
}
