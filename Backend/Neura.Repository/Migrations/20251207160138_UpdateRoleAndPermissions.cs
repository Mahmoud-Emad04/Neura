using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoleAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "Role",
                table: "CourseUsers");

            migrationBuilder.AddColumn<int>(
                name: "PermissionMask",
                table: "CourseUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 111,
                column: "ClaimValue",
                value: "courses:read");

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 113,
                column: "ClaimValue",
                value: "courses:update");

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 114,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "courses:read", "019aeef9-ea10-7594-a042-ebca472ee63f" });

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 115,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "courses:update", "019aeef9-ea10-7594-a042-ebca472ee63f" });

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 116,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "courses:read", "019aeef9-ea10-7594-a042-ebccf58eb683" });

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 117,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "courses:read", "019aeef9-ea10-7594-a042-ebce4c1dec9b" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermissionMask",
                table: "CourseUsers");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "CourseUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 111,
                column: "ClaimValue",
                value: "courses:update");

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 113,
                column: "ClaimValue",
                value: "lessons:add");

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 114,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "lessons:update", "019aeef9-ea10-7594-a042-ebc8958f1366" });

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 115,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "students:view", "019aeef9-ea10-7594-a042-ebc8958f1366" });

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 116,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "lessons:add", "019aeef9-ea10-7594-a042-ebca472ee63f" });

            migrationBuilder.UpdateData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 117,
                columns: new[] { "ClaimValue", "RoleId" },
                values: new object[] { "lessons:update", "019aeef9-ea10-7594-a042-ebca472ee63f" });

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 118, "permissions", "students:view", "019aeef9-ea10-7594-a042-ebca472ee63f" },
                    { 119, "permissions", "students:view", "019aeef9-ea10-7594-a042-ebccf58eb683" },
                    { 120, "permissions", "moderate:qna", "019aeef9-ea10-7594-a042-ebccf58eb683" },
                    { 121, "permissions", "courses:view", "019aeef9-ea10-7594-a042-ebce4c1dec9b" }
                });
        }
    }
}
