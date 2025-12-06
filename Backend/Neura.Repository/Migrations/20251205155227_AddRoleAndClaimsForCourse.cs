using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAndClaimsForCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "IsDefualt", "IsDeleted", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "019aeef9-ea10-7594-a042-ebc8958f1366", "019aeef9-ea10-7594-a042-ebc9c5b329bd", false, false, "CourseOwner", "COURSEOWNER" },
                    { "019aeef9-ea10-7594-a042-ebca472ee63f", "019aeef9-ea10-7594-a042-ebcbaa20d23d", false, false, "CoInstructor", "COINSTRUCTOR" },
                    { "019aeef9-ea10-7594-a042-ebccf58eb683", "019aeef9-ea10-7594-a042-ebcdfbd1c0ff", false, false, "TeachingAssistant", "TEACHINGASSISTANT" },
                    { "019aeef9-ea10-7594-a042-ebce4c1dec9b", "019aeef9-ea10-7594-a042-ebcf7ddf3644", false, false, "Student", "STUDENT" }
                });

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 111, "permissions", "courses:update", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 112, "permissions", "courses:delete", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 113, "permissions", "lessons:add", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 114, "permissions", "lessons:update", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 115, "permissions", "students:view", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 116, "permissions", "lessons:add", "019aeef9-ea10-7594-a042-ebca472ee63f" },
                    { 117, "permissions", "lessons:update", "019aeef9-ea10-7594-a042-ebca472ee63f" },
                    { 118, "permissions", "students:view", "019aeef9-ea10-7594-a042-ebca472ee63f" },
                    { 119, "permissions", "students:view", "019aeef9-ea10-7594-a042-ebccf58eb683" },
                    { 120, "permissions", "moderate:qna", "019aeef9-ea10-7594-a042-ebccf58eb683" },
                    { 121, "permissions", "courses:view", "019aeef9-ea10-7594-a042-ebce4c1dec9b" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebc8958f1366");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebca472ee63f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebccf58eb683");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebce4c1dec9b");
        }
    }
}
