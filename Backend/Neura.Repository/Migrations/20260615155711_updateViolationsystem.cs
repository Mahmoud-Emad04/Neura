using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateViolationsystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AreGradesPublished",
                table: "Exams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "ExamAttempts",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorNotes",
                table: "ExamAttempts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalScore",
                table: "ExamAttempts",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViolationReason",
                table: "ExamAttempts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreGradesPublished",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "InstructorNotes",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "OriginalScore",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "ViolationReason",
                table: "ExamAttempts");
        }
    }
}
