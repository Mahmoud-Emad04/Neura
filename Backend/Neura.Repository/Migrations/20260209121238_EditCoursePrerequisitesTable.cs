using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class EditCoursePrerequisitesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseLearningOutcome_Courses_CourseId",
                table: "CourseLearningOutcome");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursePrerequisite_Courses_CourseId",
                table: "CoursePrerequisite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CoursePrerequisite",
                table: "CoursePrerequisite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseLearningOutcome",
                table: "CourseLearningOutcome");

            migrationBuilder.RenameTable(
                name: "CoursePrerequisite",
                newName: "CoursePrerequisites");

            migrationBuilder.RenameTable(
                name: "CourseLearningOutcome",
                newName: "CourseLearningOutcomes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CoursePrerequisites",
                table: "CoursePrerequisites",
                columns: new[] { "CourseId", "Requirement" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseLearningOutcomes",
                table: "CourseLearningOutcomes",
                columns: new[] { "CourseId", "Outcome" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseLearningOutcomes_Courses_CourseId",
                table: "CourseLearningOutcomes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePrerequisites_Courses_CourseId",
                table: "CoursePrerequisites",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseLearningOutcomes_Courses_CourseId",
                table: "CourseLearningOutcomes");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursePrerequisites_Courses_CourseId",
                table: "CoursePrerequisites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CoursePrerequisites",
                table: "CoursePrerequisites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseLearningOutcomes",
                table: "CourseLearningOutcomes");

            migrationBuilder.RenameTable(
                name: "CoursePrerequisites",
                newName: "CoursePrerequisite");

            migrationBuilder.RenameTable(
                name: "CourseLearningOutcomes",
                newName: "CourseLearningOutcome");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CoursePrerequisite",
                table: "CoursePrerequisite",
                columns: new[] { "CourseId", "Requirement" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseLearningOutcome",
                table: "CourseLearningOutcome",
                columns: new[] { "CourseId", "Outcome" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseLearningOutcome_Courses_CourseId",
                table: "CourseLearningOutcome",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePrerequisite_Courses_CourseId",
                table: "CoursePrerequisite",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
