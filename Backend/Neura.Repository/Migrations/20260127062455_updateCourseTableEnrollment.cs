using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateCourseTableEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "CourseUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "CourseUsers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "CourseUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CourseUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "CourseUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "CourseUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseUsers_ApplicationUserId",
                table: "CourseUsers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseUsers_CreatedById",
                table: "CourseUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CourseUsers_UpdatedById",
                table: "CourseUsers",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUsers_AspNetUsers_ApplicationUserId",
                table: "CourseUsers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUsers_AspNetUsers_CreatedById",
                table: "CourseUsers",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUsers_AspNetUsers_UpdatedById",
                table: "CourseUsers",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseUsers_AspNetUsers_ApplicationUserId",
                table: "CourseUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseUsers_AspNetUsers_CreatedById",
                table: "CourseUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseUsers_AspNetUsers_UpdatedById",
                table: "CourseUsers");

            migrationBuilder.DropIndex(
                name: "IX_CourseUsers_ApplicationUserId",
                table: "CourseUsers");

            migrationBuilder.DropIndex(
                name: "IX_CourseUsers_CreatedById",
                table: "CourseUsers");

            migrationBuilder.DropIndex(
                name: "IX_CourseUsers_UpdatedById",
                table: "CourseUsers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CourseUsers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "CourseUsers");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "CourseUsers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CourseUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "CourseUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "CourseUsers");
        }
    }
}
