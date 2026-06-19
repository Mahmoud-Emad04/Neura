using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateViolationExamId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamViolations_Exams_ExamId",
                table: "ExamViolations");

            migrationBuilder.RenameColumn(
                name: "ExamId",
                table: "ExamViolations",
                newName: "LessonId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamViolations_ExamId",
                table: "ExamViolations",
                newName: "IX_ExamViolations_LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamViolations_Lessons_LessonId",
                table: "ExamViolations",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamViolations_Lessons_LessonId",
                table: "ExamViolations");

            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "ExamViolations",
                newName: "ExamId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamViolations_LessonId",
                table: "ExamViolations",
                newName: "IX_ExamViolations_ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamViolations_Exams_ExamId",
                table: "ExamViolations",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
