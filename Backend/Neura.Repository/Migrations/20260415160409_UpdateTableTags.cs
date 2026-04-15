using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTag_Courses_CoursesId",
                table: "CourseTag");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseTag_Tags_TagsId",
                table: "CourseTag");

            migrationBuilder.RenameColumn(
                name: "TagsId",
                table: "CourseTag",
                newName: "TagId");

            migrationBuilder.RenameColumn(
                name: "CoursesId",
                table: "CourseTag",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseTag_TagsId",
                table: "CourseTag",
                newName: "IX_CourseTag_TagId");

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Tags",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Tags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tags",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Tags",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Tags",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Tags",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsActive",
                table: "Tags",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsActive_IsDeleted",
                table: "Tags",
                columns: new[] { "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTag_Courses_CourseId",
                table: "CourseTag",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTag_Tags_TagId",
                table: "CourseTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTag_Courses_CourseId",
                table: "CourseTag");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseTag_Tags_TagId",
                table: "CourseTag");

            migrationBuilder.DropIndex(
                name: "IX_Tags_IsActive",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_IsActive_IsDeleted",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Tags");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "CourseTag",
                newName: "TagsId");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "CourseTag",
                newName: "CoursesId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseTag_TagId",
                table: "CourseTag",
                newName: "IX_CourseTag_TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTag_Courses_CoursesId",
                table: "CourseTag",
                column: "CoursesId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTag_Tags_TagsId",
                table: "CourseTag",
                column: "TagsId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
