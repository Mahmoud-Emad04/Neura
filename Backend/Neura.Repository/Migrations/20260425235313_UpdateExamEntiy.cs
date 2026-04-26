using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExamEntiy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttemptViolation_ExamAttempts_ExamAttemptId",
                table: "AttemptViolation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttemptViolation",
                table: "AttemptViolation");

            migrationBuilder.RenameTable(
                name: "AttemptViolation",
                newName: "AttemptViolations");

            migrationBuilder.RenameIndex(
                name: "IX_AttemptViolation_ExamAttemptId",
                table: "AttemptViolations",
                newName: "IX_AttemptViolations_ExamAttemptId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttemptViolations",
                table: "AttemptViolations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptViolations_ExamAttempts_ExamAttemptId",
                table: "AttemptViolations",
                column: "ExamAttemptId",
                principalTable: "ExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttemptViolations_ExamAttempts_ExamAttemptId",
                table: "AttemptViolations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttemptViolations",
                table: "AttemptViolations");

            migrationBuilder.RenameTable(
                name: "AttemptViolations",
                newName: "AttemptViolation");

            migrationBuilder.RenameIndex(
                name: "IX_AttemptViolations_ExamAttemptId",
                table: "AttemptViolation",
                newName: "IX_AttemptViolation_ExamAttemptId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttemptViolation",
                table: "AttemptViolation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptViolation_ExamAttempts_ExamAttemptId",
                table: "AttemptViolation",
                column: "ExamAttemptId",
                principalTable: "ExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
